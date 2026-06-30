// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Services
{
    /// <summary>
    /// 功能接口，每个功能都要实现这个接口
    /// </summary>
    public interface IFunction
    {
        public string FunctionName { get; }        // 对应原来的 Immno、ECURead 等
        public Task<bool> ExecuteFunc();                    // 返回 ResultFlag
    }
}
