// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Services.Function
{
    public class ImmoFunction : IFunction
    {
        public string FunctionName => "IMMO";

        public Task<bool> ExecuteFunc() => throw new NotImplementedException();

        private string Select_KeyType() { /* 具体实现 */ return ""; }
    }
}
