// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOLTest.API;

namespace EOLTest.Services
{
    public interface IVciControl
    {
        /// <summary>
        /// 电脑与VCI建立通讯
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> OpenVciAsync(string dllPath);
        /// <summary>
        /// 断开通讯
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> CloseVciAsync();
        /// <summary>
        /// 电脑与VCI初始化参数连接
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> InitVciAsync();
        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> ExitVciAsync();
        /// <summary>
        /// VCI连接模块
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> ConnectEcuAsync(string txId, string rxId, string linId);
        /// <summary>
        /// 关闭连接模块
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> DisConnectEcuAsync(uint filterId);
        /// <summary>
        /// 发送物理寻址指令
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> SendCanPhyAsync(string reqMsg);
        /// <summary>
        /// 发送功能寻址指令
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> SendCanFuncAsync(string reqMsg);
        /// <summary>
        /// 接受指令
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> ReceiveAsync();
        /// <summary>
        /// 启动周期性消息发送
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> StartPeriodicMsgAsync(string txId, string rxId, string linId);
        /// <summary>
        /// 停止周期性消息发送
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> StopPeriodicMsgAsync();
        /// <summary>
        /// 读版本信息
        /// </summary>
        /// <returns></returns>
        Task<ApiResult> ReadVerAsync();

        // <summary>
        /// 物理寻址发送并等待最终响应（自动处理 78 pending、超时重试等）
        /// </summary>
        /// <param name="reqMsg">诊断请求十六进制字符串</param>
        /// <param name="overallTimeoutMs">单次尝试的总超时（毫秒），默认 20000</param>
        /// <param name="maxRetries">最大尝试次数（含第一次），默认 3</param>
        /// <param name="sendToRecvDelayMs">发送后等待时间（毫秒），默认 20</param>
        /// <returns>最终响应</returns>
        Task<ApiResult> SendAndWaitPhyAsync(string reqMsg,
                                            uint overallTimeoutMs = 20000,
                                            int maxRetries = 3,
                                            int sendToRecvDelayMs = 20);
        /// <summary>
        /// 功能寻址（单模块）发送并等待最终响应
        /// </summary>
        Task<ApiResult> SendAndWaitFuncAsync(string reqMsg,
                                             uint overallTimeoutMs = 20000,
                                             int maxRetries = 3,
                                             int sendToRecvDelayMs = 20);
    }
}
