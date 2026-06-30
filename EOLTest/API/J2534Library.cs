using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static EOLTest.API.J2534Native;

namespace EOLTest.API
{
    // J2534Library类：封装J2534 DLL的动态加载和函数调用，实现IDisposable接口用于资源清理
    internal sealed class J2534Library : IDisposable
    {
        // Win32 动态加载方法
        // 原生Windows API函数声明，用于动态加载DLL
        private static class Native
        {
            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibrary(string lpFileName); // 加载DLL到进程内存

            [DllImport("kernel32", SetLastError = true)]
            public static extern bool FreeLibrary(IntPtr hModule); // 卸载DLL

            [DllImport("kernel32", SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName); // 获取函数地址
        }

        // J2534 函数委托（StdCall） // J2534标准API函数委托声明（使用StdCall调用约定，这是Windows API标准）
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruOpenDelegate(IntPtr pName, out uint deviceId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruCloseDelegate(uint deviceId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruConnectDelegate(uint deviceId, uint protocolId, uint flags, uint baudRate, out uint channelId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruDisconnectDelegate(uint channelId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruReadMsgsDelegate(uint channelId, IntPtr pMsg, ref uint numMsgs, uint timeout);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruWriteMsgsDelegate(uint channelId, IntPtr pMsg, ref uint numMsgs, uint timeout);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruStartPeriodicMsgDelegate(uint channelId, ref PASSTHRU_MSG msg, ref uint msgId, uint timeInterVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruStopPeriodicMsgDelegate(uint channelId, uint msgId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruStartMsgFilterDelegate(uint channelId, uint filterType, ref PASSTHRU_MSG maskMsg, ref PASSTHRU_MSG patternMsg, ref J2534Native.PASSTHRU_MSG flowControlMsg, out uint filterId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruStopMsgFilterDelegate(uint channelId, uint filterId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruSetProgrammingVoltageDelegate(uint deviceId, uint pinNumber, uint voltage);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruReadVersionDelegate(uint deviceId, StringBuilder firmwareVersion, StringBuilder dllVersion, StringBuilder apiVersion);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruGetLastErrorDelegate(StringBuilder errorDescription);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PassThruIoctlDelegate(uint channelId, uint ioctlId, IntPtr input, IntPtr output);
        // 私有字段：存储DLL句柄和函数委托
        private readonly IntPtr _dllHandle; // DLL模块句柄
        private readonly PassThruOpenDelegate _passThruOpen;
        private readonly PassThruCloseDelegate _passThruClose;
        private readonly PassThruConnectDelegate _passThruConnect;
        private readonly PassThruDisconnectDelegate _passThruDisconnect;
        private readonly PassThruReadMsgsDelegate _passThruReadMsgs;
        private readonly PassThruWriteMsgsDelegate _passThruWriteMsgs;
        private readonly PassThruStartPeriodicMsgDelegate _passThruStartPeriodicMsg;
        private readonly PassThruStopPeriodicMsgDelegate _passThruStopPeriodicMsg;
        private readonly PassThruStartMsgFilterDelegate _passThruStartMsgFilter;
        private readonly PassThruStopMsgFilterDelegate _passThruStopMsgFilter;
        private readonly PassThruSetProgrammingVoltageDelegate _passThruSetProgrammingVoltage;
        private readonly PassThruReadVersionDelegate _passThruReadVersion;
        private readonly PassThruGetLastErrorDelegate _passThruGetLastError;
        private readonly PassThruIoctlDelegate _passThruIoctl;
        // 构造函数：加载J2534 DLL并获取所有API函数地址
        public J2534Library(string dllPath)
        {
            if (string.IsNullOrWhiteSpace(dllPath))
                throw new ArgumentException("Invalid DLL Path");
            // 1. 加载DLL到进程内存
            _dllHandle = Native.LoadLibrary(dllPath);
            if (_dllHandle == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to Load J2534 DLL: {dllPath} (Win32Error={Marshal.GetLastWin32Error()})");
            // 2. 加载所有J2534 API函数
            _passThruOpen = LoadFunc<PassThruOpenDelegate>("PassThruOpen");
            _passThruClose = LoadFunc<PassThruCloseDelegate>("PassThruClose");
            _passThruConnect = LoadFunc<PassThruConnectDelegate>("PassThruConnect");
            _passThruDisconnect = LoadFunc<PassThruDisconnectDelegate>("PassThruDisconnect");
            _passThruReadMsgs = LoadFunc<PassThruReadMsgsDelegate>("PassThruReadMsgs");
            _passThruWriteMsgs = LoadFunc<PassThruWriteMsgsDelegate>("PassThruWriteMsgs");
            _passThruStartPeriodicMsg = LoadFunc<PassThruStartPeriodicMsgDelegate>("PassThruStartPeriodicMsg");
            _passThruStopPeriodicMsg = LoadFunc<PassThruStopPeriodicMsgDelegate>("PassThruStopPeriodicMsg");
            _passThruStartMsgFilter = LoadFunc<PassThruStartMsgFilterDelegate>("PassThruStartMsgFilter");
            _passThruStopMsgFilter = LoadFunc<PassThruStopMsgFilterDelegate>("PassThruStopMsgFilter");
            _passThruSetProgrammingVoltage = LoadFunc<PassThruSetProgrammingVoltageDelegate>("PassThruSetProgrammingVoltage");
            _passThruReadVersion = LoadFunc<PassThruReadVersionDelegate>("PassThruReadVersion");
            _passThruGetLastError = LoadFunc<PassThruGetLastErrorDelegate>("PassThruGetLastError");
            _passThruIoctl = LoadFunc<PassThruIoctlDelegate>("PassThruIoctl");
        }
        // 辅助方法：从DLL中加载指定函数并创建委托
        private T LoadFunc<T>(string name) where T : Delegate
        {
            var ptr = Native.GetProcAddress(_dllHandle, name);
            if (ptr == IntPtr.Zero)
                throw new MissingMethodException($"\"{name}\" not found in J2534 DLL");
            return (T)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }
        // ========== J2534 API包装方法 ==========

        // PassThruOpen: 打开J2534设备，获取设备句柄
        public int PassThruOpen(IntPtr pName, out uint deviceId)
        {
            return _passThruOpen(pName, out deviceId);
        }
        // PassThruClose: 关闭J2534设备，释放资源
        public int PassThruClose(uint deviceId)
        {
            return _passThruClose(deviceId);
        }
        // PassThruConnect: 在设备上建立通信通道
        public int PassThruConnect(uint deviceId, uint protocolId, uint flags, uint baudRate, out uint channelId)
        {
            return _passThruConnect(deviceId, protocolId, flags, baudRate, out channelId);
        }
        // PassThruDisconnect: 断开通信通道
        public int PassThruDisconnect(uint channelId)
        {
            return _passThruDisconnect(channelId);
        }
        // PassThruReadMsgs: 从通道读取消息（重要：处理非托管内存到托管数组的转换）
        public int PassThruReadMsgs(uint channelId, PASSTHRU_MSG[] pMsg, ref uint numMsgs, uint timeout)
        {
            // 计算PASSTHRU_MSG结构的大小（因平台而异：x86通常为76字节，x64可能更大）
            int PASSTHRU_MSG_SIZE = Marshal.SizeOf<PASSTHRU_MSG>();
            IntPtr rMsgs = IntPtr.Zero;
            try
            {
                // 分配非托管内存缓冲区，用于接收消息
                rMsgs = Marshal.AllocHGlobal(PASSTHRU_MSG_SIZE * pMsg.Length);
                for (int i =0;i< pMsg.Length; i++)
                {
                    Marshal.StructureToPtr(pMsg[i], rMsgs + i * PASSTHRU_MSG_SIZE, false);
                }
                // 调用原生API读取消息到非托管内存
                int ret = _passThruReadMsgs(channelId, rMsgs, ref numMsgs, timeout);
                // 将非托管内存中的数据复制回托管数组
                int count = (int)Math.Min(numMsgs, pMsg.Length);
                for(int i = 0; i < count; i++)
                {
                    // 将指针位置的数据转换为PASSTHRU_MSG结构
                    pMsg[i] = Marshal.PtrToStructure<PASSTHRU_MSG>(rMsgs + i * PASSTHRU_MSG_SIZE);
                }
                return ret;
            }
            finally
            {
                // 释放rMsgs内存
                if (rMsgs != IntPtr.Zero)
                    Marshal.FreeHGlobal(rMsgs);  // 确保释放
            }
        }
        // PassThruWriteMsgs: 向通道写入消息（托管数组到非托管内存的转换）
        public int PassThruWriteMsgs(uint channelId, PASSTHRU_MSG[] pMsg, ref uint numMsgs, uint timeout)
        {
            int PASSTHRU_MSG_SIZE = Marshal.SizeOf<PASSTHRU_MSG>();
            IntPtr wMsgs = IntPtr.Zero;
            try
            {
                wMsgs = Marshal.AllocHGlobal(PASSTHRU_MSG_SIZE * pMsg.Length);
                // 将托管数组复制到非托管内存
                for (int i=0;i< pMsg.Length;i++)
                {
                    Marshal.StructureToPtr(pMsg[i], wMsgs + i * PASSTHRU_MSG_SIZE, false);
                }
                return _passThruWriteMsgs(channelId, wMsgs, ref numMsgs, timeout);
            }
            finally
            {
                if (wMsgs != IntPtr.Zero)
                    Marshal.FreeHGlobal(wMsgs);  // 确保释放
            }
        }
        // PassThruStartPeriodicMsg: 开始周期性发送消息（用于心跳、保持激活等）
        public int PassThruStartPeriodicMsg(uint channelId, ref PASSTHRU_MSG msg, ref uint msgId, uint timeInterVal)
        {
            return _passThruStartPeriodicMsg(channelId, ref msg, ref msgId, timeInterVal);
        }
        // PassThruStopPeriodicMsg: 停止周期性消息发送
        public int PassThruStopPeriodicMsg(uint channelId, uint msgId)
        {
            return _passThruStopPeriodicMsg(channelId, msgId);
        }
        // PassThruStartMsgFilter: 启动消息过滤器（用于筛选特定CAN ID的消息）
        public int PassThruStartMsgFilter(uint channelId, uint filterType, ref PASSTHRU_MSG maskMsg, ref PASSTHRU_MSG patternMsg, ref PASSTHRU_MSG flowControlMsg, out uint filterId)
        {
            return _passThruStartMsgFilter(channelId, filterType, ref maskMsg, ref patternMsg, ref flowControlMsg, out filterId);
        }
        // PassThruStopMsgFilter: 停止消息过滤器
        public int PassThruStopMsgFilter(uint channelId, uint filterId)
        {
            return _passThruStopMsgFilter(channelId, filterId);
        }
        // PassThruSetProgrammingVoltage: 设置编程电压（用于ECU编程）
        public int PassThruSetProgrammingVoltage(uint deviceId, uint pinNumber, uint voltage)
        {
            return _passThruSetProgrammingVoltage(deviceId, pinNumber, voltage);
        }
        public int PassThruReadVersion(uint deviceId, StringBuilder firmwareVersion, StringBuilder dllVersion, StringBuilder apiVersion)
        {
            return _passThruReadVersion(deviceId, firmwareVersion, dllVersion, apiVersion);
        }
        public int PassThruGetLastError(StringBuilder errorDescription)
        {
            return _passThruGetLastError(errorDescription);
        }
        // PassThruIoctl: 输入/输出控制，用于各种设备控制操作
        public int PassThruIoctl(uint channelId, uint ioctlId, IntPtr input, IntPtr output)
        {
            return _passThruIoctl(channelId, ioctlId, input, output);
        }
        // IDisposable实现：释放DLL资源
        public void Dispose()
        {
            if (_dllHandle != IntPtr.Zero)
            {
                Native.FreeLibrary(_dllHandle); // 卸载DLL
            }
        }
    }
}
