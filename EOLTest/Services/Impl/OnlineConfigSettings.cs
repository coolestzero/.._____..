// Copyright (c) TBD 2026.

// 用途：存放在线配置服务器地址映射，替代原 VB6 全局变量 factory
using System.Collections.Generic;

namespace EOLTest.Services.Impl
{
    public class OnlineConfigSettings
    {
        /// <summary>当前工厂标识（例如 "LD", "FW", "JAK1"）</summary>
        public string Factory { get; set; }
        /// <summary>工厂名称 -> (IP/域名, 端口) 映射表</summary>
        public Dictionary<string, (string ip, int port)> FactoryServers { get; set; } = new()
        {
            ["FW"] = ("127.0.0.1", 8080),
        };
        public (string ip, int port) GetServerAddress()
        {
            if (FactoryServers.TryGetValue(Factory ?? "", out var addr))
                return addr;
            // 默认回退地址
            return ("127.0.0.1", 8080);
        }
    }
}
