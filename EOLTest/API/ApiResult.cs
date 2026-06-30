// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.API
{
    /// <summary>
    /// API调用结果模型
    /// </summary>
    public class ApiResult
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string DetailInfo { get; set; } // 保留原demo的详细信息用于调试

        public static ApiResult Ok(string message = "", object data = null, string detailInfo = "")
        {
            return new ApiResult
            {
                Success = true,
                Message = message,
                Data = data,
                DetailInfo = detailInfo
            };
        }

        public static ApiResult Fail(string errorCode, string message = "", string detailInfo = "")
        {
            return new ApiResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                DetailInfo = detailInfo
            };
        }
    }

    /// <summary>
    /// 设备信息模型
    /// </summary>
    public class DeviceInfo
    {
        public string FirmwareVersion { get; set; }
        public string DllVersion { get; set; }
        public string ApiVersion { get; set; }
        public string SerialNumber { get; set; }
        public uint Voltage { get; set; }
    }

    /// <summary>
    /// 诊断响应模型
    /// </summary>
    /// <summary>
    /// 诊断响应模型
    /// </summary>
    public class DiagnosticResponse
    {
        /// <summary>
        /// 发送方ECU ID（功能寻址时使用）
        /// </summary>
        public uint SenderId { get; set; }

        /// <summary>
        /// 响应数据（完整数据或去除CAN ID后的数据）
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public uint Timestamp { get; set; }

        /// <summary>
        /// 十六进制显示
        /// </summary>
        public string DataHex => Data != null ? BitConverter.ToString(Data).Replace("-", " ") : "";
    }
}
