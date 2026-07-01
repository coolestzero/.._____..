// Services/Impl/LoggedVciControl.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using EOLTest.API;
using EOLTest.Services.Aggregators;
using EOLTest.Services.Common;      // 假设 DiagnosticUtils 等在这里
using Serilog;

namespace EOLTest.Services.Impl
{
    /// <summary>
    /// VCI 控制装饰器：在底层通信前后自动记录车辆报文日志
    /// </summary>
    public class LoggedVciControl : IVciControl
    {
        private readonly IVciControl _inner;
        private readonly DataAggregator _data;

        public LoggedVciControl(IVciControl inner, DataAggregator data)
        {
            _inner = inner;
            _data = data;
        }

        // ========== 发送方法：投递前记录 ==========
        public async Task<ApiResult> SendCanPhyAsync(string reqMsg)
        {
            LogVehicle("发送", reqMsg);
            var result = await _inner.SendCanPhyAsync(reqMsg);
            return result;
        }

        public async Task<ApiResult> SendCanFuncAsync(string reqMsg)
        {
            LogVehicle("发送", reqMsg);
            var result = await _inner.SendCanFuncAsync(reqMsg);
            return result;
        }

        // ========== 接收方法：成功后提取 hex 报文并记录 ==========
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

        //public async Task<ApiResult> ReceiveMultipleAsync()
        //{
        //    var result = await _inner.ReceiveMultipleAsync();
        //    if (result.Success)
        //    {
        //        string hex = TryExtractHexData(result);
        //        if (!string.IsNullOrEmpty(hex))
        //            LogVehicle("接收", hex);
        //    }
        //    return result;
        //}

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

        // ---------- 辅助方法 ----------

        /// <summary>
        /// 向当前车辆日志写入一条报文记录，格式：时间 [方向] 报文内容
        /// 如果还没有车辆日志（vehicleLogger 为 null）则忽略
        /// </summary>
        private void LogVehicle(string direction, string message)
        {
            try
            {
                _data.vehicleLogger?.Information("[{Direction}] {Message}", direction, message);
            }
            catch (Exception ex)
            {
                // 日志记录失败不应影响主业务，输出到全局日志即可
                _data.sysLogger?.Error(ex, "写入车辆日志失败");
            }
        }

        /// <summary>
        /// 尝试从 ApiResult.Data 中提取 hex 字符串
        /// </summary>
        private string TryExtractHexData(ApiResult result)
        {
            try
            {
                if (result?.Data == null) return null;

                if (result.Data is DiagnosticResponse diagResp && diagResp.Data != null)
                    return DiagnosticUtils.ToHexString(diagResp.Data);

                if (result.Data is byte[] rawBytes && rawBytes.Length > 0)
                    return DiagnosticUtils.ToHexString(rawBytes);

                return result.Data.ToString(); // 兜底
            }
            catch
            {
                return null;
            }
        }
    }
}
