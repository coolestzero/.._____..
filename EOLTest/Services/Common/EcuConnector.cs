// 功能：封装 ECU 连接通用逻辑
// 负责：查询 CAN ID → 连接 ECU → 返回统一结果
// 所有 Function（DTC、VIN、IMMO 等）均可复用此类

using System;
using System.Threading.Tasks;
using EOLTest.API;
using EOLTest.Models;
using EOLTest.Services.Mdb;
using Serilog;

namespace EOLTest.Services.Common
{
    /// <summary>
    /// ECU 连接结果 —— 封装一次连接尝试的完整信息
    /// </summary>
    public class EcuConnectionResult
    {
        /// <summary>ECU 名称</summary>
        public string EcuName { get; set; }

        /// <summary>查询到的 CAN ID 信息（连接失败时为 null）</summary>
        public EcuCanId CanId { get; set; }

        /// <summary>连接成功时为 ConnectEcuAsync 返回的 ApiResult</summary>
        public ApiResult ConnectResult { get; set; }

        /// <summary>整体是否成功（CAN ID 查到 + 连接成功）</summary>
        public bool Success { get; set; }

        /// <summary>失败时的错误信息</summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// ECU 连接器 —— 封装「查 CAN ID + 连接 ECU」的通用流程
    ///
    /// 使用方式（通过 DI 注入或手动创建）：
    ///   var connector = new EcuConnector(diagnosticService, vciControl, logShow, logger);
    ///   var result = await connector.ConnectToEcuAsync(ecuModule);
    ///   if (result.Success) { /* 使用 result.ConnectResult.Data 作为 filterId */ }
    /// </summary>
    public class EcuConnector
    {
        private readonly IDiagnosticService _diagnostic;
        private readonly IVciControl _vci;
        private readonly ILogShowService _logshow;
        private readonly ILogger _logger;

        public EcuConnector(
            IDiagnosticService diagnostic,
            IVciControl vci,
            ILogShowService logshow,
            ILogger logger)
        {
            _diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
            _vci = vci ?? throw new ArgumentNullException(nameof(vci));
            _logshow = logshow ?? throw new ArgumentNullException(nameof(logshow));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 对单个 ECU 执行完整连接流程：查 CAN ID → 连接
        /// </summary>
        /// <param name="ecu">目标 ECU 模块</param>
        /// <returns>连接结果，包含成功/失败信息和 filterId</returns>
        public async Task<EcuConnectionResult> ConnectToEcuAsync(EcuModule ecu)
        {
            if (ecu == null)
                throw new ArgumentNullException(nameof(ecu));

            var result = new EcuConnectionResult { EcuName = ecu.EcuName };

            // 1. 从数据库查询该 ECU 的 CAN 通信 ID
            _logger.Information("── 正在连接 {EcuName} ──", ecu.EcuName);

            var canId = await _diagnostic.GetEcuCanIdAsync(ecu.EcuName);
            if (canId == null)
            {
                _logger.Warning("⚠️ 未找到 {EcuName} 的 CAN ID，跳过", ecu.EcuName);
                result.Success = false;
                result.ErrorMessage = "未找到 CAN ID";
                return result;
            }

            result.CanId = canId;

            // 2. 连接 ECU
            var connectResult = await _vci.ConnectEcuAsync(canId.TxId, canId.RxId, canId.LinId);
            result.ConnectResult = connectResult;

            if (!connectResult.Success)
            {
                _logshow.AddLog("FAIL", $"❌ 连接 {ecu.EcuName} 失败: {connectResult.Message}");
                _logger.Error("❌ 连接 {EcuName} 失败: {Message}", ecu.EcuName, connectResult.Message);
                result.Success = false;
                result.ErrorMessage = connectResult.Message;
                return result;
            }

            _logger.Information("✅ 已连接 {EcuName}，过滤器ID：{FilterId}", ecu.EcuName, connectResult.Data);
            result.Success = true;
            return result;
        }
    }
}
