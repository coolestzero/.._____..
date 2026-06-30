using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Services.Impl
{
    /// <summary>
    /// 数据库操作例子
    /// </summary>
    public class UseMdbServices: IUseMdbService
    {
        private readonly IDataBaseService _db;  // 依赖接口，不依赖具体实现

        // 构造函数注入IDatabaseService接口
        public UseMdbServices(IDataBaseService db)
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

        // 异步获取所有用户
        //public async Task<List<UserModel>> GetAllUsersAsync()
        //{
        //    string sql = "SELECT * FROM UserInfo ORDER BY ID";
        //    DataTable dt = await _db.QueryAsync(sql);

        //    var users = new List<UserModel>();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        users.Add(new UserModel
        //        {
        //            Id = Convert.ToInt32(row["ID"]),
        //            UserName = row["UserName"].ToString(),
        //            Password = row["Password"].ToString(),
        //            Level = Convert.ToInt32(row["Level"]),
        //            CreateTime = Convert.ToDateTime(row["CreateTime"])
        //        });
        //    }
        //    return users;
        //}

        //// 异步根据用户名查询
        //public async Task<UserModel> GetUserByNameAsync(string userName)
        //{
        //    // 注意：生产环境应该用参数化查询，这里为了简单使用字符串拼接
        //    string sql = $"SELECT * FROM UserInfo WHERE UserName='{userName.Replace("'", "''")}'";
        //    DataTable dt = await _db.QueryAsync(sql);

        //    if (dt.Rows.Count == 0) return null;

        //    DataRow row = dt.Rows[0];
        //    return new UserModel
        //    {
        //        Id = Convert.ToInt32(row["ID"]),
        //        UserName = row["UserName"].ToString(),
        //        Password = row["Password"].ToString(),
        //        Level = Convert.ToInt32(row["Level"]),
        //        CreateTime = Convert.ToDateTime(row["CreateTime"])
        //    };
        //}

        //// 异步添加用户
        //public async Task<bool> AddUserAsync(UserModel user)
        //{
        //    string sql = $"INSERT INTO UserInfo (UserName, Password, Level, CreateTime) " +
        //                $"VALUES ('{user.UserName.Replace("'", "''")}', " +
        //                $"'{user.Password.Replace("'", "''")}', " +
        //                $"{user.Level}, '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')";

        //    int result = await _db.ExecuteAsync(sql);
        //    return result > 0;
        //}

        //// 异步更新用户
        //public async Task<bool> UpdateUserAsync(UserModel user)
        //{
        //    string sql = $"UPDATE UserInfo SET " +
        //                $"UserName='{user.UserName.Replace("'", "''")}', " +
        //                $"Password='{user.Password.Replace("'", "''")}', " +
        //                $"Level={user.Level} " +
        //                $"WHERE ID={user.Id}";

        //    int result = await _db.ExecuteAsync(sql);
        //    return result > 0;
        //}

        // ✅ 新方式：参数化查询（OleDB用?占位符）
        //public async Task<VehicleInfo> GetVehicleByVinAsync(string vin)
        //{
        //    string sql = "SELECT * FROM VehicleInfo WHERE VIN = ?";
        //    var parameters = new Dictionary<string, object>
        //{
        //    { "@VIN", vin }
        //};

        //    var dt = await _db.QueryWithParamsAsync(sql, parameters);
        //    // 自动处理特殊字符，不需要转义
        //}

        //// ✅ 多参数查询
        //public async Task<List<VehicleInfo>> GetVehiclesByDateRangeAsync(DateTime start, DateTime end)
        //{
        //    string sql = "SELECT * FROM VehicleInfo WHERE ProduceDate >= ? AND ProduceDate <= ?";
        //    var parameters = new Dictionary<string, object>
        //{
        //    { "@StartDate", start },
        //    { "@EndDate", end }
        //};

        //    var dt = await _db.QueryWithParamsAsync(sql, parameters);
        //    // 日期自动转为正确格式，不用担心
        //}

        //// ✅ 插入操作
        //public async Task<bool> AddVehicleAsync(VehicleInfo vehicle)
        //{
        //    string sql = @"INSERT INTO VehicleInfo (VIN, VSN, CxName, EngineName, ProduceDate, Color) 
        //               VALUES (?, ?, ?, ?, ?, ?)";

        //    var parameters = new Dictionary<string, object>
        //{
        //    { "@VIN", vehicle.Vin },
        //    { "@VSN", vehicle.Vsn },
        //    { "@CxName", vehicle.CxName },
        //    { "@EngineName", vehicle.EngineName },
        //    { "@ProduceDate", vehicle.ProduceDate },
        //    { "@Color", vehicle.Color }
        //};

        //    int result = await _db.ExecuteWithParamsAsync(sql, parameters);
        //    return result > 0;
        //}

        //// ✅ 更新操作（带数字和布尔值）
        //public async Task<bool> UpdateEngineAsync(EngineInfo engine)
        //{
        //    string sql = "UPDATE EngineInfo SET EngineType = ?, Displacement = ? WHERE EngineNumber = ?";

        //    var parameters = new Dictionary<string, object>
        //{
        //    { "@Type", engine.EngineType },
        //    { "@Displacement", engine.Displacement },  // 数字类型自动处理
        //    { "@Number", engine.EngineNumber }
        //};

        //    int result = await _db.ExecuteWithParamsAsync(sql, parameters);
        //    return result > 0;
        //}

    }
}
