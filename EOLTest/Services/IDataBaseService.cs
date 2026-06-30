
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Services
{
    /// <summary>
    /// 数据访问服务接口 
    /// </summary>
    public interface IDataBaseService
    {
        /// <summary>
        /// 查询数据
        /// </summary>
        Task<DataTable> QueryAsync(string sql);

        /// <summary>
        /// 执行增删改操作
        /// </summary>
        Task<int> ExecuteAsync(string sql);

        /// <summary>
        /// 查询单个值（如COUNT、MAX等）
        /// </summary>
        Task<object> ExecuteScalarAsync(string sql);

        // 新增：参数化查询方法（使用字典传参）
        Task<DataTable> QueryWithParamsAsync(string sql, Dictionary<string, object> parameters);
        Task<int> ExecuteWithParamsAsync(string sql, Dictionary<string, object> parameters);
    }
}
