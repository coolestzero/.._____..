using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.API
{
    /// <summary>
    /// SAE J2534 标准常量与结构声明。
    /// 这个类包含了J2534 API的所有常量、错误码、协议ID、标志位和数据结构定义。
    /// </summary>
    internal class J2534Native
    {
        internal const int STATUS_NOERROR = 0x00000000; //Function call was successful
        internal const int ERR_NOT_SUPPORTED = 0x00000001; //Device does not support the API function.A fully compliant SAE J2534 - 1 Pass - Thru Interface shall never return ERR_NOT_SUPPORTED.
        internal const int ERR_INVALID_CHANNEL_ID = 0x00000002; //Invalid <ChannelID> value
        internal const int ERR_PROTOCOL_ID_NOT_SUPPORTED = 0x00000003; //<ProtocolID> value is not supported (either invalid or unknown)
        internal const int ERR_NULL_PARAMETER = 0x00000004; //NULL pointer supplied where a valid pointer is required
        internal const int ERR_IOCTL_VALUE_NOT_SUPPORTED = 0x00000005; //Value referenced in the SCONFIG_LIST structure is either invalid, out of range, or not applicable for the current channel
        internal const int ERR_FLAG_NOT_SUPPORTED = 0x00000006; //<Flags> value(s) are either invalid, unknown, or not applicable for the current channel
        internal const int ERR_FAILED = 0x00000007; //Undefined error, use PassThruGetLastError for text description.A fully compliant SAE J2534 - 1 Pass - Thru Interface shall never return ERR_FAILED.
        internal const int ERR_DEVICE_NOT_CONNECTED = 0x00000008; //Pass - Thru Device communication error.This indicates that the PassThru Interface DLL has, at some point, failed to communicate with the Pass - Thru Device – even though it may not currently be disconnected.
        internal const int ERR_TIMEOUT = 0x0000009; //Request could not be completed in
        internal const int ERR_INVALID_MSG = 0x0000000A; //Message structure is invalid for the given <ChannelID>(refer to Section 7.2.4 for more details)
        internal const int ERR_TIME_INTERVAL_NOT_SUPPORTED = 0x0000000B; //Value for the <TimeInterval> is either invalid or out of range for the current channel
        internal const int ERR_EXCEEDED_LIMIT = 0x0000000C; //Exceeded the allowed limits
        internal const int ERR_INVALID_MSG_ID = 0x0000000D; //Invalid <MsgID> value
        internal const int ERR_DEVICE_IN_USE = 0x0000000E; //Device is currently open
        internal const int ERR_IOCTL_ID_NOT_SUPPORTED = 0x0000000F; //<IoctlID> value is either invalid, unknown, or not applicable for the current channel
        internal const int ERR_BUFFER_EMPTY = 0x00000010; //The buffer is empty, no data available
        internal const int ERR_BUFFER_FULL = 0x00000011; //Buffer is full
        internal const int ERR_BUFFER_OVERFLOW = 0x00000012; //Indicates a buffer overflow occurred, data was lost
        internal const int ERR_PIN_NOT_SUPPORTED = 0x00000013; //Pin number and / or connector specified is either invalid or unknown
        internal const int ERR_RESOURCE_CONFLICT = 0x00000014; //Request causes a resource conflict (such as a pin on a connector is already in use, a data link controller is already in use, etc.)
        internal const int ERR_MSG_PROTOCOL_ID = 0x00000015; //Protocol ID in the PASSTHRU_MSG structure does not match the Protocol ID from the original call to PassThruConnect / PassThruLogicalConnect for the Channel ID
        internal const int ERR_INVALID_FILTER_ID = 0x00000016; //Invalid <FilterID> value
        internal const int ERR_MSG_NOT_ALLOWED = 0x00000017; //Attempting to queue a Segmented Message whose network address and / or <TxFlags> does not match those defined for the <RemoteAddress> or <RemoteTxFlags> during channel creation on a logical communication channel (This Return Value is only applicable to ISO 15765 logical communication channels)
        internal const int ERR_NOT_UNIQUE = 0x00000018; //Attempt was made to create a duplicate where one is not allowed.
        internal const int ERR_BAUDRATE_NOT_SUPPORTED = 0x00000019; //Baud rate is either invalid or unachievable for the current channel
        internal const int ERR_INVALID_DEVICE_ID = 0x0000001A; //PassThruOpen has been successfully called, but the current Device ID is not valid
        internal const int ERR_DEVICE_NOT_OPEN = 0x0000001B; //PassThruOpen has not been successfully called
        internal const int ERR_NULL_REQUIRED = 0x0000001C; //A parameter that is required to be NULL is not set to NULL
        internal const int ERR_FILTER_TYPE_NOT_SUPPORTED = 0x0000001D; //<FilterType> is either invalid orunknown for the current channelSAE INTERNATIONAL J2534 - 1 OCT2015 Page 111 of 128
        internal const int ERR_IOCTL_PARAM_ID_NOT_SUPPORTED = 0x0000001E; //Parameter referenced in the SCONFIG_LIST structure is not supported(either invalid, unknown, or not applicable for the current channel)
        internal const int ERR_VOLTAGE_IN_USE = 0x0000001F; //Programming voltage is currently being applied to another pin
        internal const int ERR_PIN_IN_USE = 0x00000020; //Pin number specified is currently in use(either for voltage, ground, or by another channel)
        internal const int ERR_INIT_FAILED = 0x00000021; //Physical vehicle bus initialization failed
        internal const int ERR_OPEN_FAILED = 0x00000022; //There is an invalid name or there is a configuration issue(e.g., firmware / DLL mismatch, etc.) and the associated device could not be opened – run the device configuration application(from the Pass - Thru Interface manufacturer) to resolve
        internal const int ERR_BUFFER_TOO_SMALL = 0x00000023; //The size of <DataBuffer>, as indicated by the parameter <DataBufferSize> in the PASSTHRU_MSG structure, is too small to accommodate the full message
        internal const int ERR_LOG_CHAN_NOT_ALLOWED = 0x00000024; //Logical communication channel is not allowed for the designated physical communication channel and Protocol ID combination
        internal const int ERR_SELECT_TYPE_NOT_SUPPORTED = 0x00000025; //<SelectType> is either invalid or unknown
        internal const int ERR_CONCURRENT_API_CALL = 0x00000026; //A J2534 API function has been called before the previous J2534 function call has completed

        //Protocol ID Values
        internal const uint PROTC_J1850VPW = 1;
        internal const uint PROTC_J1850PWM = 2;
        internal const uint PROTC_ISO9141 = 3;
        internal const uint PROTC_ISO14230 = 4;
        internal const uint PROTC_CAN = 5;
        internal const uint PROTC_ISO15765 = 6;
        internal const uint PROTC_SCI_A_ENGINE = 7;
        internal const uint PROTC_SCI_A_TRANS = 8;
        internal const uint PROTC_SCI_B_ENGINE = 9;
        internal const uint PROTC_SCI_B_TRANS = 0x0A;
        internal const uint PROTC_J1850VPW_PS = 0x8000;
        internal const uint PROTC_J1850PWM_PS = 0x8001;
        internal const uint PROTC_ISO9141_PS = 0x8002;
        internal const uint PROTC_ISO14230_PS = 0x8003;
        internal const uint PROTC_CAN_PS = 0x8004;
        internal const uint PROTC_ISO15765_PS = 0x8005;
        internal const uint PROTC_J2610_PS = 0x8006;
        internal const uint PROTC_SW_ISO15765_PS = 0x8007;
        internal const uint PROTC_SW_CAN_PS = 0x8008;
        internal const uint PROTC_GM_UART_PS = 0x8009;
        internal const uint PROTC_UART_ECHO_BYTE_PS = 0x800A;
        internal const uint PROTC_HONDA_DIAGH_PS = 0x800B;
        internal const uint PROTC_J1939_PS = 0x800C;
        internal const uint PROTC_J1708_PS = 0x800D;
        internal const uint PROTC_TP2_0_PS = 0x800E;
        internal const uint PROTC_FT_CAN_PS = 0x800F;
        internal const uint PROTC_FT_ISO15765_PS = 0x8010;
        internal const uint PROTC_FD_CAN_PS = 0x8011;
        internal const uint PROTC_FD_ISO15765_PS = 0x8012;
        internal const uint PROTC_ETHERNET_NDIS = 0x8013;
        internal const uint PROTC_CAN_CH1 = 0x9000;
        internal const uint PROTC_CAN_CH2 = 0x9001;
        internal const uint PROTC_J1850VPW_CH1 = 0x9080;
        internal const uint PROTC_J1850PWM_CH1 = 0x9100;
        internal const uint PROTC_ISO9141_CH1 = 0x9180;
        internal const uint PROTC_ISO9141_CH2 = 0x9181;
        internal const uint PROTC_ISO14230_CH1 = 0x9200;
        internal const uint PROTC_ISO14230_CH2 = 0x9201;
        internal const uint PROTC_ISO15765_CH1 = 0x9280;
        internal const uint PROTC_ISO15765_CH2 = 0x9281;
        internal const uint PROTC_SW_CAN_CH1 = 0x9300;
        internal const uint PROTC_SW_CAN_CH2 = 0x9301;
        internal const uint PROTC_SW_CAN_ISO15765_CH1 = 0x9380;
        internal const uint PROTC_SW_CAN_ISO15765_CH2 = 0x9381;
        internal const uint PROTC_J2610_CH1 = 0x9400;
        internal const uint PROTC_J2610_CH2 = 0x9401;
        internal const uint PROTC_FT_CAN_CH1 = 0x9480;
        internal const uint PROTC_FT_CAN_CH2 = 0x9481;
        internal const uint PROTC_FT_ISO15765_CH1 = 0x9500;
        internal const uint PROTC_FT_ISO15765_CH2 = 0x9501;
        internal const uint PROTC_GM_UART_CH1 = 0x9580;
        internal const uint PROTC_GM_UART_CH2 = 0x9581;
        internal const uint PROTC_ECHO_BYTE_CH1 = 0x9600;
        internal const uint PROTC_ECHO_BYTE_CH2 = 0x9601;
        internal const uint PROTC_HONDA_DIAGH_CH1 = 0x9680;
        internal const uint PROTC_HONDA_DIAGH_CH2 = 0x9681;
        internal const uint PROTC_J1939_CH1 = 0x9700;
        internal const uint PROTC_J1939_CH2 = 0x9701;
        internal const uint PROTC_J1708_CH1 = 0x9780;
        internal const uint PROTC_J1708_CH2 = 0x9781;
        internal const uint PROTC_TP2_0_CH1 = 0x9800;
        internal const uint PROTC_TP2_0_CH2 = 0x9801;
        internal const uint PROTC_FD_CAN_CH1 = 0x9880;
        internal const uint PROTC_FD_CAN_CH2 = 0x9881;
        internal const uint PROTC_FD_ISO15765_CH1 = 0x9900;
        internal const uint PROTC_FD_ISO15765_CH2 = 0x9901;
        internal const uint PROTC_ANALOG_IN_CH1 = 0xC000;

        //Connect Flag Values
        internal const uint FLAGS_K_LINE_ONLY = (1 << 12);
        internal const uint FLAGS_CAN_ID_BOTH = (1 << 11);
        internal const uint FLAGS_CHECKSUM_DISABLED = (1 << 9);
        internal const uint FLAGS_CAN_29BIT_ID = (1 << 8);
        internal const uint FLAGS_FULL_DUPLEX = (1 << 0);

        //RXSTATUS/TXFLAGS
        internal const uint TX_MSG_TYPE = (1 << 0);
        internal const uint START_OF_MESSAGE = (1 << 1);
        internal const uint RX_BREAK = (1 << 2);
        internal const uint TX_SUCCESS = (1 << 3);
        internal const uint ISO15765_PADDING = (1 << 4);
        internal const uint ERROR_INDICATION = (1 << 5);
        internal const uint BUFFER_OVERFLOW = (1 << 6);
        internal const uint ISO15765_FRAME_PAD = (1 << 6);
        internal const uint ISO15765_ADDR_TYPE = (1 << 7);
        internal const uint CAN_29BIT_ID = (1 << 8);
        internal const uint TX_FAILED = (1 << 9);
        internal const uint WAIT_P3_MIN_ONLY = (1 << 9);
        internal const uint FD_CAN_BRS = (1 << 19);
        internal const uint FD_CAN_FORMAT = (1 << 20);
        internal const uint FD_CAN_ESI = (1 << 21);
        internal const uint SCI_MODE = (1 << 22);
        internal const uint SCI_TX_VOLTAGE = (1 << 23);
        internal const uint BOSCH_FD_FORMAT = (1 << 24);
        internal const uint BOSCH_FD_BRS = (1 << 25);

        //Filter Type Values
        internal const uint PASS_FILTER = 1;
        internal const uint BLOCK_FILTER = 2;
        internal const uint FLOW_CONTROL_FILTER = 3;

        //Ioctl ID Values
        internal const uint ICP_GET_CONFIG = 0x01; //
        internal const uint ICP_SET_CONFIG = 0x02; //
        internal const uint ICP_READ_VBATT = 0x03; //
        internal const uint ICP_FIVE_BAUD_INIT = 0x04; //
        internal const uint ICP_FAST_INIT = 0x05; //
        internal const uint ICP_CLEAR_TX_BUFFER = 0x07; //
        internal const uint ICP_CLEAR_RX_BUFFER = 0x08; //
        internal const uint ICP_CLEAR_PERIODIC_MSGS = 0x09; //
        internal const uint ICP_CLEAR_MSG_FILTERS = 0x0A; //
        internal const uint ICP_CLEAR_FUNCT_MSG_LOOKUP_TABLE = 0x0B; //
        internal const uint ICP_ADD_TO_FUNCT_MSG_LOOKUP_TABLE = 0x0C; //
        internal const uint ICP_DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE = 0x0D; //
        internal const uint ICP_READ_PROG_VOLTAGE = 0x0E;  //
        internal const uint ICP_SW_CAN_HS = 0x8000;
        internal const uint ICP_SW_CAN_NS = 0x8001;
        internal const uint ICP_POLL_RESPONSE = 0x8002;
        internal const uint ICP_BECOME_MASTER = 0x8003;
        internal const uint ICP_START_REPEAT_MESSAGE = 0x8004;
        internal const uint ICP_QUERY_REPEAT_MESSAGE = 0x8005;
        internal const uint ICP_STOP_REPEAT_MESSAGE = 0x8006;
        internal const uint ICP_GET_DEVICE_CONFIG = 0x8007;
        internal const uint ICP_SET_DEVICE_CONFIG = 0x8008;
        internal const uint ICP_PROTECT_J1939_ADDR = 0x8009;
        internal const uint ICP_REQUEST_CONNECTION = 0x800A;
        internal const uint ICP_TEARDOWN_CONNECTION = 0x800B;
        internal const uint ICP_GET_DEVICE_INFO = 0x800C;
        internal const uint ICP_GET_PROTOCOL_INFO = 0x800D;
        internal const uint ICP_READ_J1962PIN_VOLTAGE = 0x800E;
        internal const uint ICP_GET_NDIS_ADAPTER_INFO = 0x800F;
        internal const uint ICP_DETECT_BAUDRATE_INIT = 0x10000;
        internal const uint ICP_GET_PIN_VOLTAGE = 0x10001;
        internal const uint CLEAR_LAST_USED_DEVICE = 0x10001;//BOSCH
        internal const uint ICP_SET_PIN_STATUS = 0x10002;
        internal const uint GET_DEVICE_SERIAL_NUMBER = 0x10002;
        internal const uint ICP_SET_DEVICE_SERIALNUMBER = 0x10010;
        internal const uint ICP_GET_DEVICE_DATA = 0x10011;
        internal const uint ICP_SET_DEVICE_OPERATION = 0x10012;
        // DoIP (以太网诊断)相关
        internal const uint ICP_SET_DOIP_PULL_UP_ACTIVE = 0x10020;  /* 8号脚上拉使能 */
        internal const uint ICP_SET_DOIP_ETH_PIN_ASSIGNMENT_OPTION = 0x10021;   /* 选项1为（PIN03PIN11PIN12PIN13），选项2为（PIN01PIN09PIN12PIN13） */
        internal const uint ICP_SET_DOIP_RJ45_ROUTE_LAN9514_OPTION = 0x10022;   /* 两种情况，一种为上位机网口与OBD网口直连，另外一种为上位机usb连下位机9514转RJ45再连OBD网口 */
        internal const uint ICP_ASYNC_FIVE_BAUD_INIT = 0x10030; /* 与 FIVE_BAUD_INIT 一致，区别是非阻塞，此接口立即返回，通过 PassThruReadMsgs
                                                           		* 获取到 RXSTATUS_FIVE_BAUD_INIT_LOST 或者 RXSTATUS_FIVE_BAUD_INIT_ESTABLISHED
                                                           		* 来判断 FIVE_BAUD_INIT 是否成功*/
        internal const uint ICP_ASYNC_FAST_INIT = 0x10031;
        internal const uint ICP_ASYNC_DETECT_BAUDRATE_INIT = 0x10032;	/* 与 FIVE_BAUD_INIT 一致，区别是非阻塞，此接口立即返回，通过 PassThruReadMsgs 
        														        * 获取到 RXSTATUS_FIVE_BAUD_INIT_LOST 或者 RXSTATUS_FIVE_BAUD_INIT_ESTABLISHED
        														        * 来判断 FIVE_BAUD_INIT 是否成功*/
        internal const uint ICP_GET_LOG_BUFFER_SIZE = 0x1004A;
        internal const uint ICP_GET_LOG_BUFFER = 0x1004B;
        internal const uint ICP_HONDA_SELF_INTERFACE_1 = 0x100000;
        internal const uint ICP_UNSUPORT = 0xffffffff;

        //IOCTL GET_CONFIG/SET_CONFIG PARAMETER
        internal const uint IOC_GSET_PARM_DATA_RATE = 1;
        internal const uint IOC_GSET_PARM_LINK_LOOPBACK = 3;
        internal const uint IOC_GSET_PARM_NODE_ADDRESS = 4;
        internal const uint IOC_GSET_PARM_NETWORK_LINE = 5;
        internal const uint IOC_GSET_PARM_P1_MIN = 6;
        internal const uint IOC_GSET_PARM_P1_MAX = 7;
        internal const uint IOC_GSET_PARM_P2_MIN = 8;
        internal const uint IOC_GSET_PARM_P2_MAX = 9;
        internal const uint IOC_GSET_PARM_P3_MIN = 0x0A;
        internal const uint IOC_GSET_PARM_P3_MAX = 0x0B;
        internal const uint IOC_GSET_PARM_P4_MIN = 0x0C;
        internal const uint IOC_GSET_PARM_P4_MAX = 0x0D;
        internal const uint IOC_GSET_PARM_W1 = 0x0E;
        internal const uint IOC_GSET_PARM_W2 = 0x0F;
        internal const uint IOC_GSET_PARM_W3 = 0x10;
        internal const uint IOC_GSET_PARM_W4 = 0x11;
        internal const uint IOC_GSET_PARM_W5 = 0x12;
        internal const uint IOC_GSET_PARM_TIDLE = 0x13;
        internal const uint IOC_GSET_PARM_TINIL = 0x14;
        internal const uint IOC_GSET_PARM_TWUP = 0x15;
        internal const uint IOC_GSET_PARM_PARITY = 0x16;
        internal const uint IOC_GSET_PARM_BIT_SAMPLE_POINT = 0X17;
        internal const uint IOC_GSET_PARM_SYNC_JUMP_WIDTH = 0x18;
        internal const uint IOC_GSET_PARM_W0 = 0x19;
        internal const uint IOC_GSET_PARM_T1_MAX = 0x1A;
        internal const uint IOC_GSET_PARM_T2_MAX = 0x1B;
        internal const uint IOC_GSET_PARM_T4_MAX = 0x1C;
        internal const uint IOC_GSET_PARM_T5_MAX = 0x1D;
        internal const uint IOC_GSET_PARM_ISO15765_BS = 0x1E;
        internal const uint IOC_GSET_PARM_ISO15765_STMIN = 0x1F;
        internal const uint IOC_GSET_PARM_DATA_BITS = 0x20;
        internal const uint IOC_GSET_PARM_FIVE_BAUD_MOD = 0x21;
        internal const uint IOC_GSET_PARM_BS_TX = 0x22;
        internal const uint IOC_GSET_PARM_STMIN_TX = 0x23;
        internal const uint IOC_GSET_PARM_T3_MAX = 0x24;
        internal const uint IOC_GSET_ISO15765_WFT_MAX = 0x25;
        internal const uint IOC_GSET_PARM_CAN_MIXED_FORMAT = 0x8000;
        internal const uint IOC_GSET_PARM_J1962_PINS = 0x8001;
        internal const uint IOC_GSET_PARM_SW_CAN_HS_DATA_RATE = 0x8010;
        internal const uint IOC_GSET_PARM_SW_CAN_SPEEDCHANGE_ENABLE = 0x8011;
        internal const uint IOC_GSET_PARM_SW_CAN_RES_SWITCH = 0x8012;
        internal const uint IOC_GSET_PARM_ACTIVE_CHANNELS = 0x8020;
        internal const uint IOC_GSET_PARM_SAMPLE_RATE = 0x8021;
        internal const uint IOC_GSET_PARM_SAMPLES_PER_READING = 0x8022;
        internal const uint IOC_GSET_PARM_READINGS_PER_MSG = 0x8023;
        internal const uint IOC_GSET_PARM_AVERAGING_METHOD = 0x8024;
        internal const uint IOC_GSET_PARM_SAMPLE_RESOLUTION = 0x8025;
        internal const uint IOC_GSET_PARM_INPUT_RANGE_LOW = 0x8026;
        internal const uint IOC_GSET_PARM_INPUT_RANGE_HIGH = 0x8027;
        internal const uint IOC_GSET_PARM_UEB_T0_MIN = 0x8028;
        internal const uint IOC_GSET_PARM_UEB_T1_MAX = 0x8029;
        internal const uint IOC_GSET_PARM_UEB_T2_MAX = 0x802A;
        internal const uint IOC_GSET_PARM_UEB_T3_MAX = 0x802B;
        internal const uint IOC_GSET_PARM_UEB_T4_MIN = 0x802C;
        internal const uint IOC_GSET_PARM_UEB_T5_MAX = 0x802D;
        internal const uint IOC_GSET_PARM_UEB_T6_MAX = 0x802E;
        internal const uint IOC_GSET_PARM_UEB_T7_MIN = 0x802F;
        internal const uint IOC_GSET_PARM_UEB_T7_MAX = 0x8030;
        internal const uint IOC_GSET_PARM_UEB_T9_MIN = 0x8031;
        internal const uint IOC_GSET_PARM_J1939_PINS = 0x803D;
        internal const uint IOC_GSET_PARM_J1708_PINS = 0x803E;
        internal const uint IOC_GSET_PARM_J1939_T1 = 0x803F;
        internal const uint IOC_GSET_PARM_J1939_T2 = 0x8040;
        internal const uint IOC_GSET_PARM_J1939_T3 = 0x8041;
        internal const uint IOC_GSET_PARM_J1939_T4 = 0x8042;
        internal const uint IOC_GSET_PARM_J1939_BRDCST_MIN_DELAY = 0x8043;
        internal const uint IOC_GSET_PARM_TP2_0_T_BR_INT = 0x8044;
        internal const uint IOC_GSET_PARM_TP2_0_T_E = 0x8045;
        internal const uint IOC_GSET_PARM_TP2_0_MNTC = 0x8046;
        internal const uint IOC_GSET_PARM_TP2_0_T_CTA = 0x8047;
        internal const uint IOC_GSET_PARM_TP2_0_MNCT = 0x8048;
        internal const uint IOC_GSET_PARM_TP2_0_MNTB = 0x8049;
        internal const uint IOC_GSET_PARM_TP2_0_MNT = 0x804A;
        internal const uint IOC_GSET_PARM_TP2_0_T_WAIT = 0x804B;
        internal const uint IOC_GSET_PARM_TP2_0_T1 = 0x804C;
        internal const uint IOC_GSET_PARM_TP2_0_T3 = 0x804D;
        internal const uint IOC_GSET_PARM_TP2_0_IDENTIFER = 0x804E;
        internal const uint IOC_GSET_PARM_TP2_0_RXIDPASSIVE = 0x804F;
        /* CAN FD in J2534-2 JAN2019 */
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        internal const uint IOC_GSET_PARM_FD_CAN_DATA_PHASE_RATE = 0x805C;	/* The data rate used for the data section of a CAN FD message. The data phase rate cannot be less than the arbitration data rate.
        																	* Valid Values for Parameter 250000 500000 1000000 2000000 4000000 and 5000000 */
        internal const uint IOC_GSET_PARM_FD_ISO15765_TX_DATA_LENGTH = 0x805D;	/* The data phase data size used for ISO15765 single frames first frames and consecutive frames. 
        																		* Note: The last frame shall always use the smallest data length possible.
                                                                                * Valid Values for Parameter 8 12 16 20 24 32 48 64 */
        internal const uint IOC_GSET_PARM_HS_CAN_TERMINATION = 0x805E;	/* 0	no termination;  3	120-ohm termination */
        internal const uint IOC_GSET_PARM_N_CR_MAX = 0x805F;	/* Consecutive frame timeout. */
        internal const uint IOC_GSET_PARM_ISO15765_PAD_VALUE = 0x8060;  /* Pad byte used to pad frames. */
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        internal const uint IOC_GSET_PARM_ISO15765_FLOWCTRL_AUTO = 0x10000;
        internal const uint IOC_GSET_PARM_ISO15765_PAD = 0x10001;
        internal const uint IOC_GSET_PARM_ISO15765_FLOWCTRL_DELAY = 0x10002;
        internal const uint IOC_GSET_PARM_TP1_6_KWP2K = 0x10003;
        internal const uint IOC_GSET_PARM_ASSIST_PIN = 0x10004;
        internal const uint IOC_GSET_PARM_UEB_RECV_IDMSGS = 0x10005;
        internal const uint IOC_GSET_PARM_UEB_ADRCODE_PARITY = 0x10006;
        internal const uint IOC_GSET_PARM_ISO15765_FLOWCTRL_AUTO_ID_RECV_SUB_SEND = 0x10007;
        internal const uint IOC_GSET_PARM_KLINE_RESOURCE_KX = 0x10008; /*选择K1、K2、K3、K4通道 
        																* 值为0，此值为默认值 K线使用哪条K通道，下位机按照下位机的规则决定，
        																* 值为1，K线使用K1通道
                                                                        * 值为2，K线使用K2通道
                                                                        * 值为3，K线使用K3通道
                                                                        * 值为4，K线使用K4通道*/

        internal const uint IOC_GSET_PARM_CAN_RESOURCE_CAN_CHANNEL = 0x10009;	/* 选择HSCAN1、HSCAN2、HSCAN2-BUSC、SWCAN-BUSA、SWCAN-BUSB
        																		* 值为0，此值为默认值 CAN使用哪条通道，下位机按照下位机的规则决定，
        																		* 值为1，使用HSCAN1通道
                                                                                * 值为2，使用HSCAN2通道
                                                                                * 值为3，使用HSCAN2接BUSC通道
                                                                                * 值为4，SWCAN使用BUSA通道
                                                                                * 值为5，SWCAN使用BUSB通道*/

        internal const uint IOC_GSET_PARM_ISO15765_ENABLE_TX_7_DATA_LENGTH = 0x1000A; /* 当数据长度为7时按照分帧发送
        																		       * 值为0，数据长度大于7时，ISO15765分帧发送，数据长度小于等于7，单帧发送
                                                                                       * 值为1，数据长度等于7时，ISO15765分帧发送，数据长度小于7，单帧发送*/
        internal const uint IOC_GSET_TP2_0_BR_SEND_CNT = 0x1000B; /* TP20广播消息发送次数,默认为5次 */
        internal const uint IOC_GSET_PARM_RECV_CAN_HW_CTRL_ERROR = 0x1000C; /* 是否使能CAN控制器错误消息接收 */
        internal const uint IOC_GSET_PARM_ISO15765_FIRST_CF_SN = 0x1000D; /* 第一个续发帧序号 */

        /* CAN FD in GMW17753 */
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        internal const uint IOC_GSET_CAN_FD_DATA_PHASE_RATE = 0x10010; /* Specifies the baud rate for the data phase of a CAN FD frame in bits per second (bps).
        																* To minimize the possibility of Bus Errors the Data phase rate should be set prior to
        																* setting J1962_PINS.*/
        internal const uint IOC_GSET_CAN_FD_TC_DATA_LENGTH = 0x10011;	/* Supported by ISO15765_FD_PS only. The frame size the Pass-Thru Interface shall use
        																 * when transmitting a CAN FD formatted segmented message. Transmit flag CAN_FD_FORMAT
                                                                         * determines the frame format of the message. For the CAN 2.0 format the frame size
        															     * is always 8 bytes.
                                                                         * 8 bytes (default) 12 16 20 24 32 48 64 bytes*/
        internal const uint IOC_GSET_CAN_FD_TERMINATION = 0x10012;	/* The termination resistor shall be applied to across the DLC pins specified by J1962_PINS.
        														     * To minimize the possibility of Bus Errors termination should be set prior to setting J1962_PINS.
                                                                     * 0	no termination(Default).
        															 * 3	120 Ω termination.*/
        internal const uint IOC_GSET_CAN_FD_TYPE = 0x10013;	/*Switches between Bosch CAN FD and ISO CAN FD definitions. To minimize the possibility of
        													* Bus Errors CAN FD type should be set prior to setting J1962_PINS.
        													* 0	ISO CAN FD (Default).
        													* 1	Bosch CAN FD.*/
        internal const uint IOC_GSET_N_CR_MAX = 0x10014;    /* Supported by ISO15765_FD_PS.
        													* The maximum time between consecutive frames of a ISO 15765 Message that is being received.
        													* If this time limit is detected the Pass-Thru Device shall discard the message.
        													* Resolution: 1 μs.
                                                            * Default value: 150 000.*/
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        internal const uint IOC_GSET_PARM_CAN_FD_ADAPTER_FIRMWARE_VER = 0x2000D;


        internal const uint NON_VOLATILE_SN_0 = 0x00010000;  // 序列号0，OTP Block0
        internal const uint NON_VOLATILE_SN_1 = 0x00010001;  // 序列号1，
        internal const uint NON_VOLATILE_SN_2 = 0x00010002;  // 序列号2，
        internal const uint NON_VOLATILE_SN_3 = 0x00010003;  // 序列号3，


        // ===== 结构体声明 =====
        [StructLayout(LayoutKind.Sequential)]
        internal struct PASSTHRU_MSG
        {
            public uint ProtocolID;
            public uint RxStatus;
            public uint TxFlags;
            public uint Timestamp;
            public uint DataSize;
            public uint ExtraDataIndex;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4128)]
            public byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SCONFIG
        {
            public uint Parameter;
            public uint Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SCONFIG_LIST
        {
            public uint NumOfParams;
            public IntPtr ConfigPtr;
        }

        /// <summary>
        /// DoIP激活结构体
        /// 用于 ICP_SET_DOIP_PULL_UP_ACTIVE IOCTL 命令
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EU_DOIP_ACTIVE
        {
            public uint PinNum;        // 引脚号，默认8
            public uint ActiveEnable;  // 1为开启，0为关闭（注意：文档写2为关闭，但实际可能是0）
        }
    }
}
