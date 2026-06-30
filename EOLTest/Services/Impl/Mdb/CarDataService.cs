using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOLTest.Services.Mdb;

namespace EOLTest.Services.Impl.Mdb
{
    public class CarDataService : ICarDataService
    {
        private readonly IDataBaseService _db;  // 依赖接口，不依赖具体实现

        // 构造函数注入IDatabaseService接口
        public CarDataService(IDataBaseService db)
        {
            _db = db;
        }
        public async Task<string> GetCarNameAsync(string pzdm)
        {
            // 注意：生产环境应该用参数化查询，这里为了简单使用字符串拼接
            string sql = $"SELECT * FROM CAR_INFORMATION WHERE CAR_CODE='{pzdm.Replace("'", "''")}'";
            DataTable dt = await _db.QueryAsync(sql);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return row["CAR_TYPE"].ToString();
        }
    }
}
