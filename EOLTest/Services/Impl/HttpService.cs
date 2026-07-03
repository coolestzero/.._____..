// Copyright (c) TBD 2026.
// 用途：增强版 HTTP 客户端，支持连接/总超时分离、指数退避重试、错误分类、SSL忽略
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EOLTest.Models;
using EOLTest.Services.Common;
using Serilog;

namespace EOLTest.Services.Impl
{
    public class HttpService : IHttpService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly HttpRetryConfig _retryConfig;
        /// <summary>
        /// 重试策略配置
        /// </summary>
        public class HttpRetryConfig
        {
            /// <summary>最大重试次数（不含第一次），默认 3 次</summary>
            public int MaxRetries { get; set; } = 3;
            /// <summary>重试基础间隔（毫秒），默认 1000ms</summary>
            public int BaseDelayMs { get; set; } = 1000;
            /// <summary>是否使用指数退避（间隔 = BaseDelay × 2^重试次数），默认 true</summary>
            public bool UseExponentialBackoff { get; set; } = true;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">Serilog 日志</param>
        /// <param name="connectTimeoutSeconds">TCP 连接超时（秒），指建立 TCP 连接的最长等待时间</param>
        /// <param name="totalTimeoutSeconds">总超时（秒），指从发送请求到收完响应的最长等待时间</param>
        /// <param name="retryConfig">重试配置，不传则使用默认</param>
        public HttpService(int connectTimeoutSeconds = 10,
                           int totalTimeoutSeconds = 30,
                           HttpRetryConfig retryConfig = null)
        {
            // ★ 创建专属 Logger：写入 Log/Socket/XmlSocket.txt，按天滚动
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: "Log/Socket/XmlSocket.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger()
                .ForContext<HttpService>();
            _retryConfig = retryConfig ?? new HttpRetryConfig();
            // ★ 使用 SocketsHttpHandler 可以分别控制连接超时
            var handler = new SocketsHttpHandler
            {
                // 连接超时：仅限 TCP 握手阶段（DNS 解析 + TCP 三次握手）
                ConnectTimeout = TimeSpan.FromSeconds(connectTimeoutSeconds),
                // 忽略 SSL 证书错误（内网环境常见）
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                },
                // 限制到同一服务器的最大并发连接数，防止耗尽端口
                MaxConnectionsPerServer = 5
            };
            _httpClient = new HttpClient(handler)
            {
                // 总超时：包含连接、发送、等待响应、接收正文的全部时间
                Timeout = TimeSpan.FromSeconds(totalTimeoutSeconds)
            };
        }
        // ---------- 公开接口 ----------
        public async Task<HttpResult<string>> PostXmlAsync(string url, string xmlBody, CancellationToken ct = default)
            => await SendWithRetryAsync(HttpMethod.Post, url, xmlBody, "application/xml", ct);
        public async Task<HttpResult<string>> PostJsonAsync(string url, string jsonBody, CancellationToken ct = default)
            => await SendWithRetryAsync(HttpMethod.Post, url, jsonBody, "application/json", ct);
        public async Task<HttpResult<string>> GetStringAsync(string url, CancellationToken ct = default)
            => await SendWithRetryAsync(HttpMethod.Get, url, null, null, ct);
        // ---------- 核心：带重试的发送逻辑 ----------
        /// <summary>
        /// 发送 HTTP 请求，并按照配置自动重试（仅对网络错误/超时）。
        /// 执行流程：
        ///   1. 首次尝试
        ///   2. 若失败且错误类型允许重试 → 等待一段时间 → 再次尝试
        ///   3. 直到成功或达到最大次数
        /// </summary>
        private async Task<HttpResult<string>> SendWithRetryAsync(
            HttpMethod method, string url, string body, string contentType, CancellationToken ct)
        {
            int totalAttempts = 1 + _retryConfig.MaxRetries;   // 总尝试次数
            HttpResult<string> lastResult = null;
            for (int attempt = 0; attempt < totalAttempts; attempt++)
            {
                // 如果不是第一次尝试，则先等待（重试间隔）
                if (attempt > 0)
                {
                    int delayMs = CalculateRetryDelay(attempt);
                    _logger.Warning("[HttpService] {Method} {Url} 第 {Attempt}/{Total} 次重试，等待 {Delay}ms",
                        method, url, attempt, _retryConfig.MaxRetries, delayMs);
                    try
                    {
                        await Task.Delay(delayMs, ct);   // 等待指定毫秒，支持取消
                    }
                    catch (OperationCanceledException)
                    {
                        return HttpResult<string>.Fail(HttpErrorType.Cancelled, "重试等待期间用户取消");
                    }
                }
                // 执行单次请求
                lastResult = await SendOnceAsync(method, url, body, contentType, ct);
                // 成功了就直接返回，不再重试
                if (lastResult.Success)
                {
                    if (attempt > 0)
                        _logger.Information("[HttpService] {Method} {Url} 第 {Attempt} 次重试成功", method, url, attempt);
                    return lastResult;
                }
                // 失败情况：判断当前错误是否应该重试
                bool shouldRetry = lastResult.ErrorType switch
                {
                    HttpErrorType.Timeout => true,      // 超时：可能服务器暂时过载，可重试
                    HttpErrorType.NetworkError => true, // 网络断连：可能网络抖动，可重试
                    _ => false                          // 其他错误（如HTTP 4xx/5xx、解析错误）不重试
                };
                // 如果不可重试 或者 已经是最后一次尝试，则退出循环
                if (!shouldRetry || attempt >= totalAttempts - 1)
                    break;
                _logger.Warning("[HttpService] {Method} {Url} 失败 ({ErrorType}: {Message})，将重试",
                    method, url, lastResult.ErrorType, lastResult.ErrorMessage);
            }
            // 所有尝试均失败，记录最终错误
            _logger.Error("[HttpService] {Method} {Url} 最终失败 ({ErrorType}: {Message})，已重试 {Retries} 次",
                method, url, lastResult?.ErrorType, lastResult?.ErrorMessage, _retryConfig.MaxRetries);
            return lastResult;
        }
        /// <summary>
        /// 单次 HTTP 请求（不含重试逻辑）
        /// 发送请求并等待响应，超时或网络错误会通过异常抛出，由本方法捕获分类。
        /// </summary>
        private async Task<HttpResult<string>> SendOnceAsync(
            HttpMethod method, string url, string body, string contentType, CancellationToken ct)
        {
            try
            {
                // 构建 HttpRequestMessage
                using var request = new HttpRequestMessage(method, url);
                // 如果有请求体（POST 等），设置内容和 Content-Type
                if (body != null)
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
                _logger.Debug("[HttpService] >>> {Method} {Url}", method, url);
                // ★ 核心：发送并异步等待响应
                // 该方法会：
                //   1. 根据 URL 解析域名并建立 TCP 连接（受 ConnectTimeout 限制）
                //   2. 发送 HTTP 请求
                //   3. 等待服务器返回响应（受 HttpClient.Timeout 限制）
                //   4. 将响应头、正文读取到内存
                // 整个过程是“发送→等待接收”的完整操作，不需要额外的 Task.Delay。
                using var response = await _httpClient.SendAsync(request, ct);
                _logger.Debug("[HttpService] <<< {Method} {Url} → HTTP {(int)StatusCode}",
                    method, url, (int)response.StatusCode);
                // 检查 HTTP 状态码是否成功（2xx）
                if (!response.IsSuccessStatusCode)
                {
                    // 尝试读取响应体用于错误日志（限制 200 字符）
                    string bodyPreview = "";
                    try { bodyPreview = (await response.Content.ReadAsStringAsync()).Truncate(200); } catch { }
                    return HttpResult<string>.Fail(HttpErrorType.HttpError,
                        $"服务器返回 HTTP {(int)response.StatusCode}", bodyPreview);
                }
                // 读取响应体字符串
                string content = await response.Content.ReadAsStringAsync();
                return HttpResult<string>.Ok(content);
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                // 用户主动取消（外部 CancellationToken 被触发）
                return HttpResult<string>.Fail(HttpErrorType.Cancelled, "操作已被用户取消", ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                // 超时引发的 TaskCanceledException（没有外部取消令牌时，就是超时）
                // HttpClient 内部会通过 CancellationTokenSource 触发超时
                return HttpResult<string>.Fail(HttpErrorType.Timeout,
                    $"请求超时（超过 {_httpClient.Timeout.TotalSeconds} 秒未收到响应）", ex.Message);
            }
            catch (HttpRequestException ex)
            {
                // 网络层错误：DNS 解析失败、连接被拒、连接中断、SSL 错误等
                // 将 .NET 的错误码转成用户友好的提示
                string friendlyMsg = ex.HttpRequestError switch
                {
                    System.Net.Http.HttpRequestError.NameResolutionError => "DNS 解析失败，请检查服务器地址和网络",
                    System.Net.Http.HttpRequestError.ConnectionError => "无法连接到服务器，请检查网络或服务器是否在线",
                    System.Net.Http.HttpRequestError.SecureConnectionError => "SSL/TLS 安全连接失败",
                    _ => $"网络请求异常：{ex.Message}"
                };
                return HttpResult<string>.Fail(HttpErrorType.NetworkError, friendlyMsg, ex.Message);
            }
            catch (Exception ex)
            {
                // 其他未预料异常
                return HttpResult<string>.Fail(HttpErrorType.Unknown, $"未知异常：{ex.Message}", ex.ToString());
            }
        }
        /// <summary>
        /// 计算重试延迟（指数退避）
        /// 第1次重试: BaseDelay × 2^1 = 1s × 2 = 2s
        /// 第2次重试: BaseDelay × 2^2 = 1s × 4 = 4s
        /// 第3次重试: BaseDelay × 2^3 = 1s × 8 = 8s
        /// 上限 30 秒
        /// </summary>
        private int CalculateRetryDelay(int retryAttempt)
        {
            if (!_retryConfig.UseExponentialBackoff)
                return _retryConfig.BaseDelayMs;
            int delay = _retryConfig.BaseDelayMs * (int)Math.Pow(2, retryAttempt);
            return Math.Min(delay, 30_000);
        }
        public void Dispose() => _httpClient?.Dispose();
    }
}
