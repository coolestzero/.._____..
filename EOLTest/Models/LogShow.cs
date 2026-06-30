// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Models
{
    /// <summary>
    /// 在界面显示日志
    /// </summary>
    public class LogShow
    {
        /// <summary>日志序号，从1开始递增</summary>
        public int Index { get; set; }
        /// <summary>时间，格式 HH:mm:ss</summary>
        public string Time { get; set; } = string.Empty;
        /// <summary>级别：INFO / WARN / ERROR / SEND / RECV</summary>
        public string Level { get; set; } = "INFO";
        /// <summary>日志正文</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>用于控制前景色（在 XAML 里绑定）</summary>
        public string ForegroundColor
        {
            get
            {
                return Level switch
                {
                    "ERROR" => "#E74C3C",   // 红色
                    "WARN" => "#E67E22",   // 橙色
                    "SEND" => "#3498DB",   // 蓝色（发送）
                    "RECV" => "#27AE60",   // 绿色（接收）
                    _ => "#BDC3C7"    // 默认灰色
                };
            }
        }
    }
}
