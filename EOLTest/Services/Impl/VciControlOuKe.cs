// Copyright (c) TBD 2026.

using System;
using System.Threading.Tasks;
using EOLTest.API;
using static EOLTest.API.APITester; 

namespace EOLTest.Services.Impl
{
    /// <summary>
    /// VCI 控制实现类
    /// 封装底层 APITester 诊断通信接口，提供面向业务的异步方法
    /// </summary>
    public class VciControlOuKe : IVciControl
    {
        private readonly APITester _api;

        public VciControlOuKe(APITester api)
        {
            _api = api;
        }

        #region 设备生命周期

        /// <summary>
        /// 打开VCI设备（加载库 + 打开设备）
        /// </summary>
        public async Task<ApiResult> OpenVciAsync(string dllPath)
        {
            // 使用 Task.Run<ApiResult> 指定返回类型，内部可返回 DiagnosticResult（子类）
            return await Task.Run<ApiResult>(() =>
            {
                var result = _api.LoadLibrary(dllPath);
                if (!result.Success) return result; // ApiResult 直接返回
                return _api.Open(); // APIResult，Open 返回 ApiResult 不是 DiagnosticResult
            });
        }

        /// <summary>
        /// 初始化VCI（设置参数）
        /// </summary>
        public async Task<ApiResult> InitVciAsync()
        {
            return await Task.Run<ApiResult>(() => _api.OpenChannel());
        }

        /// <summary>
        /// 读取版本信息
        /// </summary>
        public async Task<ApiResult> ReadVerAsync()
        {
            return await Task.Run<ApiResult>(() => _api.ReadVer());
        }

        #endregion

        #region ECU 连接（物理寻址 & 功能寻址）

        /// <summary>
        /// 连接ECU（物理寻址）
        /// 设置当前模块、添加硬件过滤器
        /// </summary>
        /// <param name="txId">请求 CAN ID（十六进制字符串）</param>
        /// <param name="rxId">响应 CAN ID（十六进制字符串）</param>
        /// <param name="linId">扩展地址/LIN ID（十六进制字符串），无则为 "0"</param>
        public async Task<ApiResult> ConnectEcuAsync(string txId, string rxId, string linId)
        {
            return await Task.Run<ApiResult>(() =>
            {
                // 设置当前操作的模块为物理寻址
                var moduleResult = _api.SetActiveModule(txId, rxId, linId, AddressingType.Physical);
                if (!moduleResult.Success) return moduleResult;

                // 为该模块添加硬件过滤器
                var filterResult = _api.AddModuleFilter(txId, rxId, linId, AddressingType.Physical);
                // 即使过滤器失败，也可以继续（某些硬件不强制要求过滤器），这里选择返回其状态
                if (!filterResult.Success) return filterResult;
                //data是返回的filteID
                // 返回成功状态，附加简要信息
                return DiagnosticResult.Ok("物理寻址连接成功", filterResult.Data,
                    $"TxId:{txId}, RxId:{rxId}, LinId:{linId}");
            });
        }

        /// <summary>
        /// 连接ECU（功能寻址）
        /// 设置当前模块为功能寻址、添加过滤器
        /// 注意：功能寻址的请求会发到广播 ID 0x7DF，响应可能来自多个 ECU
        /// </summary>
        /// <param name="txId">请求 CAN ID（占位，功能寻址实际使用 0x7DF）</param>
        /// <param name="rxId">期望接收响应的物理响应 ID</param>
        /// <param name="linId">扩展地址（必须为 "0"）</param>
        public async Task<ApiResult> ConnectEcuFunctionalAsync(string txId, string rxId, string linId)
        {
            return await Task.Run<ApiResult>(() =>
            {
                var moduleResult = _api.SetActiveModule(txId, rxId, linId, AddressingType.Functional);
                if (!moduleResult.Success) return moduleResult;

                var filterResult = _api.AddModuleFilter(txId, rxId, linId, AddressingType.Functional);
                if (!filterResult.Success) return filterResult;
                //data是返回的filteID
                return DiagnosticResult.Ok("功能寻址连接成功", filterResult.Data,
                    $"RxId:{rxId}, LinId:{linId}");
            });
        }

        /// <summary>
        /// 断开ECU连接（关闭通道，自动停止所有过滤器和周期性消息）
        /// </summary>
        public async Task<ApiResult> DisConnectEcuAsync(uint filterId)
        {
            return await Task.Run<ApiResult>(() =>
            {
                var result = _api.StopFilter(filterId); // 返回 DiagnosticResult
                return result;
            });
        }

        #endregion

        #region 数据收发

        /// <summary>
        /// 物理寻址发送
        /// 前提：必须先通过 ConnectEcuAsync 设置物理寻址模块
        /// </summary>
        public async Task<ApiResult> SendCanPhyAsync(string reqMsg)
        {
            return await Task.Run<ApiResult>(() =>
            {
                return _api.SendPhysical(reqMsg); // 返回 DiagnosticResult
            });
        }

        /// <summary>
        /// 功能寻址发送
        /// 前提：必须先通过 ConnectEcuFunctionalAsync 设置功能寻址模块
        /// </summary>
        public async Task<ApiResult> SendCanFuncAsync(string reqMsg)
        {
            return await Task.Run<ApiResult>(() =>
            {
                return _api.SendFunctional(reqMsg);
            });
        }

        /// <summary>
        /// 接收单个响应（物理寻址）
        /// </summary>
        public async Task<ApiResult> ReceiveAsync()
        {
            return await Task.Run<ApiResult>(() =>
            {
                return _api.ReceivePhysicalResponse(); // 返回 DiagnosticResult
            });
        }

        /// <summary>
        /// 接收多个响应（功能寻址）
        /// </summary>
        public async Task<ApiResult> ReceiveMultipleAsync()
        {
            return await Task.Run<ApiResult>(() =>
            {
                return _api.ReceiveFunctionalResponses(); // 返回 DiagnosticResult
            });
        }

        // 在 VciControlOuKe 中添加（简单透传，真正的重试逻辑在 LoggedVciControl 装饰器中）
        public async Task<ApiResult> SendAndWaitPhyAsync(string reqMsg,
                                                          uint overallTimeoutMs = 20000,
                                                          int maxRetries = 3,
                                                          int sendToRecvDelayMs = 20)
        {
            // 基础实现：不处理78，不自动重试，仅封装一次收发流程
            // 真正的重试和78处理由 LoggedVciControl 装饰器提供
            return await Task.Run<ApiResult>(() =>
            {
                var sendResult = _api.SendPhysical(reqMsg);
                if (!sendResult.Success) return sendResult;
                return _api.ReceivePhysicalResponse();
            });
        }

        public async Task<ApiResult> SendAndWaitFuncAsync(string reqMsg,
                                                           uint overallTimeoutMs = 20000,
                                                           int maxRetries = 3,
                                                           int sendToRecvDelayMs = 20)
        {
            return await Task.Run<ApiResult>(() =>
            {
                var sendResult = _api.SendFunctional(reqMsg);
                if (!sendResult.Success) return sendResult;
                return _api.ReceiveFunctionalResponses(); 
            });
        }

        #endregion

        #region 周期性消息

        /// <summary>
        /// 启动周期性消息（例如 Tester Present 3E 80）
        /// 内部会根据参数设置当前模块，并自动添加过滤器
        /// </summary>
        /// <param name="txId">请求 CAN ID</param>
        /// <param name="rxId">响应 CAN ID</param>
        /// <param name="linId">扩展地址/LIN ID</param>
        public async Task<ApiResult> StartPeriodicMsgAsync(string txId, string rxId, string linId)
        {
            return await Task.Run<ApiResult>(() =>
            {
                // 设置当前模块（物理寻址，周期性消息通常用于物理通道保持会话）
                var moduleResult = _api.SetActiveModule(txId, rxId, linId, AddressingType.Physical);
                if (!moduleResult.Success) return moduleResult;

                // 启动周期性消息（新底层 StartPeriodicMsg 无参，会自动添加过滤器）
                var startResult = _api.StartPeriodicMsg(); // 此方法返回 ApiResult（不是 DiagnosticResult）
                return startResult;
            });
        }

        /// <summary>
        /// 停止周期性消息（不关闭通道，保留过滤器）
        /// </summary>
        public async Task<ApiResult> StopPeriodicMsgAsync()
        {
            return await Task.Run<ApiResult>(() =>
            {
                return _api.StopPeriodicMsg(); // 返回 ApiResult
            });
        }

        #endregion

        #region 关闭与退出

        /// <summary>
        /// 关闭VCI设备（关闭通道 + 关闭设备）
        /// 注意：这不会释放 DLL，可再次打开设备
        /// </summary>
        public async Task<ApiResult> CloseVciAsync()
        {
            return await Task.Run<ApiResult>(() =>
            {
                // 先关闭通道（安全关闭，内部会停过滤器）
                _api.CloseChannel();
                _api.Dispose();
                // 再关闭设备
                return _api.CloseDevice(); // 返回 ApiResult
            });
        }

        /// <summary>
        /// 退出通道（关闭通道）
        /// </summary>
        public async Task<ApiResult> ExitVciAsync()
        {
            return await Task.Run<ApiResult>(() =>
            {
                var deviceResult = _api.CloseChannel();
                return deviceResult;
            });
        }

        #endregion
    }
}
