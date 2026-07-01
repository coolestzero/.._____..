// 功能：全车 DTC（故障码）读取
// 流程：初始化 VCI → 遍历每个 ECU → 查 CAN ID → 连接 → 发送 190208 → 接收 → 解析 → 断开

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EOLTest.API;
using EOLTest.Models;
using EOLTest.Services.Aggregators;
using EOLTest.Services.Common;
using EOLTest.Services.Mdb;
using Serilog;

namespace EOLTest.Services.Function
{
    internal class DtcFunction : IFunction
    {
        private readonly IVciControl _vci;
        private readonly DataAggregator _data;        // 数据相关

        public string FunctionName => "DTC";

        // ── 当前车辆引用（由调用方设置） ──
        private Vehicle _currentVehicle;

        // ── DTC 读取结果集合（供 ViewModel 绑定到 UI） ──
        public ObservableCollection<EcuDtcResult> DtcResults { get; } = new ObservableCollection<EcuDtcResult>();

        public DtcFunction(IVciControl vci, DataAggregator dataService)
        {
            _vci = vci;
            _data = dataService;
        }

        /// <summary>
        /// 在执行功能前设置目标车辆
        /// </summary>
        public void SetVehicle(Vehicle vehicle)
        {
            _currentVehicle = vehicle;
        }

        /// <summary>
        /// 入口：执行 DTC 功能
        /// </summary>
        public async Task<bool> ExecuteFunc()
        {
            if (_currentVehicle == null)
            {
                _data.logshow.AddLog("ERROR", "❌ DTC 功能未设置车辆信息");
                return false;
            }

            return await DtcCheck(_currentVehicle);
        }

        /// <summary>
        /// [核心] 对指定车辆执行全车 DTC 检查
        /// </summary>
        /// <param name="vehicle">包含所有 ECU 模块信息的车辆对象</param>
        /// <returns>全部成功返回 true，任一失败返回 false</returns>
        public async Task<bool> DtcCheck(Vehicle vehicle)
        {
            if (vehicle?.EcuModules == null || vehicle.EcuModules.Count == 0)
            {
                _data.logshow.AddLog("WARN", "⚠️ 车辆没有 ECU 模块信息");
                return false;
            }

            bool allSuccess = true;
            DtcResults.Clear();

            // 1. 初始化 VCI 设备（只做一次）
            ApiResult result = await _vci.InitVciAsync();
            if (!result.Success)
            {
                _data.logshow.AddLog("FAIL", "❌ VCI 初始化失败，无法读取 DTC");
                _data.sysLogger?.Error("VCI 初始化失败: {Message}", result.Message);
                return false;
            }
            _data.sysLogger?.Information("✅ VCI 初始化成功");

            // 2. 遍历所有 ECU
            foreach (var ecu in vehicle.EcuModules)
            {
                _data.sysLogger?.Information($"── 正在读取 {ecu.EcuName} 故障码 ──");

                // 2a. 从数据库查询该 ECU 的 CAN 通信 ID
                var ecuId = await _data.diagnostic.GetEcuCanIdAsync(ecu.EcuName);
                if (ecuId == null)
                {
                    _data.sysLogger?.Warning($"⚠️ 未找到 {ecu.EcuName} 的 CAN ID，跳过");
                    DtcResults.Add(new EcuDtcResult
                    {
                        EcuName = ecu.EcuName,
                        Success = false,
                        ErrorMessage = "未找到 CAN ID"
                    });
                    allSuccess = false;
                    continue;
                }

                // 2b. 连接 ECU
                result = await _vci.ConnectEcuAsync(ecuId.TxId, ecuId.RxId, ecuId.LinId);
                if (!result.Success)
                {
                    _data.logshow.AddLog("FAIL", $"❌ 连接 {ecu.EcuName} 失败: {result.Message}");
                    _data.sysLogger?.Error($"❌ 连接 {ecu.EcuName} 失败", result.Message);
                    DtcResults.Add(new EcuDtcResult
                    {
                        EcuName = ecu.EcuName,
                        Success = false,
                        ErrorMessage = result.Message
                    });
                    allSuccess = false;
                    continue;
                }
                _data.sysLogger?.Information($"✅ 已连接 {ecu.EcuName}，过滤器ID：{result.Data}");

                // 2c. 读取该 ECU 的 DTC（发送指令、接收、解析一体）
                var dtcResult = await ReadDtcFromSingleEcu(ecu.EcuName, "190208");
                DtcResults.Add(dtcResult);
                if (!dtcResult.Success) allSuccess = false;

                // 2d. 断开 ECU
                await _vci.DisConnectEcuAsync((uint)result.Data);
            }

            // 3. 输出汇总
            int successCount = DtcResults.Count(r => r.Success);
            int totalActive = DtcResults
            .Where(r => r.Success)
            .SelectMany(r => r.DtcList)
            .Count(d => d.IsActive);
            _data.sysLogger?.Information(
                $"DTC 读取完成: 总结果 {allSuccess}，{successCount}/{DtcResults.Count} ECU 成功，当前故障 {totalActive} 个");

            return allSuccess;
        }

        /// <summary>
        /// 对单个 ECU 发送 19 02 08 指令，接收响应并解析
        /// </summary>
        /// <param name="ecuName">ECU 名称（用于日志）</param>
        /// <param name="command">十六进制命令字符串，如 "190208"</param>
        /// <returns>该 ECU 的 DTC 解析结果</returns>
        private async Task<EcuDtcResult> ReadDtcFromSingleEcu(string ecuName, string command)
        {
            var result = new EcuDtcResult { EcuName = ecuName };

            try
            {
                // 发送命令
                //_data.logshow.AddLog("SEND", $"[{ecuName}] 发送: {command}");
                ApiResult sendResult = await _vci.SendCanPhyAsync(command);
                if (!sendResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = $"发送失败: {sendResult.Message}";
                    return result;
                }

                // 接收响应
                ApiResult recvResult = await _vci.ReceiveAsync();
                if (!recvResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = $"接收超时: {recvResult.Message}";
                    return result;
                }

                // 提取原始字节数组（从 DiagnosticResponse 或直接 byte[]）
                byte[] fullFrame = null;
                if (recvResult.Data is DiagnosticResponse diagResp)
                    fullFrame = diagResp.Data;
                else if (recvResult.Data is byte[] rawBytes)
                    fullFrame = rawBytes;
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "响应数据类型不匹配";
                    _data.sysLogger?.Error("[{EcuName}] 接收数据类型异常: {Type}", ecuName, recvResult.Data?.GetType());
                    return result;
                }

                // 记录原始数据（用于调试）
                string hexString = DiagnosticUtils.ToHexString(fullFrame);
                //_data.logshow.AddLog("RECV", $"[{ecuName}] 收到全帧: {hexString}");

                // 解析报文
                result = ParseDtcResponse(ecuName, fullFrame);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"异常: {ex.Message}";
                _data.sysLogger?.Error(ex, "[{EcuName}] DTC 读取异常", ecuName);
            }

            return result;
        }

/// <summary>
/// [功能专属解析] 解析 19 02 08（或 19 02 FF）的正响应
/// 
/// 完整帧 = [帧头4字节] + [诊断负载]
/// 诊断负载格式：
///   59                  ← 正响应 SID
///   02                  ← 子功能（reportDTCByStatusMask）
///   XX                  ← DTCStatusAvailabilityMask（掩码，不是数量！）
///   [记录1] [记录2] ...  ← 每条 = 3字节DTC编码 + 1字节状态
/// 
/// 修正点：
/// 1. 不再读取 data[3] 作为 DTC 个数，改为通过剩余长度 / 4 计算
/// 2. 状态字节取 bit0（testFailed）判定"当前故障"，而不是 bit7
/// </summary>
private EcuDtcResult ParseDtcResponse(string ecuName, byte[] fullFrameData)
        {
            var result = new EcuDtcResult { EcuName = ecuName };
            // ── 0. 剥离帧头 ──
            byte[] data = DiagnosticUtils.ExtractPayload(fullFrameData, headerLength: 4);
            if (data == null || data.Length < 3)   // 最少 59 02 XX
            {
                result.Success = false;
                result.ErrorMessage = "诊断数据过短";
                return result;
            }
            try
            {
                // ── 1. 正响应检查 ──
                if (data[0] != 0x59)
                {
                    if (data.Length >= 3 && data[0] == 0x7F && data[1] == 0x19)
                    {
                        byte nrc = data[2];
                        string desc = GetNrcDescription(nrc);
                        result.Success = false;
                        result.ErrorMessage = $"负响应 NRC=0x{nrc:X2} ({desc})";
                        _data.sysLogger?.Warning($"⚠️ [{ecuName}] 负响应: NRC=0x{nrc:X2} {desc}");
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = $"响应首字节异常: 0x{data[0]:X2}";
                    }
                    return result;
                }
                // ── 2. 读取 DTCStatusAvailabilityMask（可选，用于日志） ──
                byte mask = data[2];
                _data.sysLogger?.Information( $"[{ecuName}] DTCStatusAvailabilityMask = 0x{mask:X2}");
                // ── 3. 计算 DTC 记录数（每记录 = 3 编码字节 + 1 状态字节 = 4 字节） ──
                int payloadLength = data.Length - 3;          // 去掉 59 02 XX 三字节
                const int recordSize = 4;                     // 3 字节编码 + 1 字节状态
                int dtcCount = payloadLength / recordSize;    // 读取"数量字节"
                if (payloadLength % recordSize != 0)
                {
                    _data.sysLogger?.Warning($"[{ecuName}] 数据长度不规整（{payloadLength} 字节），" +
                                             $"可能有 {dtcCount} 条完整记录 + 余数 {payloadLength % recordSize}");
                }
                result.DtcList.Clear();
                if (dtcCount == 0)
                {
                    // 无故障码（如报文 59 02 4F）
                    result.Success = true;
                    _data.sysLogger?.Information( $"[{ecuName}] 无故障码");
                    return result;
                }
                for (int i = 0; i < dtcCount; i++)
                {
                    int offset = 3 + i * recordSize;   // 从第4字节开始
                                                       // 3 个编码字节
                    byte[] codeBytes = { data[offset], data[offset + 1], data[offset + 2] };
                    // 状态字节
                    byte statusByte = data[offset + 3];
                    string dtcCode = DecodeDtcCode(codeBytes);
                    bool isActive = (statusByte & 0x01) != 0;   // ★ bit0 = testFailed（当前故障）
                    result.DtcList.Add(new DtcInfo { Code = dtcCode, IsActive = isActive });
                }
                result.Success = true;
                int activeCount = result.DtcList.Count(d => d.IsActive);
                int inactiveCount = dtcCount - activeCount;
                _data.sysLogger?.Information($"[{ecuName}] 解析完成：{dtcCount} 个 DTC（当前 {activeCount}，历史 {inactiveCount}）");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"解析异常: {ex.Message}";
                _data.sysLogger?.Error(ex, "[{EcuName}] DTC 解析异常", ecuName);
            }
            return result;
        }

        /// <summary>
        /// [专属工具] 解码 3 字节 DTC 编码为故障码字符串
        /// 
        /// 3 字节 = 24 位，按位划分为 7 个字段：
        ///   第1段（2位）→ 前缀字母（0=P,1=C,2=B,3=U）
        ///   第2段（2位）→ 第一位数字（0~3）
        ///   后续5段（各4位）→ 每段值 0~9 转数字，10~15 转 A~F
        /// 
        /// 完全复现 VB6 中 DTC_Number_Code=3 时的解析逻辑。
        /// </summary>
        private string DecodeDtcCode(byte[] codeBytes)
        {
            // 将 3 字节组合为 24 位整数（高位在前）
            uint dtcValue = ((uint)codeBytes[0] << 16) | ((uint)codeBytes[1] << 8) | codeBytes[2];

            // 从最高位开始按段提取
            uint prefix = (dtcValue >> 22) & 0x03;    // bits 23-22
            uint digit1 = (dtcValue >> 20) & 0x03;    // bits 21-20
            uint seg3 = (dtcValue >> 16) & 0x0F;    // bits 19-16
            uint seg4 = (dtcValue >> 12) & 0x0F;    // bits 15-12
            uint seg5 = (dtcValue >> 8) & 0x0F;    // bits 11-8
            uint seg6 = (dtcValue >> 4) & 0x0F;    // bits 7-4
            uint seg7 = dtcValue & 0x0F;            // bits 3-0

            char prefixChar = prefix switch
            {
                0 => 'P',
                1 => 'C',
                2 => 'B',
                3 => 'U',
                _ => '?'
            };

            string FormatSeg(uint val) => val < 10 ? val.ToString() : ((char)('A' + (val - 10))).ToString();

            return $"{prefixChar}{digit1}{FormatSeg(seg3)}{FormatSeg(seg4)}{FormatSeg(seg5)}{FormatSeg(seg6)}{FormatSeg(seg7)}";
        }

        /// <summary>
        /// [通用工具] 获取负响应码的描述文字
        /// </summary>
        private string GetNrcDescription(byte nrc)
        {
            return nrc switch
            {
                0x10 => "一般拒绝",
                0x11 => "不支持该服务",
                0x12 => "不支持该子功能",
                0x13 => "报文长度或格式错误",
                0x22 => "条件不满足",
                0x31 => "请求超出范围",
                0x33 => "安全访问未解锁",
                0x35 => "无效的密钥",
                0x78 => "请求已收到，正在处理",
                _ => "未知负响应码"
            };
        }
    }
}
