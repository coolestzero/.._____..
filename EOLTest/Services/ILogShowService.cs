// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Services
{
    /// <summary>
    /// 日志显示服务接口 —— 所有模块通过这个接口写日志
    /// </summary>
    public interface ILogShowService
    {
        /// <summary>添加一条日志</summary>
        void AddLog(string level, string message);
        /// <summary>清空日志</summary>
        void Clear();
    }
}
