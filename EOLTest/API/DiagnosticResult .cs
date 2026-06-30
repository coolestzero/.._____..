// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EOLTest.API.APITester;

namespace EOLTest.API
{
    /// <summary>
    /// 诊断通信结果模型
    /// </summary>
    public class DiagnosticResult : ApiResult
    {
        public uint RequestId { get; set; }
        public uint ResponseId { get; set; }
        public byte LinId { get; set; }

        public new static DiagnosticResult Ok(string message = "", object data = null, string detailInfo = "")
        {
            return new DiagnosticResult
            {
                Success = true,
                Message = message,
                Data = data,
                DetailInfo = detailInfo
            };
        }

        public new static DiagnosticResult Fail(string errorCode, string message = "", string detailInfo = "")
        {
            return new DiagnosticResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                DetailInfo = detailInfo
            };
        }
    }

    /// <summary>
    /// 通信会话状态
    /// </summary>
    public class CommunicationSession
    {
        public uint DeviceId { get; set; }
        public uint ChannelId { get; set; }
        public uint FilterId { get; set; }
        public uint Protocol { get; set; }
        public uint RequestId { get; set; }
        public uint ResponseId { get; set; }
        public byte ExtAddress { get; set; }
        public uint TxFlags { get; set; }
        public bool IsConnected { get; set; }
        public AddressingType AddressingType { get; set; } = AddressingType.Physical;
    }
}
