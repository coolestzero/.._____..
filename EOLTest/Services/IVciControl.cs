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
    }
}
