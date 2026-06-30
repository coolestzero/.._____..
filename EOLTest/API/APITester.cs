using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Serilog;
using static EOLTest.API.APIUtils;
using static EOLTest.API.J2534Native;

namespace EOLTest.API
{
    /// <summary>
    /// APITester类：J2534 API的高级封装，提供简化的接口用于车辆诊断通信
    /// 实现了IDisposable接口，确保资源正确释放
    /// </summary>
    public class APITester : IDisposable
    {
        private readonly ILogger _logger;
        private J2534Library _lib; // J2534库实例，用于调用底层API
        // 结构体大小常量，用于内存分配
        private readonly int SCONFIG_SIZE = Marshal.SizeOf<SCONFIG>(); // SCONFIG结构体大小
        private readonly int SCONFIG_LIST_SIZE = Marshal.SizeOf<SCONFIG_LIST>(); // SCONFIG_LIST结构体大小
        // 设备状态变量
        private uint _deviceId; // 设备ID（从PassThruOpen获取）
        private uint _channelId; // 通道ID（从PassThruConnect获取）
        private uint _msgId; // 消息ID（用于周期性消息）
        private uint _filterId; // 当前活动的消息过滤器 ID
        private List<uint> _activeFilterIds = new List<uint>(); // 存储所有活动的过滤器 ID
        /// <summary>
        /// 寻址方式枚举
        /// </summary>
        public enum AddressingType
        {
            Physical,  // 物理寻址：使用模块特定 CAN ID
            Functional // 功能寻址：使用 0x7DF 广播 CAN ID
        }
        // 保存当前通信会话信息
        private CommunicationSession _currentSession;
        public APITester(ILogger sysLogger)
        {
            _logger = sysLogger;
            _currentSession = new CommunicationSession();
        }

        /// <summary>
        /// 加载J2534 DLL库
        /// </summary>
        /// <param name="dllPath">J2534 DLL的完整路径</param>
        /// <returns>空字符串表示成功，否则返回错误信息</returns>
        public ApiResult LoadLibrary(string dllPath)
        {
            string ret = string.Empty;

            try
            {
                _logger?.Information($"开始加载J2534库: {dllPath}");
                _lib = new J2534Library(dllPath); // 创建J2534Library实例，这会加载DLL并获取所有函数指针
                return ApiResult.Ok("库加载成功");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, $"加载J2534库失败: {dllPath}");
                _lib = null;
                return ApiResult.Fail("LOAD_ERROR", ex.Message);; // 返回异常信息
            }
        }
        /// <summary>
        /// 打开J2534设备
        /// </summary>
        /// <returns>操作结果字符串</returns>
        public ApiResult Open()
        {
            var checkRet = CheckLibraryLoaded();
            if (!checkRet.Success) return checkRet;

            try
            {
                _logger?.Information("正在打开J2534设备...");
                // 调用PassThruOpen打开设备
                // IntPtr.Zero表示使用默认设备名称
                int errCode = _lib.PassThruOpen(IntPtr.Zero, out _deviceId);
                if (errCode != STATUS_NOERROR)
                {
                    string errMsg = $"打开设备失败: {errorCode2str(errCode)}";
                    _logger?.Error(errMsg);
                    return ApiResult.Fail(errCode.ToString(), errMsg);
                }

                _logger?.Information($"设备打开成功, DeviceId: {_deviceId}");
                return ApiResult.Ok("设备打开成功", _deviceId, $"DeviceId: {_deviceId}");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "打开设备时发生异常");
                return ApiResult.Fail("EXCEPTION", $"打开设备异常: {ex.Message}");
            }

        }
        #region 读版本信息
        /// <summary>
        /// 读取设备版本信息和配置
        /// </summary>
        /// <returns>包含固件版本、DLL版本、API版本、序列号和电压的详细信息</returns>
        public ApiResult ReadVer()
        {
            var checkRet = CheckLibraryLoaded();
            if (!checkRet.Success) return checkRet;

            var detailInfo = new StringBuilder();
            var deviceInfo = new DeviceInfo();

            try
            {
                _logger?.Information("开始读取设备版本信息...");
                //固件版本
                StringBuilder fw = new StringBuilder(256);
                StringBuilder dll = new StringBuilder(256);
                StringBuilder api = new StringBuilder(256);
                int errCode = _lib.PassThruReadVersion(_deviceId, fw, dll, api);
                if (errCode != STATUS_NOERROR)
                {
                    _logger?.Error($"读取版本失败: {errorCode2str(errCode)}");
                    return ApiResult.Fail(errCode.ToString(), "读取版本失败",
                        $"PassThruReadVersion: {errorCode2str(errCode)}");
                }
                deviceInfo.FirmwareVersion = fw.ToString();
                deviceInfo.DllVersion = dll.ToString();
                deviceInfo.ApiVersion = api.ToString();

                detailInfo.AppendLine($"固件版本={deviceInfo.FirmwareVersion}");
                detailInfo.AppendLine($"DLL版本={deviceInfo.DllVersion}");
                detailInfo.AppendLine($"API版本={deviceInfo.ApiVersion}");

                _logger?.Information($"版本信息 - FW:{deviceInfo.FirmwareVersion}, " +
                                       $"DLL:{deviceInfo.DllVersion}, API:{deviceInfo.ApiVersion}");
                //序列号
                IntPtr cfgArrPtr = IntPtr.Zero;
                IntPtr cfglstPtr = IntPtr.Zero;
                try
                {
                    // 分配内存用于4个SCONFIG参数（序列号有4个部分）
                    int SCONFIG_SIZE = Marshal.SizeOf<SCONFIG>();
                    int SCONFIG_LIST_SIZE = Marshal.SizeOf<SCONFIG_LIST>();
                    cfgArrPtr = Marshal.AllocHGlobal(4 * SCONFIG_SIZE); //分配4个内存
                    cfglstPtr = Marshal.AllocHGlobal(SCONFIG_LIST_SIZE);
                    // 初始化4个序列号参数
                    Marshal.StructureToPtr(new SCONFIG() { Parameter = NON_VOLATILE_SN_0, Value = 0 }, cfgArrPtr, false);
                    Marshal.StructureToPtr(new SCONFIG() { Parameter = NON_VOLATILE_SN_1, Value = 0 }, cfgArrPtr + 1 * SCONFIG_SIZE, false);
                    Marshal.StructureToPtr(new SCONFIG() { Parameter = NON_VOLATILE_SN_2, Value = 0 }, cfgArrPtr + 2 * SCONFIG_SIZE, false);
                    Marshal.StructureToPtr(new SCONFIG() { Parameter = NON_VOLATILE_SN_3, Value = 0 }, cfgArrPtr + 3 * SCONFIG_SIZE, false);
                    // 创建参数列表
                    Marshal.StructureToPtr(new SCONFIG_LIST() { NumOfParams = 4, ConfigPtr = cfgArrPtr }, cfglstPtr, false);
                    // 调用IOCTL获取设备配置
                    errCode = _lib.PassThruIoctl(_deviceId, ICP_GET_DEVICE_CONFIG, cfglstPtr, IntPtr.Zero);
                    if (errCode == STATUS_NOERROR)
                    {
                        // 读取4个32位序列号值
                        uint sn0 = Marshal.PtrToStructure<SCONFIG>(cfgArrPtr).Value;
                        uint sn1 = Marshal.PtrToStructure<SCONFIG>(cfgArrPtr + 1 * SCONFIG_SIZE).Value;
                        uint sn2 = Marshal.PtrToStructure<SCONFIG>(cfgArrPtr + 2 * SCONFIG_SIZE).Value;
                        uint sn3 = Marshal.PtrToStructure<SCONFIG>(cfgArrPtr + 3 * SCONFIG_SIZE).Value;
                        // 将32位值转换为字节数组
                        byte[] snArr = new byte[]
                        {
                            (byte)(sn0&0xFF), (byte)((sn0>>8)&0xFF), (byte)((sn0>>16)&0xFF), (byte)((sn0>>24)&0xFF),
                            (byte)(sn1&0xFF), (byte)((sn1>>8)&0xFF), (byte)((sn1>>16)&0xFF), (byte)((sn1>>24)&0xFF),
                            (byte)(sn2&0xFF), (byte)((sn2>>8)&0xFF), (byte)((sn2>>16)&0xFF), (byte)((sn2>>24)&0xFF),
                            (byte)(sn3&0xFF), (byte)((sn3>>8)&0xFF), (byte)((sn3>>16)&0xFF), (byte)((sn3>>24)&0xFF)
                        };
                        deviceInfo.SerialNumber = Encoding.ASCII.GetString(snArr).TrimEnd('\0'); // 将字节数组转换为ASCII字符串
                        detailInfo.AppendLine($"序列号={deviceInfo.SerialNumber}");
                        _logger?.Information($"序列号: {deviceInfo.SerialNumber}");
                    }

                }
                finally
                {
                    // 确保释放分配的内存
                    if (cfglstPtr != IntPtr.Zero) Marshal.FreeHGlobal(cfglstPtr);
                    if (cfgArrPtr != IntPtr.Zero) Marshal.FreeHGlobal(cfgArrPtr);
                }

                //电压
                IntPtr volPtr = IntPtr.Zero;
                try
                {
                    volPtr = Marshal.AllocHGlobal(sizeof(uint));
                    errCode = _lib.PassThruIoctl(_deviceId, ICP_READ_VBATT, IntPtr.Zero, volPtr);
                    if (errCode == STATUS_NOERROR)
                    {
                        uint voltage = (uint)Marshal.ReadInt32(volPtr);
                        deviceInfo.Voltage = (uint)Marshal.ReadInt32(volPtr);
                        detailInfo.AppendLine($"电压={deviceInfo.Voltage}mV");
                        _logger?.Information($"电压: {deviceInfo.Voltage}mV");
                    }
                }
                finally
                {
                    if (volPtr != IntPtr.Zero) Marshal.FreeHGlobal(volPtr);
                }

                return ApiResult.Ok("设备信息读取成功", deviceInfo, detailInfo.ToString());
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "读取设备信息异常");
                return ApiResult.Fail("EXCEPTION", $"读取设备信息异常: {ex.Message}");
            }
            
        }
        #endregion

        #region 通道连接
        /// <summary>
        /// 打开 J2534 通信通道并配置通用参数（引脚、流控、填充位、清缓冲）
        /// 此方法不绑定任何模块，适合一次打开后复用
        /// </summary>
        /// <param name="protocol">协议 ID，默认 ISO15765-2</param>
        /// <param name="flags">通道标志，默认 29 位 CAN ID + 收发双向</param>
        /// <returns>操作结果</returns>
        public DiagnosticResult OpenChannel(uint protocol = PROTC_ISO15765_PS,
                                            uint flags = FLAGS_CAN_29BIT_ID | FLAGS_CAN_ID_BOTH)
        {
            try
            {
                // 检查 DLL
                var checkResult = CheckLibraryLoaded();
                if (!checkResult.Success)
                    return DiagnosticResult.Fail(checkResult.ErrorCode, checkResult.Message);

                // 检查设备
                if (_deviceId == 0)
                    return DiagnosticResult.Fail("DEVICE_NOT_OPEN", "设备未打开，请先调用 Open 方法");

                // 如果已有通道，先关闭
                if (_currentSession.IsConnected)
                {
                    _logger?.Warning("检测到现有通道，将先关闭");
                    CloseChannel();
                }

                // 调用 PassThruConnect 建立通道
                _logger?.Information($"打开通信通道 - Protocol:0x{protocol:X}, Flags:0x{flags:X}");
                int errCode = _lib.PassThruConnect(_deviceId, protocol, flags, 500000, out _channelId);
                if (errCode != STATUS_NOERROR)
                {
                    string errMsg = $"打开通道失败: {errorCode2str(errCode)}";
                    _logger?.Error(errMsg);
                    return DiagnosticResult.Fail(errCode.ToString(), errMsg);
                }

                // 配置通道通用参数（使用默认 txFlags，后续模块设置时会覆盖其中的寻址标志）
                uint defaultTxFlags = ISO15765_FRAME_PAD;
                SetupChannelParameters(protocol, defaultTxFlags);

                // 保存通道会话（此时没有模块信息）
                _currentSession = new CommunicationSession
                {
                    DeviceId = _deviceId,
                    ChannelId = _channelId,
                    Protocol = protocol,
                    RequestId = 0,          // 待 SetActiveModule 赋值
                    ResponseId = 0,
                    ExtAddress = 0,
                    TxFlags = defaultTxFlags,
                    IsConnected = true
                };

                // 清空过滤器跟踪列表
                _activeFilterIds.Clear();

                _logger?.Information($"通道打开成功 - ChannelId:{_channelId}");
                return DiagnosticResult.Ok("通道打开成功", _currentSession, $"ChannelId: {_channelId}");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "打开通道时发生异常");
                return DiagnosticResult.Fail("OPEN_CHANNEL_EXCEPTION", $"异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭当前 J2534 通信通道，并自动停止所有活动的过滤器
        /// </summary>
        public DiagnosticResult CloseChannel()
        {
            try
            {
                if (!_currentSession.IsConnected)
                    return DiagnosticResult.Ok("通道未打开，无需关闭");

                _logger?.Information($"关闭通信通道 - ChannelId:{_channelId}");

                // 停止所有过滤器
                StopAllFilters();

                // 断开通道
                int errCode = _lib.PassThruDisconnect(_channelId);
                _currentSession.IsConnected = false;
                _channelId = 0;

                if (errCode != STATUS_NOERROR)
                {
                    _logger?.Warning($"关闭通道时发生错误: {errorCode2str(errCode)}");
                    return DiagnosticResult.Fail(errCode.ToString(), $"关闭通道失败: {errorCode2str(errCode)}");
                }

                return DiagnosticResult.Ok("通道已关闭");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "关闭通道时发生异常");
                return DiagnosticResult.Fail("CLOSE_CHANNEL_EXCEPTION", $"异常: {ex.Message}");
            }
        }
        #endregion

        #region 模块连接

        /// <summary>
        /// 解析并验证模块参数，返回处理后的通信参数
        /// 供 SetActiveModule 和 AddModuleFilter 共用
        /// </summary>
        private DiagnosticResult PrepareModuleParams(
            string requestId, string responseId, string strlinId,
            AddressingType addressingType,
            out uint reqId, out uint rspId, out byte extAddr, out uint txFlags)
        {
            reqId = 0;
            rspId = 0;
            extAddr = 0;
            txFlags = ISO15765_FRAME_PAD;

            // 检查通道是否打开
            if (!_currentSession.IsConnected)
                return DiagnosticResult.Fail("CHANNEL_NOT_OPEN",
                    "请先调用 OpenChannel 打开通信通道");

            // 解析 CAN ID 参数
            var paramResult = CheckInputCanId(requestId, responseId, strlinId,
                out reqId, out rspId, out extAddr);
            if (!paramResult.Success)
                return DiagnosticResult.Fail(paramResult.ErrorCode, paramResult.Message);

            // 功能寻址业务约束
            bool isFunctional = (addressingType == AddressingType.Functional);
            if (isFunctional && extAddr != 0)
                return DiagnosticResult.Fail("FUNCTIONAL_EXTADDR_NOT_SUPPORTED",
                    $"功能寻址不支持扩展地址 (LIN ID: 0x{extAddr:X2})");

            // 计算 txFlags
            if (!isFunctional && extAddr != 0)
                txFlags |= ISO15765_ADDR_TYPE;
            if (isFunctional)
                extAddr = 0;

            return DiagnosticResult.Ok();
        }
        /// <summary>
        /// 设置当前操作的模块及其寻址方式
        /// 后续 SendPhysical / SendFunctional 将使用这些信息
        /// 注意：此方法不设置硬件过滤器
        /// </summary>
        public DiagnosticResult SetActiveModule(string requestId, string responseId, string strlinId,
                                                 AddressingType addressingType = AddressingType.Physical)
        {
            var prepResult = PrepareModuleParams(requestId, responseId, strlinId, addressingType,
                out uint reqId, out uint rspId, out byte extAddr, out uint txFlags);
            if (!prepResult.Success)
                return prepResult;

            // 更新当前会话（核心职责：改变应用层状态）
            _currentSession.RequestId = reqId;
            _currentSession.ResponseId = rspId;
            _currentSession.ExtAddress = extAddr;
            _currentSession.TxFlags = txFlags;
            _currentSession.AddressingType = addressingType;

            string addrName = addressingType == AddressingType.Functional ? "功能寻址" : "物理寻址";
            _logger?.Information($"当前模块已设置 - {addrName} ReqId:0x{reqId:X}, RspId:0x{rspId:X}, ExtAddr:0x{extAddr:X2}");

            return DiagnosticResult.Ok("模块设置成功", _currentSession);
        }
        /// <summary>
        /// 为一个模块添加硬件消息过滤器
        /// 支持同时存在多个过滤器
        /// 注意：此方法不改变当前操作模块（不影响 Send/Receive 使用的 ID）
        /// </summary>
        public DiagnosticResult AddModuleFilter(string requestId, string responseId, string strlinId,
                                                 AddressingType addressingType = AddressingType.Physical)
        {
            var prepResult = PrepareModuleParams(requestId, responseId, strlinId, addressingType,
                out uint reqId, out uint rspId, out byte extAddr, out uint txFlags);
            if (!prepResult.Success)
                return prepResult;

            // 构造过滤器的三个消息结构
            byte[] maskMsgData = GetFilterMaskId(extAddr);
            byte[] patternMsgData = GetFilterId(rspId, extAddr);
            byte[] flowControlMsgData = GetFilterId(reqId, extAddr);

            PASSTHRU_MSG mask = NewMsg(_currentSession.Protocol, 0, txFlags, maskMsgData);
            PASSTHRU_MSG pattern = NewMsg(_currentSession.Protocol, 0, txFlags, patternMsgData);
            PASSTHRU_MSG flowControl = NewMsg(_currentSession.Protocol, 0, txFlags, flowControlMsgData);

            uint filterId;
            int errCode = _lib.PassThruStartMsgFilter(_channelId, FLOW_CONTROL_FILTER,
                ref mask, ref pattern, ref flowControl, out filterId);

            if (errCode != STATUS_NOERROR)
            {
                string errMsg = $"添加过滤器失败: {errorCode2str(errCode)}";
                _logger?.Error(errMsg);
                return DiagnosticResult.Fail(errCode.ToString(), errMsg);
            }

            _currentSession.FilterId = filterId; //记录过滤器ID
            _activeFilterIds.Add(filterId);

            string filterType = addressingType == AddressingType.Functional ? "功能寻址" : "物理寻址";
            _logger?.Information($"{filterType}过滤器已添加 - FilterId:{filterId}");

            return DiagnosticResult.Ok("过滤器添加成功", filterId);
        }

        /// <summary>
        /// 停止指定的消息过滤器
        /// </summary>
        /// <param name="filterId">要停止的过滤器 ID</param>
        /// <returns>操作结果</returns>
        public DiagnosticResult StopFilter(uint filterId)
        {
            try
            {
                if (!_activeFilterIds.Contains(filterId))
                    return DiagnosticResult.Ok("该过滤器不存在或已停止");

                _logger?.Information($"停止过滤器 - FilterId:{filterId}");
                int errCode = _lib.PassThruStopMsgFilter(_channelId, filterId);

                if (errCode != STATUS_NOERROR)
                {
                    _logger?.Warning($"停止过滤器失败: {errorCode2str(errCode)}");
                    return DiagnosticResult.Fail(errCode.ToString(), $"停止过滤器失败: {errorCode2str(errCode)}");
                }

                _activeFilterIds.Remove(filterId);
                return DiagnosticResult.Ok("过滤器已停止", filterId);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "停止过滤器时发生异常");
                return DiagnosticResult.Fail("STOP_FILTER_EXCEPTION", $"异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止所有活动的消息过滤器（通常由 CloseChannel 调用）
        /// </summary>
        public void StopAllFilters()
        {
            foreach (var filterId in _activeFilterIds.ToList()) // 用 ToList 避免迭代时修改集合
            {
                _lib.PassThruStopMsgFilter(_channelId, filterId);
                _logger?.Information($"已停止过滤器 - FilterId:{filterId}");
            }
            _activeFilterIds.Clear();
        }
        /// <summary>
        /// 设置ID并设置过滤器 SetActiveModule + AddModuleFilter 合并成一个快捷调用
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="responseId"></param>
        /// <param name="strlinId"></param>
        /// <param name="addressingType"></param>
        /// <returns></returns>
        public DiagnosticResult ConnectToModule(string requestId, string responseId, string strlinId,
                                         AddressingType addressingType = AddressingType.Physical)
        {
            var setResult = SetActiveModule(requestId, responseId, strlinId, addressingType);
            if (!setResult.Success) return setResult;
            return AddModuleFilter(requestId, responseId, strlinId, addressingType);
        }
        #endregion

        #region 断开连接
        /// <summary>
        /// 断开当前连接
        /// </summary>
        public ApiResult Disconnect()
        {
            try
            {
                if (!_currentSession.IsConnected)
                {
                    return ApiResult.Ok("当前无活动连接");
                }

                _logger?.Information($"断开连接 - ChannelId:{_channelId}");

                int errCode = _lib.PassThruDisconnect(_channelId);

                _currentSession.IsConnected = false;
                _channelId = 0;

                if (errCode != STATUS_NOERROR)
                {
                    string errorMsg = $"断开连接失败: {errorCode2str(errCode)}";
                    _logger?.Warning(errorMsg);
                    return ApiResult.Fail(errCode.ToString(), errorMsg);
                }

                return ApiResult.Ok("连接已断开");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "断开连接时发生异常");
                return ApiResult.Fail("DISCONNECT_EXCEPTION", $"断开异常: {ex.Message}");
            }
        }
        #endregion

        #region 发送和接收
        /// <summary>
        /// 物理寻址发送诊断请求（使用当前激活模块的物理请求 CAN ID）
        /// 调用前必须先 OpenChannel 并 SetActiveModule 为物理寻址
        /// </summary>
        /// <param name="hexData">十六进制诊断数据（如 "19 02 FF 00"）</param>
        /// <param name="timeout">发送超时时间（毫秒）</param>
        /// <returns>发送结果</returns>
        public DiagnosticResult SendPhysical(string hexData, uint timeout = 200)
        {
            try
            {
                // 1. 检查通道是否打开
                if (!_currentSession.IsConnected)
                    return DiagnosticResult.Fail("CHANNEL_NOT_OPEN",
                        "请先调用 OpenChannel 打开通信通道");

                // 2. 检查模块是否已设置（RequestId 为 0 表示未调用 SetActiveModule）
                if (_currentSession.RequestId == 0)
                    return DiagnosticResult.Fail("MODULE_NOT_SET",
                        "请先调用 SetActiveModule 设置要通信的模块");

                // 3. 检查寻址方式是否为物理寻址
                if (_currentSession.AddressingType != AddressingType.Physical)
                    return DiagnosticResult.Fail("WRONG_ADDRESSING",
                        "当前模块设置为功能寻址，请使用 SendFunctional 方法发送");

                // 4. 解析诊断数据
                byte[] msgData = HexString2HexData(hexData);
                if (msgData == null || msgData.Length == 0)
                    return DiagnosticResult.Fail("INVALID_DATA",
                        $"无效的发送数据: {hexData}");

                _logger?.Information($"物理寻址发送: {hexData}");

                // 5. 构造 CAN 消息头（物理寻址使用模块特定的 ReqId 和 ExtAddress）
                byte[] data = GetReqestData(_currentSession.RequestId,
                                           _currentSession.ExtAddress,
                                           msgData);

                // 6. 创建消息结构体
                PASSTHRU_MSG[] wMsgs = new PASSTHRU_MSG[1];
                wMsgs[0] = NewMsg(_currentSession.Protocol, 0, _currentSession.TxFlags, data);

                // 7. 发送
                uint numMsgs = (uint)wMsgs.Length;
                int errCode = _lib.PassThruWriteMsgs(_currentSession.ChannelId,
                                                    wMsgs, ref numMsgs, timeout);
                if (errCode != STATUS_NOERROR)
                {
                    string errorMsg = $"物理寻址发送失败: {errorCode2str(errCode)}";
                    _logger?.Error(errorMsg);
                    return DiagnosticResult.Fail(errCode.ToString(), errorMsg);
                }

                // 8. 记录发送内容
                string sentData = BitConverter.ToString(wMsgs[0].Data, 0, (int)wMsgs[0].DataSize)
                                              .Replace("-", " ");
                string detailInfo = $"物理寻址发送成功 - 数据:[{sentData}]";
                _logger?.Information(detailInfo);

                return DiagnosticResult.Ok("发送成功", msgData, detailInfo);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "物理寻址发送异常");
                return DiagnosticResult.Fail("SEND_EXCEPTION", $"发送异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 功能寻址发送诊断请求（使用固定广播 CAN ID 0x7DF）
        /// 调用前必须先 OpenChannel 并 SetActiveModule 为功能寻址
        /// </summary>
        /// <param name="hexData">十六进制诊断数据（如 "10 03"）</param>
        /// <param name="timeout">发送超时时间（毫秒）</param>
        /// <returns>发送结果</returns>
        public DiagnosticResult SendFunctional(string hexData, uint timeout = 500)
        {
            try
            {
                // 1. 检查通道是否打开
                if (!_currentSession.IsConnected)
                    return DiagnosticResult.Fail("CHANNEL_NOT_OPEN",
                        "请先调用 OpenChannel 打开通信通道");

                // 2. 检查模块是否已设置
                if (_currentSession.RequestId == 0)
                    return DiagnosticResult.Fail("MODULE_NOT_SET",
                        "请先调用 SetActiveModule 设置通信模块（功能寻址也需要模块信息）");

                // 3. 检查寻址方式是否为功能寻址
                if (_currentSession.AddressingType != AddressingType.Functional)
                    return DiagnosticResult.Fail("WRONG_ADDRESSING",
                        "当前模块设置为物理寻址，请使用 SendPhysical 方法发送");

                // 4. 解析诊断数据
                byte[] msgData = HexString2HexData(hexData);
                if (msgData == null || msgData.Length == 0)
                    return DiagnosticResult.Fail("INVALID_DATA",
                        $"无效的发送数据: {hexData}");

                _logger?.Information($"功能寻址发送: {hexData}");

                // 5. 构造功能寻址数据：前4字节固定为 0x00 0x00 0x07 0xDF（大端表示的 0x7DF）
                byte[] data = new byte[4 + msgData.Length];
                Array.Copy(new byte[] { 0x00, 0x00, 0x07, 0xDF }, data, 4);
                Array.Copy(msgData, 0, data, 4, msgData.Length);

                // 6. 创建消息（注意 txFlags 不应包含扩展地址标志）
                PASSTHRU_MSG[] wMsgs = new PASSTHRU_MSG[1];
                wMsgs[0] = NewMsg(_currentSession.Protocol, 0, _currentSession.TxFlags, data);

                // 7. 发送
                uint numMsgs = (uint)wMsgs.Length;
                int errCode = _lib.PassThruWriteMsgs(_currentSession.ChannelId,
                                                    wMsgs, ref numMsgs, timeout);
                if (errCode != STATUS_NOERROR)
                {
                    string errorMsg = $"功能寻址发送失败: {errorCode2str(errCode)}";
                    _logger?.Error(errorMsg);
                    return DiagnosticResult.Fail(errCode.ToString(), errorMsg);
                }

                // 8. 记录发送内容
                string sentData = BitConverter.ToString(wMsgs[0].Data, 0, (int)wMsgs[0].DataSize)
                                              .Replace("-", " ");
                string detailInfo = $"功能寻址发送成功 - 数据:[{sentData}]";
                _logger?.Information(detailInfo);

                return DiagnosticResult.Ok("发送成功", msgData, detailInfo);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "功能寻址发送异常");
                return DiagnosticResult.Fail("SEND_EXCEPTION", $"发送异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 物理寻址接收（单响应）
        /// 严格按照原始SendRecv中的接收逻辑
        /// </summary>
        /// <param name="timeout">单次接收超时时间（毫秒）</param>
        /// <returns>接收结果</returns>
        public DiagnosticResult ReceivePhysicalResponse(uint timeout = 100)
        {
            try
            {
                if (!_currentSession.IsConnected)
                {
                    return DiagnosticResult.Fail("NOT_CONNECTED", "未建立连接");
                }

                _logger?.Information("等待物理寻址响应...");

                // 原始代码中的接收逻辑
                PASSTHRU_MSG[] rMsgs = new PASSTHRU_MSG[1];  // 物理寻址期望1个响应
                int numNeedRecv = rMsgs.Length;  // 需要接收的数量
                int numHaveRecv = 0;             // 已接收的数量

                Stopwatch sw = Stopwatch.StartNew();
                DiagnosticResponse response = null;

                while (numNeedRecv > 0)
                {
                    uint numRecv = (uint)numNeedRecv;
                    PASSTHRU_MSG[] tmpMsgs = new PASSTHRU_MSG[numNeedRecv];

                    // 读取消息
                    int errCode = _lib.PassThruReadMsgs(_currentSession.ChannelId,
                                                       tmpMsgs, ref numRecv, timeout);

                    // 处理接收结果（原始代码的判断条件）
                    if (errCode == STATUS_NOERROR || errCode == ERR_TIMEOUT ||
                        (errCode == ERR_BUFFER_EMPTY && numRecv > 0))
                    {
                        for (int i = 0; i < numRecv; i++)
                        {
                            // 过滤掉发送确认和开始消息
                            if ((tmpMsgs[i].RxStatus & TX_SUCCESS) == TX_SUCCESS) continue;
                            if ((tmpMsgs[i].RxStatus & START_OF_MESSAGE) == START_OF_MESSAGE) continue;

                            // 收到有效消息
                            if (tmpMsgs[i].DataSize > 0)
                            {
                                response = new DiagnosticResponse
                                {
                                    Data = tmpMsgs[i].Data.Take((int)tmpMsgs[i].DataSize).ToArray(),
                                    Timestamp = tmpMsgs[i].Timestamp
                                };
                                rMsgs[numHaveRecv++] = tmpMsgs[i];
                                numNeedRecv--;
                            }
                        }
                    }

                    // 超时检查（原始代码的超时逻辑）
                    if (sw.ElapsedMilliseconds > rMsgs.Length * timeout) break;
                }
                sw.Stop();

                if (numHaveRecv > 0 && response != null)
                {
                    string detailInfo = $"物理寻址响应 - 数据:[{response.DataHex}], " +
                                       $"耗时:{sw.ElapsedMilliseconds}ms";
                    _logger?.Information(detailInfo);

                    return DiagnosticResult.Ok("接收成功", response, detailInfo);
                }
                else
                {
                    string errorMsg = $"物理寻址接收超时或失败, 耗时:{sw.ElapsedMilliseconds}ms";
                    _logger?.Warning(errorMsg);
                    return DiagnosticResult.Fail("NO_RESPONSE", errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "物理寻址接收异常");
                return DiagnosticResult.Fail("RECEIVE_EXCEPTION", $"接收异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 功能寻址接收（多响应）
        /// </summary>
        /// <param name="timeout">总接收超时时间（毫秒）</param>
        /// <returns>接收结果</returns>
        public DiagnosticResult ReceiveFunctionalResponses(uint timeout = 1000)
        {
            try
            {
                if (!_currentSession.IsConnected)
                {
                    return DiagnosticResult.Fail("NOT_CONNECTED", "未建立连接");
                }

                _logger?.Information("等待功能寻址响应...");

                // 准备接收缓冲区（原始代码中功能寻址用10个）
                PASSTHRU_MSG[] rMsgs = new PASSTHRU_MSG[10];
                var responses = new List<DiagnosticResponse>();

                int totalReceived = 0;
                var sw = Stopwatch.StartNew();

                // 原始代码的接收逻辑
                while (totalReceived < rMsgs.Length && sw.ElapsedMilliseconds < timeout)
                {
                    uint numRecv = (uint)(rMsgs.Length - totalReceived);
                    PASSTHRU_MSG[] tmpMsgs = new PASSTHRU_MSG[numRecv];

                    int errCode = _lib.PassThruReadMsgs(_currentSession.ChannelId,
                                                       tmpMsgs, ref numRecv, 100);

                    if (errCode == STATUS_NOERROR || errCode == ERR_TIMEOUT)
                    {
                        for (int i = 0; i < numRecv; i++)
                        {
                            // 原始代码的过滤逻辑
                            if ((tmpMsgs[i].RxStatus & TX_SUCCESS) == TX_SUCCESS) continue;

                            if (tmpMsgs[i].DataSize > 0)
                            {
                                // 提取ECU地址（原始代码中的提取方式）
                                uint senderId = (uint)(tmpMsgs[i].Data[2] << 8) | tmpMsgs[i].Data[3];
                                string responseData = BitConverter.ToString(tmpMsgs[i].Data, 4,
                                                                            (int)tmpMsgs[i].DataSize - 4)
                                                                  .Replace("-", " ");

                                responses.Add(new DiagnosticResponse
                                {
                                    SenderId = senderId,
                                    Data = tmpMsgs[i].Data.Skip(4).Take((int)tmpMsgs[i].DataSize - 4).ToArray(),
                                    Timestamp = tmpMsgs[i].Timestamp

                                });

                                totalReceived++;
                            }
                        }
                    }
                }
                sw.Stop();

                if (responses.Count > 0)
                {
                    string detailInfo = $"功能寻址响应 - 收到{responses.Count}个响应, " +
                                       $"耗时:{sw.ElapsedMilliseconds}ms";
                    foreach (var resp in responses)
                    {
                        detailInfo += $"\n  ECU 0x{resp.SenderId:X3}: {resp.DataHex}";
                    }

                    _logger?.Information(detailInfo);
                    return DiagnosticResult.Ok($"收到 {responses.Count} 个响应", responses, detailInfo);
                }
                else
                {
                    string errorMsg = $"功能寻址未收到响应 (>{timeout}ms)";
                    _logger?.Warning(errorMsg);
                    return DiagnosticResult.Fail("NO_RESPONSE", errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "功能寻址接收异常");
                return DiagnosticResult.Fail("RECEIVE_EXCEPTION", $"接收异常: {ex.Message}");
            }
        }

        #endregion

        #region 其他必要方法

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public DiagnosticResult GetConnectionStatus()
        {
            return DiagnosticResult.Ok(
                _currentSession.IsConnected ? "已连接" : "未连接",
                _currentSession);
        }

        /// <summary>
        /// 判断是否已连接
        /// </summary>
        public bool IsConnected => _currentSession.IsConnected;

        #endregion

        // 增强的接收方法，支持多个响应
        private string SendRecvMultiple(PASSTHRU_MSG[] wMsgs, PASSTHRU_MSG[] rMsgs)
        {
            StringBuilder ret = new StringBuilder();

            // 发送消息
            uint numMsgs = (uint)wMsgs.Length;
            uint timeout = 500; // 增加超时时间
            int errCode = _lib.PassThruWriteMsgs(_channelId, wMsgs, ref numMsgs, timeout);
            if (errCode != STATUS_NOERROR)
                return $"PassThruWriteMsgs: error code={errorCode2str(errCode)}";

            // 记录发送内容
            ret.Append($"发送: {BitConverter.ToString(wMsgs[0].Data, 0, (int)wMsgs[0].DataSize)}");

            // 接收多个响应
            int totalReceived = 0;
            Stopwatch sw = Stopwatch.StartNew();

            while (totalReceived < rMsgs.Length && sw.ElapsedMilliseconds < 1000) // 最多等待1秒
            {
                uint numRecv = (uint)(rMsgs.Length - totalReceived);
                PASSTHRU_MSG[] tmpMsgs = new PASSTHRU_MSG[numRecv];

                errCode = _lib.PassThruReadMsgs(_channelId, tmpMsgs, ref numRecv, 100);

                if (errCode == STATUS_NOERROR || errCode == ERR_TIMEOUT)
                {
                    for (int i = 0; i < numRecv; i++)
                    {
                        // 过滤无效响应
                        if ((tmpMsgs[i].RxStatus & TX_SUCCESS) == TX_SUCCESS) continue;
                        if (tmpMsgs[i].DataSize > 0)
                        {
                            rMsgs[totalReceived++] = tmpMsgs[i];
                        }
                    }
                }
            }
            sw.Stop();

            ret.Append($"\n收到 {totalReceived} 个响应");
            return ret.ToString();
        }
        /// <summary>
        /// 启动周期性消息（例如 Tester Present 3E 80）
        /// 要求：通道已打开，当前模块已通过 SetActiveModule 设置
        /// 会自动为当前模块添加一个过滤器（如果还没有）
        /// </summary>
        public ApiResult StartPeriodicMsg()
        {
            var checkResult = CheckLibraryLoaded();
            if (!checkResult.Success) return checkResult;

            if (!_currentSession.IsConnected)
                return ApiResult.Fail("CHANNEL_NOT_OPEN", "请先调用 OpenChannel");
            if (_currentSession.RequestId == 0)
                return ApiResult.Fail("MODULE_NOT_SET", "请先调用 SetActiveModule 设置当前模块");

            uint reqId = _currentSession.RequestId;
            byte extAddr = _currentSession.ExtAddress;
            uint txFlags = _currentSession.TxFlags;

            // 自动添加当前模块的过滤器，以便接收响应
            // 注意：如果过滤器已存在，这里会重复添加，但硬件一般支持多个相同过滤器，影响不大。
            // 更严谨的做法是先查找是否已有相同过滤器，但为简化先允许重复添加。
            var filterResult = AddModuleFilter(
                $"0x{reqId:X}", $"0x{_currentSession.ResponseId:X}", $"0x{extAddr:X2}",
                AddressingType.Physical);
            if (!filterResult.Success)
                return ApiResult.Fail(filterResult.ErrorCode, filterResult.Message);

            // 构造 3E 80 消息
            byte[] data = GetReqestData(reqId, extAddr, new byte[] { 0x3E, 0x80 });
            PASSTHRU_MSG periodicMsg = NewMsg(_currentSession.Protocol, 0, txFlags, data);

            int errCode = _lib.PassThruStartPeriodicMsg(_channelId, ref periodicMsg, ref _msgId, 500);
            if (errCode != STATUS_NOERROR)
                return ApiResult.Fail(errCode.ToString(), $"启动周期性消息失败: {errorCode2str(errCode)}");

            _logger?.Information($"周期性消息已启动 - MsgId:{_msgId}, 间隔:500ms");
            return ApiResult.Ok("周期性消息启动成功", _msgId, $"MsgId:{_msgId}");
        }

        /// <summary>
        /// 停止周期性消息（不关闭通道，不停止过滤器）
        /// </summary>
        public ApiResult StopPeriodicMsg()
        {
            var checkResult = CheckLibraryLoaded();
            if (!checkResult.Success) return checkResult;

            if (_msgId == 0)
                return ApiResult.Ok("没有活动的周期性消息");

            int errCode = _lib.PassThruStopPeriodicMsg(_channelId, _msgId);
            _msgId = 0;

            if (errCode != STATUS_NOERROR)
                return ApiResult.Fail(errCode.ToString(), $"停止周期性消息失败: {errorCode2str(errCode)}");

            _logger?.Information("周期性消息已停止");
            return ApiResult.Ok("周期性消息已停止");
        }
        /// <summary>
        /// 关闭设备
        /// </summary>
        public ApiResult CloseDevice()
        {
            var checkRet = CheckLibraryLoaded();
            if (!checkRet.Success) return checkRet;

            int errCode = _lib.PassThruClose(_deviceId);
            return ApiResult.Ok("设备关闭成功", _deviceId, $"DeviceId: {_deviceId}");
        }
        /// <summary>
        /// 释放资源（IDisposable实现）
        /// </summary>
        public void Dispose()
        {
            if (_lib != null)
            {
                _lib.Dispose();
            }
        }
        /// <summary>
        /// 配置通道通用参数：引脚、流控、填充位、清空接收缓冲
        /// 这些参数与具体模块无关，每次建立新通道都需要设置
        /// </summary>
        /// <param name="protocol">协议 ID（如 PROTC_ISO15765_PS）</param>
        /// <param name="txFlags">发送标志（如 ISO15765_FRAME_PAD）</param>
        private void SetupChannelParameters(uint protocol, uint txFlags)
        {
            int errCode;

            try
            {
                _logger?.Information("开始配置通道参数...");
                // 1. 设置J1962引脚配置
                // 0x060E表示使用CAN总线的引脚6和14（高速CAN）
                uint pin = 0x060E;
                IntPtr cfgPtr = IntPtr.Zero;
                IntPtr cfglstPtr = IntPtr.Zero;
                try
                {
                    cfgPtr = Marshal.AllocHGlobal(SCONFIG_SIZE);
                    cfglstPtr = Marshal.AllocHGlobal(SCONFIG_LIST_SIZE);
                    Marshal.StructureToPtr(new SCONFIG() { Parameter = IOC_GSET_PARM_J1962_PINS, Value = pin }, cfgPtr, false);
                    Marshal.StructureToPtr(new SCONFIG_LIST() { NumOfParams = 1, ConfigPtr = cfgPtr }, cfglstPtr, false);
                    errCode = _lib.PassThruIoctl(_channelId, ICP_SET_CONFIG, cfglstPtr, IntPtr.Zero);
                    if (errCode != STATUS_NOERROR)
                    {
                        _logger?.Warning($"设置J1962引脚失败: {errorCode2str(errCode)}");
                    }
                }
                finally
                {
                    if (cfglstPtr != IntPtr.Zero) Marshal.FreeHGlobal(cfglstPtr);
                    if (cfgPtr != IntPtr.Zero) Marshal.FreeHGlobal(cfgPtr);
                }

                //设置流控
                uint bs = 0;            //ISO15765_BS 块大小（0表示不使用块大小限制）
                uint stmin = 10;         //ISO15765_STMIN 最小间隔时间（10ms）
                                         //uint bx_tx = 0xFFFF;    //BS_TX
                                         //uint stmin_tx = 0xFFFF; //STMIN_TX
                cfgPtr = IntPtr.Zero;
                cfglstPtr = IntPtr.Zero;
                try
                {
                    cfgPtr = Marshal.AllocHGlobal(2 * SCONFIG_SIZE);
                    cfglstPtr = Marshal.AllocHGlobal(SCONFIG_LIST_SIZE);
                    Marshal.StructureToPtr(new SCONFIG() { Parameter = IOC_GSET_PARM_ISO15765_BS, Value = bs }, cfgPtr, false);
                    Marshal.StructureToPtr(new SCONFIG() { Parameter = IOC_GSET_PARM_ISO15765_STMIN, Value = stmin }, cfgPtr + 1 * SCONFIG_SIZE, false);
                    //Marshal.StructureToPtr(new SCONFIG() { Parameter = IOC_GSET_PARM_BS_TX, Value = bx_tx }, cfgPtr + 2 * SCONFIG_SIZE, false);
                    //Marshal.StructureToPtr(new SCONFIG() { Parameter = IOC_GSET_PARM_STMIN_TX, Value = stmin_tx }, cfgPtr + 3 * SCONFIG_SIZE, false);
                    Marshal.StructureToPtr(new SCONFIG_LIST() { NumOfParams = 2, ConfigPtr = cfgPtr }, cfglstPtr, false);
                    errCode = _lib.PassThruIoctl(_channelId, ICP_SET_CONFIG, cfglstPtr, IntPtr.Zero);
                    if (errCode != STATUS_NOERROR)
                    {
                        _logger?.Warning($"设置流控参数失败: {errorCode2str(errCode)}");
                    }
                }
                finally
                {
                    if (cfglstPtr != IntPtr.Zero) Marshal.FreeHGlobal(cfglstPtr);
                    if (cfgPtr != IntPtr.Zero) Marshal.FreeHGlobal(cfgPtr);
                }

                //设置填充位
                uint pad = 0x55; // 默认填充值0x55
                cfgPtr = IntPtr.Zero;
                cfglstPtr = IntPtr.Zero;
                try
                {
                    cfgPtr = Marshal.AllocHGlobal(SCONFIG_SIZE);
                    cfglstPtr = Marshal.AllocHGlobal(SCONFIG_LIST_SIZE);
                    Marshal.StructureToPtr(new SCONFIG() { Parameter = IOC_GSET_PARM_ISO15765_PAD, Value = pad }, cfgPtr, false);
                    Marshal.StructureToPtr(new SCONFIG_LIST() { NumOfParams = 1, ConfigPtr = cfgPtr }, cfglstPtr, false);
                    errCode = _lib.PassThruIoctl(_channelId, ICP_SET_CONFIG, cfglstPtr, IntPtr.Zero);
                    if (errCode != STATUS_NOERROR)
                    {
                        _logger?.Warning($"设置填充位失败: {errorCode2str(errCode)}");
                    }
                }
                finally
                {
                    if (cfglstPtr != IntPtr.Zero) Marshal.FreeHGlobal(cfglstPtr);
                    if (cfgPtr != IntPtr.Zero) Marshal.FreeHGlobal(cfgPtr);
                }

                ////设置doip激活 20260421
                //IntPtr inputPtr = IntPtr.Zero;
                //try
                //{

                //    // 1. 分配结构体内存
                //    inputPtr = Marshal.AllocHGlobal(Marshal.SizeOf<EU_DOIP_ACTIVE>());

                //    // 2. 填充结构体数据
                //    EU_DOIP_ACTIVE doipConfig = new EU_DOIP_ACTIVE
                //    {
                //        PinNum = 8,                    // 默认8号引脚
                //        ActiveEnable = 1
                //    };

                //    // 3. 将结构体封送到非托管内存
                //    Marshal.StructureToPtr(doipConfig, inputPtr, false);

                //    // 4. 调用IOCTL
                //    // 注意：这里直接传递结构体指针，不需要 SCONFIG_LIST！
                //    errCode = _lib.PassThruIoctl(
                //        _deviceId,                           // 设备ID
                //        ICP_SET_DOIP_PULL_UP_ACTIVE,        // IOCTL命令
                //        inputPtr,                            // 输入：指向EU_DOIP_ACTIVE结构体
                //        IntPtr.Zero                          // 输出：不使用
                //    );

                //}
                //finally
                //{
                //    // 释放内存
                //    if (inputPtr != IntPtr.Zero)
                //        Marshal.FreeHGlobal(inputPtr);
                //}

                //清空缓存
                errCode = _lib.PassThruIoctl(_channelId, ICP_CLEAR_RX_BUFFER, IntPtr.Zero, IntPtr.Zero);
                if (errCode != STATUS_NOERROR)
                {
                    _logger?.Warning($"清空缓冲区失败: {errorCode2str(errCode)}");
                }

                _logger?.Information("通道配置完成");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "配置通道参数时发生严重异常");
                throw; // 配置失败应该抛出异常，因为后续通信无法进行
            }
            
        }

        /// <summary>
        /// 配置消息过滤器 — 告诉 J2534 硬件只接收特定 CAN ID 的消息
        /// 这是模块特定的配置，不同模块有不同的 RequestId / ResponseId
        /// </summary>
        /// <param name="protocol">协议 ID</param>
        /// <param name="reqId">请求 CAN ID（用于流控帧过滤）</param>
        /// <param name="rspId">响应 CAN ID（用于接收帧过滤）</param>
        /// <param name="extAddr">扩展地址（0 表示不使用扩展地址）</param>
        /// <param name="txFlags">发送标志</param>
        /// <param name="isFunctional">是否为功能寻址（目前仅用于日志标记，未来可扩展不同过滤策略）</param>
        private void SetupMessageFilter(uint protocol, uint reqId, uint rspId, byte extAddr, uint txFlags, bool isFunctional = false)
        {
            try
            {
                _logger?.Information($"配置消息过滤器 - RspId:0x{rspId:X}, ReqId:0x{reqId:X}, ExtAddr:0x{extAddr:X2}, " +
                                   $"Functional:{isFunctional}");

                // 构造过滤器的三个关键消息结构：
                // 1. maskMsgData — 掩码（哪些位需要比较）
                // 2. patternMsgData — 模式（期望匹配的值，即我们要接收的响应 ID）
                // 3. flowControlMsgData — 流控帧 ID（即我们发送的请求 ID）
                byte[] maskMsgData = GetFilterMaskId(extAddr);
                byte[] patternMsgData = GetFilterId(rspId, extAddr);
                byte[] flowControlMsgData = GetFilterId(reqId, extAddr);

                PASSTHRU_MSG mask = NewMsg(protocol, 0, txFlags, maskMsgData);
                PASSTHRU_MSG pattern = NewMsg(protocol, 0, txFlags, patternMsgData);
                PASSTHRU_MSG flowControl = NewMsg(protocol, 0, txFlags, flowControlMsgData);

                uint filterId;
                int errCode = _lib.PassThruStartMsgFilter(_channelId, FLOW_CONTROL_FILTER,
                    ref mask, ref pattern, ref flowControl, out filterId);

                if (errCode != STATUS_NOERROR)
                {
                    // 过滤器设置失败通常不是致命错误（取决于硬件实现），记录警告即可
                    _logger?.Warning($"设置消息过滤器失败: {errorCode2str(errCode)}");
                }
                else
                {
                    _logger?.Information($"消息过滤器设置成功, FilterId: {filterId}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Warning(ex, "设置消息过滤器时发生异常");
                // 过滤器异常也不应中断连接流程
            }
        }
        /// <summary>
        /// 发送和接收消息的核心方法
        /// </summary>
        /// <param name="wMsgs">要发送的消息数组</param>
        /// <param name="rMsgs">接收到的消息缓冲区</param>
        /// <returns>操作结果字符串</returns>
        private string SendRecv(PASSTHRU_MSG[] wMsgs, PASSTHRU_MSG[] rMsgs)
        {
            StringBuilder ret = new StringBuilder();


            //WriteMsg
            uint numMsgs = (uint)wMsgs.Length;
            uint timeout = 100;   // 超时时间100ms
            int errCode = _lib.PassThruWriteMsgs(_channelId, wMsgs, ref numMsgs, timeout);
            if (errCode != STATUS_NOERROR) return $"PassThruWriteMsgs: error code={errorCode2str(errCode)}";
            // 记录发送的消息内容
            for (int i = 0; i < numMsgs; i++)
            {
                ret.Append($"PassThruWriteMsgs: {BitConverter.ToString(wMsgs[i].Data, 0, (int)wMsgs[i].DataSize).Replace("-", " ")}\n");
            }

            //ReadMsg // 2. 接收响应
            int numNeedRecv = rMsgs.Length; // 期望接收的消息数量
            int numHaveRecv = 0;      // 已接收的消息数量
            Stopwatch sw = Stopwatch.StartNew();
            while (numNeedRecv > 0)
            {
                uint numRecv = (uint)numNeedRecv;
                PASSTHRU_MSG[] tmpMsgs = new PASSTHRU_MSG[numNeedRecv];
                // 读取消息
                errCode = _lib.PassThruReadMsgs(_channelId, tmpMsgs, ref numRecv, timeout);
                // 处理接收结果
                if (errCode == STATUS_NOERROR || errCode == ERR_TIMEOUT ||
                    (errCode == ERR_BUFFER_EMPTY && numRecv > 0))
                {
                    for (int i = 0; i < numRecv; i++)
                    {
                        // 过滤掉发送确认和开始消息
                        if ((tmpMsgs[i].RxStatus & TX_SUCCESS) == TX_SUCCESS) continue;
                        if ((tmpMsgs[i].RxStatus & START_OF_MESSAGE) == START_OF_MESSAGE) continue;
                        rMsgs[numHaveRecv++] = tmpMsgs[i];
                        numNeedRecv--;
                    }
                }
                // 超时检查
                if (sw.ElapsedMilliseconds > rMsgs.Length * timeout) break;
            }
            sw.Stop();
            // 3. 格式化输出接收到的消息
            if (numHaveRecv > 0)
            {
                for (int i = 0; i < numHaveRecv; i++)
                {
                    ret.Append($"PassThruReadMsgs: {BitConverter.ToString(rMsgs[i].Data, 0, (int)rMsgs[i].DataSize).Replace("-", " ")}");
                    if (i != numHaveRecv - 1) ret.Append("\n");
                }
            }
            else
            {
                ret.Append($"PassThruReadMsgs: error code={errorCode2str(errCode)}");
            }

            return ret.ToString();
        }
        /// <summary>
        /// 创建新的PASSTHRU_MSG消息结构
        /// </summary>
        private PASSTHRU_MSG NewMsg(uint ProtocolID, uint RxStatus, uint TxFlags, byte[] data)
        {
            PASSTHRU_MSG msg = new PASSTHRU_MSG
            {
                ProtocolID = ProtocolID,
                RxStatus = RxStatus,
                TxFlags = TxFlags,
                Timestamp = 0,
                DataSize = (uint)(data?.Length ?? 0),
                ExtraDataIndex = 0,
                Data = new byte[4128] // 分配最大缓冲区（支持CAN FD）
            };
            if (data != null) Array.Copy(data, msg.Data, data.Length);
            return msg;
        }
        /// <summary>
        /// 检查库是否已加载
        /// </summary>
        private ApiResult CheckLibraryLoaded()
        {
            if (_lib == null)
            {
                _logger?.Error("J2534库未加载");
                return ApiResult.Fail("NOT_LOADED", "J2534库未加载");
            }
            return ApiResult.Ok();
        }
        /// <summary>
        /// 验证CAN ID输入参数
        /// </summary>
        private static ApiResult CheckInputCanId(string requestId, string responseId, string strlinId, out uint reqId, out uint rspId, out byte linId)
        {
            reqId = GetCanId(requestId);
            rspId = GetCanId(responseId);
            uint extAddr = GetCanId(strlinId);
            linId = (byte)extAddr;
            if (reqId == 0)
            {
                return ApiResult.Fail("ID_ERROR", $"Invalid Request ID: {requestId}");
            }
            if (rspId == 0)
            {
                return ApiResult.Fail("ID_ERROR", $"Invalid Response ID: {responseId}");
            }
            if (extAddr > 0xFF) 
            {
                return ApiResult.Fail("ID_ERROR", $"Invalid Lin ID: {strlinId}");
            }
            return ApiResult.Ok();
        }
    }
}
