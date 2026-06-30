// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOLTest.API;
using EOLTest.Models;

namespace EOLTest.Services.Function
{
    public class VinReadWriteFunc : IFunction
    {
        private IVciControl _vci;
        private ILogShowService _logshow;
        public VinReadWriteFunc(IVciControl vci, ILogShowService logshow)
        {
            _vci = vci;
            _logshow = logshow;
        }
        public string FunctionName => "VIN";

        public async Task<bool> ExecuteFunc()
        {
            return await VinRead();
        }

        private async Task<bool> VinRead()
        {
            await _vci.InitVciAsync();
            ApiResult result = await _vci.ConnectEcuAsync("761", "769", "0");
            _logshow.AddLog("INFO", "连接成功");
            result = result.Success ? await _vci.SendCanPhyAsync("22F18A") : result;
            result = result.Success ? await _vci.ReceiveAsync() : result;

            return result.Success;
        }
    }
}
