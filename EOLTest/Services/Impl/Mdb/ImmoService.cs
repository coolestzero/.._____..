using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOLTest.Services.Mdb;

namespace EOLTest.Services.Impl.Mdb
{
    public class ImmoService:IImmoService
    {
        private readonly IDataBaseService _db;  // 依赖接口，不依赖具体实现

        // 构造函数注入IDatabaseService接口
        public ImmoService(IDataBaseService db)
        {
            _db = db;
        }
    }
}
