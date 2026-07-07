// Copyright (c) TBD 2026.

// 用途：在线配置完整业务流程，含容错、重试、延时、结果封装
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using EOLTest.Models;
using EOLTest.Services.Common;
using Serilog;

namespace EOLTest.Services.Impl
{
    public class OnlineConfigService
    {
        private readonly IHttpService _httpService;
        private readonly OnlineConfigSettings _settings;
        private readonly ILogger _logger;
        public OnlineConfigService(IHttpService httpService, OnlineConfigSettings settings)
        {
            _httpService = httpService;
            _settings = settings;
            _logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.File(
            path: "Log/Socket/XmlSocket.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        .CreateLogger()
        .ForContext<OnlineConfigService>();
        }
        /// <summary>
        /// 执行在线配置，返回包含 Vehicle 的结果
        /// </summary>
        public async Task<OnlineConfigResult> ExecuteAsync(string vin, CancellationToken ct = default)
        {
            _logger.Information("[在线配置] 开始，VIN={Vin}", vin);
            var (ip, port) = _settings.GetServerAddress();
            string baseUrl = $"http://{ip}:{port}";
            string pnetUrl = $"{baseUrl}/pnet";
            // 1. 获取 Session
            var sessionResult = await GetSessionAsync(pnetUrl, ct);
            if (!sessionResult.Success)
            {
                _logger.Error("[在线配置] 获取Session失败: {Error}", sessionResult.ErrorMessage);
                return OnlineConfigResult.Fail(sessionResult);
            }
            string session = sessionResult.Data;
            _logger.Information("[在线配置] Session={Session}", session);

            // 2. 获取配置文件下载地址
            var urlResult = await GetConfigFileUrlAsync(pnetUrl, vin, session, ct);
            if (!urlResult.Success)
            {
                _logger.Error("[在线配置] 获取文件URL失败: {Error}", urlResult.ErrorMessage);
                return OnlineConfigResult.Fail(urlResult);
            }
            string fileUrl = urlResult.Data;
            _logger.Information("[在线配置] 文件URL={Url}", fileUrl);

            // 3. 下载配置文件
            var xmlResult = await DownloadConfigFileAsync(fileUrl, ct);
            if (!xmlResult.Success)
            {
                _logger.Error("[在线配置] 下载配置文件失败: {Error}", xmlResult.ErrorMessage);
                return OnlineConfigResult.Fail(xmlResult);
            }
            string configXml = xmlResult.Data;
            _logger.Information("[在线配置] 配置文件下载完成，大小 {Size} 字节", configXml.Length);
            // 4. 解析并构建 Vehicle
            var parseResult = ParseAndBuildVehicle(configXml, vin);
            if (!parseResult.Success)
            {
                _logger.Error("[在线配置] 解析失败: {Error}", parseResult.ErrorMessage);
            }
            else
            {
                _logger.Information("[在线配置] 完成，车型={CarType}, ECU数={Count}",
                    parseResult.Vehicle.CxName, parseResult.Vehicle.EcuModules?.Count ?? 0);
            }
            return parseResult;
        }
        #region 各步骤
        private async Task<HttpResult<string>> GetSessionAsync(string pnetUrl, CancellationToken ct)
        {
            string requestXml = $@"<tel>
    <module>verify</module>
    <action>request</action>
    <system>VMT</system>
    <sysnr>12</sysnr>
    <serial_no>123456</serial_no>
</tel>";
            _logger.Debug("[在线配置] >>> 发送 verify 请求");
            HttpResult<string> result = await _httpService.PostXmlAsync(pnetUrl, requestXml, ct);
            if (!result.Success) return result;
            try
            {
                XDocument doc = XDocument.Parse(result.Data);
                string session = doc.Root?.Element("data")?.Value;
                if (string.IsNullOrEmpty(session))
                    return HttpResult<string>.Fail(HttpErrorType.ParseError, "未找到 <data> 节点", result.Data.Truncate(200));
                return HttpResult<string>.Ok(session);
            }
            catch (Exception ex)
            {
                return HttpResult<string>.Fail(HttpErrorType.ParseError, "Session 响应解析失败", ex.Message);
            }
        }
        private async Task<HttpResult<string>> GetConfigFileUrlAsync(string pnetUrl, string vin, string session, CancellationToken ct)
        {
            string requestXml = $@"<tel>
    <module>vcf</module>
    <action>request</action>
    <vin>{vin}</vin>
    <system>vmt</system>
    <sysnr>12</sysnr>
    <session>{session}</session>
    <timestamp>{DateTime.Now:yyyy-MM-dd}</timestamp>
</tel>";
            _logger.Debug("[在线配置] >>> 发送 vcf 请求");
            HttpResult<string> result = await _httpService.PostXmlAsync(pnetUrl, requestXml, ct);
            if (!result.Success) return result;
            try
            {
                XDocument doc = XDocument.Parse(result.Data);
                string fileName = doc.Root?.Element("fileName")?.Value;
                if (string.IsNullOrEmpty(fileName))
                    return HttpResult<string>.Fail(HttpErrorType.ParseError, "未找到 <fileName> 节点", result.Data.Truncate(200));
                return HttpResult<string>.Ok(fileName);
            }
            catch (Exception ex)
            {
                return HttpResult<string>.Fail(HttpErrorType.ParseError, "fileName 响应解析失败", ex.Message);
            }
        }
        private async Task<HttpResult<string>> DownloadConfigFileAsync(string fileUrl, CancellationToken ct)
        {
            if (!fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var (ip, port) = _settings.GetServerAddress();
                fileUrl = $"http://{ip}:{port}/{fileUrl.TrimStart('/')}";
            }
            _logger.Debug("[在线配置] >>> 下载配置文件 {Url}", fileUrl);
            return await _httpService.GetStringAsync(fileUrl, ct);
        }
        private OnlineConfigResult ParseAndBuildVehicle(string xml, string vin)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(OnlineConfigInfo));
                using var reader = new StringReader(xml);
                var config = (OnlineConfigInfo)serializer.Deserialize(reader);
                if (config == null)
                    return OnlineConfigResult.Fail("配置文件反序列化结果为空");
                if (config.Result != "OK")
                {
                    _logger.Warning("[在线配置] result={Result}（非OK）", config.Result);
                    return OnlineConfigResult.Fail($"服务器返回异常状态：{config.Result}");
                }
                var vehicle = new Vehicle
                {
                    Vin = vin,
                    CxName = config.CarType ?? string.Empty,
                    Vsn = config.Vsn ?? string.Empty,
                };

                var ecuModules = new ObservableCollection<EcuModule>();
                var moduleDict = new Dictionary<string, EcuModule>();
                string[] pnsEntries = config.Pns.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string entry in pnsEntries)
                {
                    // 按等号分割
                    string[] parts = entry.Split(new[] { '=' }, StringSplitOptions.None);
                    //判断等号两边数组长度是否为2，并且右边不为空
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        string moduleName = parts[0].Trim();    //等号左边为模块名
                        string partNumber = parts[1].Trim();    //等号右边为零件号
                        moduleDict[moduleName] = new EcuModule
                        {
                            EcuName = moduleName,
                            PartNumber = partNumber,
                            OnlineConfig = string.Empty  
                        };
                    }

                    if (config.EcuList != null)
                    {
                        foreach (var item in config.EcuList)
                        {
                            string ecuName = item.EcuName?.Trim();
                            if (string.IsNullOrEmpty(ecuName)) continue;            // 检查是否为空

                            if (moduleDict.TryGetValue(ecuName, out EcuModule module))  // 如果字典中存在该 ECU 模块就获取
                            {
                                // 获取 data 字符串
                                module.OnlineConfig = item.Codes?.Data ?? string.Empty;
                            }
                        }
                    }
                }
                vehicle.EcuModules = new ObservableCollection<EcuModule>(moduleDict.Values);
                return OnlineConfigResult.Ok(vehicle);
            }
            catch (InvalidOperationException ex)
            {
                return OnlineConfigResult.Fail($"XML 结构不匹配：{ex.Message}", ex.ToString());
            }
            catch (Exception ex)
            {
                return OnlineConfigResult.Fail($"XML 解析异常：{ex.Message}", ex.ToString());
            }
        }


        /// <summary>
        /// 【仅限测试】从本地 XML 文件加载并解析在线配置，不经过网络。
        /// 用法：在 ViewModel 中调用此方法替代 ExecuteAsync，方便离线调试。
        /// </summary>
        /// <param name="filePath">XML 文件完整路径，例如 @"C:\Test\LK6ADAE2XSB997940.xml"</param>
        /// <param name="vin">车辆 VIN</param>
        /// <returns>解析结果</returns>
        public OnlineConfigResult LoadLocalConfigFile(string filePath, string vin)
        {
            try
            {
                string xml = File.ReadAllText(filePath, Encoding.UTF8);
                _logger.Information("[在线配置-本地] 成功读取文件 {FilePath}，大小 {Size} 字节", filePath, xml.Length);

                // 直接调用已有的解析逻辑
                return ParseAndBuildVehicle(xml, vin);
            }
            catch (FileNotFoundException)
            {
                _logger.Error("[在线配置-本地] 文件不存在: {FilePath}", filePath);
                return OnlineConfigResult.Fail($"文件不存在：{filePath}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[在线配置-本地] 读取文件异常: {FilePath}", filePath);
                return OnlineConfigResult.Fail($"文件读取失败：{ex.Message}", ex.ToString());
            }
        }
        private string NormalizeEcuName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return rawName;
            return rawName.Replace("_", "").ToUpper();
        }
        #endregion


    }
}
