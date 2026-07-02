// Services/Impl/LoggedVciControl.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EOLTest.API;
using EOLTest.Services.Aggregators;
using EOLTest.Services.Common;
using Serilog;

namespace EOLTest.Services.Impl
{
    /// <summary>
    /// VCI 控制装饰器：在底层通信前后自动记录车辆报文日志，
    /// 并提供自动处理 78 (Response Pending) 的高级收发方法
    /// </summary>
    public class LoggedVciControl : IVciControl
    {
        private readonly IVciControl _inner;
        private readonly DataAggregator _data;

        // 78 pending 相关常量
        private const int PendingWindowMs = 5000;   // ECU 承诺的"最多5秒"
        private const int MaxPendingCount = 10;     // 防止死循环的安全阀

        public LoggedVciControl(IVciControl inner, DataAggregator data)
        {
            _inner = inner;
            _data = data;
        }

        // ========== 发送方法：投递前记录 ==========
        public async Task<ApiResult> SendCanPhyAsync(string reqMsg)
        {
            LogVehicle("发送", reqMsg);
            return await _inner.SendCanPhyAsync(reqMsg);
        }

        public async Task<ApiResult> SendCanFuncAsync(string reqMsg)
        {
            LogVehicle("发送", reqMsg);
            return await _inner.SendCanFuncAsync(reqMsg);
        }

        // ========== 基础接收方法：单次接收并记录 ==========
        public async Task<ApiResult> ReceiveAsync()
        {
            var result = await _inner.ReceiveAsync();
            if (result.Success)
            {
                string hex = TryExtractHexData(result);
                if (!string.IsNullOrEmpty(hex))
                    LogVehicle("接收", hex);
            }
            return result;
        }

        // ========== ★ 物理寻址 — 发送并等待最终响应（自动处理 78，支持重试） ==========
        public async Task<ApiResult> SendAndWaitPhyAsync(string reqMsg,
                                                          uint overallTimeoutMs = 20000,
                                                          int maxRetries = 3,
                                                          int sendToRecvDelayMs = 20)
        {
            byte requestSid = ExtractServiceId(reqMsg);
            if (requestSid == 0)
                return DiagnosticResult.Fail("INVALID_DATA", $"无法从请求中解析服务ID: {reqMsg}");

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                // 发送
                LogVehicle("发送", reqMsg);
                var sendResult = await _inner.SendCanPhyAsync(reqMsg);
                if (!sendResult.Success)
                {
                    if (attempt < maxRetries)
                    {
                        _data.sysLogger?.Warning($"发送失败（第{attempt}次），准备重试...");
                        await Task.Delay(100);
                        continue;
                    }
                    return sendResult;
                }

                if (sendToRecvDelayMs > 0)
                    await Task.Delay(sendToRecvDelayMs);

                int pendingCount = 0;
                DateTime? pendingSince = null;
                var attemptSw = Stopwatch.StartNew();

                while (attemptSw.ElapsedMilliseconds < overallTimeoutMs)
                {
                    var recvResult = await _inner.ReceiveAsync(); // 物理接收

                    if (recvResult.Success)
                    {
                        string hex = TryExtractHexData(recvResult);
                        if (!string.IsNullOrEmpty(hex))
                            LogVehicle("接收", hex);

                        if (IsPendingResponse(recvResult, requestSid))
                        {
                            pendingCount++;
                            if (pendingCount > MaxPendingCount)
                            {
                                string errMsg = $"ECU连续回复78超过{MaxPendingCount}次，判定异常";
                                _data.sysLogger?.Error(errMsg);
                                return DiagnosticResult.Fail("PENDING_EXCEEDED", errMsg);
                            }

                            pendingSince = DateTime.Now;
                            _data.sysLogger?.Verbose($"收到第{pendingCount}次78 pending，等待最多{PendingWindowMs}ms...");
                            continue;
                        }

                        // 收到最终响应（肯定或非78否定）
                        return recvResult;
                    }
                    else
                    {
                        if (pendingSince.HasValue &&
                            (DateTime.Now - pendingSince.Value).TotalMilliseconds > PendingWindowMs)
                        {
                            _data.sysLogger?.Warning($"收到78后{PendingWindowMs}ms内未收到回复，退出本轮等待");
                            break;
                        }
                        await Task.Delay(30);
                    }
                }
                attemptSw.Stop();

                if (attempt < maxRetries)
                {
                    _data.sysLogger?.Warning($"接收超时（第{attempt}次尝试，已等{attemptSw.ElapsedMilliseconds}ms），准备重试...");
                    await Task.Delay(50);
                }
            }

            string timeoutMsg = $"物理寻址：重试{maxRetries}次后仍未收到最终响应";
            _data.sysLogger?.Warning(timeoutMsg);
            return DiagnosticResult.Fail("TIMEOUT", timeoutMsg);
        }

        // ========== ★ 功能寻址（单模块）— 发送并等待最终响应 ==========
        public async Task<ApiResult> SendAndWaitFuncAsync(string reqMsg,
                                                           uint overallTimeoutMs = 20000,
                                                           int maxRetries = 3,
                                                           int sendToRecvDelayMs = 20)
        {
            byte requestSid = ExtractServiceId(reqMsg);
            if (requestSid == 0)
                return DiagnosticResult.Fail("INVALID_DATA", $"无法从请求中解析服务ID: {reqMsg}");

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                LogVehicle("发送", reqMsg);
                var sendResult = await _inner.SendCanFuncAsync(reqMsg);
                if (!sendResult.Success)
                {
                    if (attempt < maxRetries)
                    {
                        _data.sysLogger?.Warning($"发送失败（第{attempt}次），准备重试...");
                        await Task.Delay(100);
                        continue;
                    }
                    return sendResult;
                }

                if (sendToRecvDelayMs > 0)
                    await Task.Delay(sendToRecvDelayMs);

                int pendingCount = 0;
                DateTime? pendingSince = null;
                var attemptSw = Stopwatch.StartNew();

                while (attemptSw.ElapsedMilliseconds < overallTimeoutMs)
                {
                    // 功能寻址单模块：直接复用物理接收（因为过滤器已限定响应 ID）
                    var recvResult = await _inner.ReceiveAsync();

                    if (recvResult.Success)
                    {
                        string hex = TryExtractHexData(recvResult);
                        if (!string.IsNullOrEmpty(hex))
                            LogVehicle("接收", hex);

                        if (IsPendingResponse(recvResult, requestSid))
                        {
                            pendingCount++;
                            if (pendingCount > MaxPendingCount)
                            {
                                string errMsg = $"ECU连续回复78超过{MaxPendingCount}次，判定异常";
                                _data.sysLogger?.Error(errMsg);
                                return DiagnosticResult.Fail("PENDING_EXCEEDED", errMsg);
                            }

                            pendingSince = DateTime.Now;
                            _data.sysLogger?.Verbose($"收到第{pendingCount}次78 pending，等待最多{PendingWindowMs}ms...");
                            continue;
                        }

                        return recvResult;
                    }
                    else
                    {
                        if (pendingSince.HasValue &&
                            (DateTime.Now - pendingSince.Value).TotalMilliseconds > PendingWindowMs)
                        {
                            _data.sysLogger?.Warning($"收到78后{PendingWindowMs}ms内未收到回复，退出本轮等待");
                            break;
                        }
                        await Task.Delay(30);
                    }
                }
                attemptSw.Stop();

                if (attempt < maxRetries)
                {
                    _data.sysLogger?.Warning($"接收超时（第{attempt}次尝试，已等{attemptSw.ElapsedMilliseconds}ms），准备重试...");
                    await Task.Delay(50);
                }
            }

            string timeoutMsg = $"功能寻址：重试{maxRetries}次后仍未收到最终响应";
            _data.sysLogger?.Warning(timeoutMsg);
            return DiagnosticResult.Fail("TIMEOUT", timeoutMsg);
        }

        // ========== 其他方法：直接透传 ==========
        public Task<ApiResult> OpenVciAsync(string dllPath) => _inner.OpenVciAsync(dllPath);
        public Task<ApiResult> InitVciAsync() => _inner.InitVciAsync();
        public Task<ApiResult> ReadVerAsync() => _inner.ReadVerAsync();
        public Task<ApiResult> ConnectEcuAsync(string txId, string rxId, string linId) => _inner.ConnectEcuAsync(txId, rxId, linId);
        public Task<ApiResult> DisConnectEcuAsync(uint filterId) => _inner.DisConnectEcuAsync(filterId);
        public Task<ApiResult> StartPeriodicMsgAsync(string txId, string rxId, string linId) => _inner.StartPeriodicMsgAsync(txId, rxId, linId);
        public Task<ApiResult> StopPeriodicMsgAsync() => _inner.StopPeriodicMsgAsync();
        public Task<ApiResult> CloseVciAsync() => _inner.CloseVciAsync();
        public Task<ApiResult> ExitVciAsync() => _inner.ExitVciAsync();

        // ========== 辅助方法 ==========

        private void LogVehicle(string direction, string message)
        {
            try
            {
                _data.vehicleLogger?.Information("[{Direction}] {Message}", direction, message);
            }
            catch (Exception ex)
            {
                _data.sysLogger?.Error(ex, "写入车辆日志失败");
            }
        }

        private string TryExtractHexData(ApiResult result)
        {
            try
            {
                if (result?.Data == null) return null;

                if (result.Data is DiagnosticResponse diagResp && diagResp.Data != null)
                    return DiagnosticUtils.ToHexString(diagResp.Data);

                if (result.Data is List<DiagnosticResponse> diagList && diagList.Count > 0)
                {
                    var parts = diagList.Select(r =>
                        $"ECU 0x{r.SenderId:X3}: {DiagnosticUtils.ToHexString(r.Data)}");
                    return string.Join(" | ", parts);
                }

                if (result.Data is byte[] rawBytes && rawBytes.Length > 0)
                    return DiagnosticUtils.ToHexString(rawBytes);

                return result.Data.ToString();
            }
            catch
            {
                return null;
            }
        }

        private byte ExtractServiceId(string hexData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hexData)) return 0;
                string cleaned = hexData.Replace(" ", "").Replace("-", "");
                if (cleaned.Length < 2) return 0;
                return Convert.ToByte(cleaned.Substring(0, 2), 16);
            }
            catch
            {
                return 0;
            }
        }

        private bool IsPendingResponse(ApiResult result, byte requestSid)
        {
            try
            {
                if (result?.Data == null) return false;

                byte[] data = null;

                if (result.Data is DiagnosticResponse diagResp)
                    data = diagResp.Data;
                else if (result.Data is List<DiagnosticResponse> diagList)
                {
                    // 功能寻址多响应时才走这里，现在单模块不会用到
                    bool allPending = diagList.Count > 0;
                    foreach (var resp in diagList)
                    {
                        if (!Contains78Pattern(resp.Data, requestSid))
                        {
                            allPending = false;
                            break;
                        }
                    }
                    return allPending;
                }
                else if (result.Data is byte[] raw)
                    data = raw;

                if (data == null || data.Length < 3) return false;
                return Contains78Pattern(data, requestSid);
            }
            catch
            {
                return false;
            }
        }

        private bool Contains78Pattern(byte[] data, byte requestSid)
        {
            for (int i = 0; i < data.Length - 2; i++)
            {
                if (data[i] == 0x7F && data[i + 1] == requestSid && data[i + 2] == 0x78)
                    return true;
            }
            return false;
        }
    }
}
