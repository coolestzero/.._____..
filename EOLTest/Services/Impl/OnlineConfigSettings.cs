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
            ["LD"] = ("10.18.243.14", 8080),
            ["LD2"] = ("10.18.243.14", 8080),
            ["FW"] = ("hxzxpz.sgmw.com.cn", 8080),
            ["FE"] = ("hxzxpz.sgmw.com.cn", 8080),
            ["JAK1"] = ("10.140.16.6", 8080),
            ["QD"] = ("10.42.243.24", 8080),
            ["CQ"] = ("10.66.243.15", 8080)
        };
        public (string ip, int port) GetServerAddress()
        {
            if (FactoryServers.TryGetValue(Factory ?? "", out var addr))
                return addr;
            // 默认回退地址
            return ("10.18.243.14", 8080);
        }
    }
}
