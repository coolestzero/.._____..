// 用途：诊断通信通用工具方法
// 所有功能（DTC、VIN等）都可以用这里的静态方法处理 VCI 返回的原始帧

using System;
using System.Linq;

namespace EOLTest.Services.Common
{
    /// <summary>
    /// 诊断工具类 —— 提供从 VCI 原始帧中提取有效负载等通用方法
    /// </summary>
    public static class DiagnosticUtils
    {
        /// <summary>
        /// 从 VCI 返回的完整帧数据中提取诊断有效负载
        /// 
        /// 帧格式（当前 VCI 实现）：
        ///   [0] [1] - 协议填充（通常为 0x00 0x00）
        ///   [2] [3] - CAN ID（高字节 + 低字节）
        ///   [4] [5] ... - 诊断数据（UDS 命令/响应）
        /// 
        /// 如果将来更换 VCI 设备，帧头长度可能变化，
        /// 只需在这里修改常量即可，所有功能无需改动。
        /// </summary>
        /// <param name="fullFrameData">VCI 返回的原始帧字节数组</param>
        /// <param name="headerLength">帧头长度（默认 4 字节）</param>
        /// <returns>诊断有效负载字节数组（如果数据不足帧头长度，返回空数组）</returns>
        public static byte[] ExtractPayload(byte[] fullFrameData, int headerLength = 4)
        {
            if (fullFrameData == null || fullFrameData.Length <= headerLength)
                return Array.Empty<byte>();

            // 跳过帧头，返回剩余部分
            return fullFrameData.Skip(headerLength).ToArray();
        }

        /// <summary>
        /// 将字节数组转为十六进制显示字符串（如 "59 02 08"）
        /// 用于日志输出
        /// </summary>
        public static string ToHexString(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;
            return BitConverter.ToString(data).Replace("-", " ");
        }
    }
}
