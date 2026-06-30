using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.API
{
    internal class APIUtils
    {
        // 错误码转字符串：将J2534 API返回的错误代码转换为可读的描述字符串
        public static string errorCode2str(int ret)
        {
            switch (ret)
            {
                case J2534Native.STATUS_NOERROR: return "STATUS_NOERROR";
                case J2534Native.ERR_NOT_SUPPORTED: return "ERR_NOT_SUPPORTED";
                case J2534Native.ERR_INVALID_CHANNEL_ID: return "ERR_INVALID_CHANNEL_ID";
                case J2534Native.ERR_PROTOCOL_ID_NOT_SUPPORTED: return "ERR_PROTOCOL_ID_NOT_SUPPORTED";
                case J2534Native.ERR_NULL_PARAMETER: return "ERR_NULL_PARAMETER";
                case J2534Native.ERR_IOCTL_VALUE_NOT_SUPPORTED: return "ERR_IOCTL_VALUE_NOT_SUPPORTED";
                case J2534Native.ERR_FLAG_NOT_SUPPORTED: return "ERR_FLAG_NOT_SUPPORTED";
                case J2534Native.ERR_FAILED: return "ERR_FAILED";
                case J2534Native.ERR_DEVICE_NOT_CONNECTED: return "ERR_DEVICE_NOT_CONNECTED";
                case J2534Native.ERR_TIMEOUT: return "ERR_TIMEOUT";
                case J2534Native.ERR_INVALID_MSG: return "ERR_INVALID_MSG";
                case J2534Native.ERR_TIME_INTERVAL_NOT_SUPPORTED: return "ERR_TIME_INTERVAL_NOT_SUPPORTED";
                case J2534Native.ERR_EXCEEDED_LIMIT: return "ERR_EXCEEDED_LIMIT";
                case J2534Native.ERR_INVALID_MSG_ID: return "ERR_INVALID_MSG_ID";
                case J2534Native.ERR_DEVICE_IN_USE: return "ERR_DEVICE_IN_USE";
                case J2534Native.ERR_IOCTL_ID_NOT_SUPPORTED: return "ERR_IOCTL_ID_NOT_SUPPORTED";
                case J2534Native.ERR_BUFFER_EMPTY: return "ERR_BUFFER_EMPTY";
                case J2534Native.ERR_BUFFER_FULL: return "ERR_BUFFER_FULL";
                case J2534Native.ERR_BUFFER_OVERFLOW: return "ERR_BUFFER_OVERFLOW";
                case J2534Native.ERR_PIN_NOT_SUPPORTED: return "ERR_PIN_NOT_SUPPORTED";
                case J2534Native.ERR_RESOURCE_CONFLICT: return "ERR_RESOURCE_CONFLICT";
                case J2534Native.ERR_MSG_PROTOCOL_ID: return "ERR_MSG_PROTOCOL_ID";
                case J2534Native.ERR_INVALID_FILTER_ID: return "ERR_INVALID_FILTER_ID";
                case J2534Native.ERR_MSG_NOT_ALLOWED: return "ERR_MSG_NOT_ALLOWED";
                case J2534Native.ERR_NOT_UNIQUE: return "ERR_NOT_UNIQUE";
                case J2534Native.ERR_BAUDRATE_NOT_SUPPORTED: return "ERR_BAUDRATE_NOT_SUPPORTED";
                case J2534Native.ERR_INVALID_DEVICE_ID: return "ERR_INVALID_DEVICE_ID";
                case J2534Native.ERR_DEVICE_NOT_OPEN: return "ERR_DEVICE_NOT_OPEN";
                case J2534Native.ERR_NULL_REQUIRED: return "ERR_NULL_REQUIRED";
                case J2534Native.ERR_FILTER_TYPE_NOT_SUPPORTED: return "ERR_FILTER_TYPE_NOT_SUPPORTED";
                case J2534Native.ERR_IOCTL_PARAM_ID_NOT_SUPPORTED: return "ERR_IOCTL_PARAM_ID_NOT_SUPPORTED";
                case J2534Native.ERR_VOLTAGE_IN_USE: return "ERR_VOLTAGE_IN_USE";
                case J2534Native.ERR_PIN_IN_USE: return "ERR_PIN_IN_USE";
                case J2534Native.ERR_INIT_FAILED: return "ERR_INIT_FAILED";
                case J2534Native.ERR_OPEN_FAILED: return "ERR_OPEN_FAILED";
                case J2534Native.ERR_BUFFER_TOO_SMALL: return "ERR_BUFFER_TOO_SMALL";
                case J2534Native.ERR_LOG_CHAN_NOT_ALLOWED: return "ERR_LOG_CHAN_NOT_ALLOWED";
                case J2534Native.ERR_SELECT_TYPE_NOT_SUPPORTED: return "ERR_SELECT_TYPE_NOT_SUPPORTED";
                case J2534Native.ERR_CONCURRENT_API_CALL: return "ERR_CONCURRENT_API_CALL";
                default: return "UNKONWN";
            }
        }
        /// <summary>
        /// 将十六进制字符串转换为字节数组
        /// 支持以下输入格式：
        ///   "19 02 FF 00"   → 带空格
        ///   "1902FF00"      → 连续无空格
        ///   "19,02,FF,00"   → 逗号分隔
        ///   "0x19 0x02"     → 带 0x 前缀
        ///   "19 02 FF 00 "  → 前后有多余空格
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>转换后的字节数组，如果输入无效则返回空数组</returns>
        public static byte[] HexString2HexData(string hex)
        {
            // 空值处理
            if (string.IsNullOrEmpty(hex))
                return new byte[0];

            try
            {
                // ========== 第 1 步：清洗字符串 ==========
                // 去掉所有空白字符（空格、\t、\r、\n）和逗号
                // 这样无论原来有没有分隔符，最终都变成一个纯十六进制字符串
                string cleaned = hex
                    .Replace(" ", "")       // 去掉空格
                    .Replace("\t", "")      // 去掉制表符
                    .Replace("\r", "")      // 去掉回车
                    .Replace("\n", "")      // 去掉换行
                    .Replace(",", "");      // 去掉逗号（支持 "19,02,FF,00" 格式）

                // 去掉所有 "0x" 或 "0X" 前缀（支持 "0x19 0x02" 格式）
                // 注意要忽略大小写
                cleaned = cleaned.Replace("0x", "", StringComparison.OrdinalIgnoreCase);

                // 如果清洗后为空，返回空数组
                if (cleaned.Length == 0)
                    return new byte[0];

                // ========== 第 2 步：校验长度 ==========
                // 十六进制每 2 个字符表示 1 个字节，所以长度必须是偶数
                if (cleaned.Length % 2 != 0)
                {
                    // 长度奇数，可能是漏了一个 0，例如 "F" 应该是 "0F"
                    // 这里在开头补一个 '0'，使其变成偶数长度
                    cleaned = "0" + cleaned;
                }

                // ========== 第 3 步：逐对转换 ==========
                int byteCount = cleaned.Length / 2;
                byte[] result = new byte[byteCount];

                for (int i = 0; i < byteCount; i++)
                {
                    // 每次取 2 个字符，从高位到低位解析
                    string hexPair = cleaned.Substring(i * 2, 2);

                    // 将 2 位十六进制字符串转为 1 个字节
                    // Convert.ToByte 遇到非法字符（如 "GH"）会抛异常，被外层 catch 捕获
                    result[i] = Convert.ToByte(hexPair, 16);
                }

                return result;
            }
            catch (Exception)
            {
                // 转换失败（非法字符等原因），返回空数组
                return new byte[0];
            }
        }
        // 获取CAN ID：将字符串格式的CAN ID转换为无符号整数
        public static uint GetCanId(string canId)
        {
            if (string.IsNullOrEmpty(canId)) return 0;

            uint id = 0;
            try
            {
                canId = canId.Trim();
                if (canId.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) canId = canId.Substring(2);
                // 解析十六进制字符串
                id = uint.Parse(canId, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception)
            {
                id = 0; // 解析失败返回0
            }
            return id;
        }
        // 获取过滤器掩码ID：根据扩展地址生成过滤器掩码
        // 掩码用于决定哪些位需要匹配（1表示需要匹配，0表示不关心）
        public static byte[] GetFilterMaskId(byte extAddr)
        {
            // 5字节掩码（29位CAN ID + 扩展地址）
            if (extAddr != 0) return new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            // 4字节掩码（11位标准CAN ID）
            return new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        }
        // 获取过滤器ID：将CAN ID和扩展地址转换为字节数组格式的过滤器ID
        public static byte[] GetFilterId(uint canId, byte extAddr)
        {
            // 决定数组长度：有扩展地址为5字节，否则为4字节
            byte[] id = new byte[extAddr != 0 ? 5 : 4];
            // 将CAN ID的4个字节分别存储（大端序）
            id[0] = (byte)((canId >> 24) & 0xFF);
            id[1] = (byte)((canId >> 16) & 0xFF);
            id[2] = (byte)((canId >> 8) & 0xFF);
            id[3] = (byte)(canId & 0xFF);
            if (extAddr != 0) id[4] = extAddr;

            return id;
        }
        // 获取请求数据：构造J2534消息的数据部分（CAN ID + 扩展地址 + 实际数据）
        public static byte[] GetReqestData(uint reqId, byte extAddr, byte[] reqData)
        {
            // 计算总长度：CAN ID(4字节) + 扩展地址(0或1字节) + 实际数据长度
            byte[] data = new byte[(extAddr != 0 ? 5 : 4) + reqData.Length];
            // 填充CAN ID（大端序）
            data[0] = (byte)((reqId >> 24) & 0xFF);
            data[1] = (byte)((reqId >> 16) & 0xFF);
            data[2] = (byte)((reqId >> 8) & 0xFF);
            data[3] = (byte)(reqId & 0xFF);
            if (extAddr != 0)
            {
                data[4] = extAddr;
                Array.Copy(reqData, 0, data, 5, reqData.Length);
            }
            else
            {
                Array.Copy(reqData, 0, data, 4, reqData.Length);
            }
            return data;
        }
    }
}
