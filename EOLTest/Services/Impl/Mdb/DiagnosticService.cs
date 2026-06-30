using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOLTest.Models;
using EOLTest.Services.Mdb;
using Serilog;

namespace EOLTest.Services.Impl.Mdb
{
    public class DiagnosticService : IDiagnosticService
    {
        private readonly IDataBaseService _db;  // 依赖接口，不依赖具体实现
        private readonly ILogger _logger;  // Serilog的日志对象
        // 构造函数注入IDatabaseService接口
        public DiagnosticService(IDataBaseService db, ILogger logger = null)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<EcuCanId> GetEcuCanIdAsync(string ecuName)
        {
            try
            {
                var canId = new EcuCanId();
                var data = await QueryTableAsync("CANID", $"modname = '{ecuName}'");
                if (data.Rows.Count == 0) return null;

                DataRow row = data.Rows[0];
                canId.EcuName = row["modname"].ToString();
                canId.TxId = row["tx_address"].ToString();
                canId.RxId = row["rx_address"].ToString();
                canId.LinId = row["LinID"].ToString();
                canId.DoipId = row["Doip"].ToString();
                _logger?.Debug($"CANID查询成功：{ecuName}-{canId.TxId}-{canId.RxId}");
                return canId;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "CANID查询失败");
                throw;
            }
            
        }

        //✅ 新方式：参数化查询（OleDB用? 占位符）
        //public Task<EcuCanId> GetEcuCanIdAsync(string ecuName)
        //{
        //    string sql = "SELECT * FROM VehicleInfo WHERE VIN = ?";
        //    var parameters = new Dictionary<string, object>
        //{
        //    { "@VIN", vin }
        //};

        //    var dt = await _db.QueryWithParamsAsync(sql, parameters);
        //    // 自动处理特殊字符，不需要转义
        //}


        /// <summary>
        /// 通用的查询方法
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="whereCondition">WHERE条件（不含WHERE关键字，传null查全部）</param>
        /// <param name="orderBy">排序字段（不含ORDER BY关键字，传null不排序）</param>
        /// <returns>DataTable结果</returns>
        public async Task<DataTable> QueryTableAsync(string tableName, string whereCondition = null, string orderBy = null)
        {
            try
            {
                string sql = $"SELECT * FROM {tableName}";

                if (!string.IsNullOrEmpty(whereCondition))
                {
                    sql += $" WHERE {whereCondition}";
                }

                if (!string.IsNullOrEmpty(orderBy))
                {
                    sql += $" ORDER BY {orderBy}";
                }

                _logger?.Debug("通用查询：{Sql}", sql);
                return await _db.QueryAsync(sql);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "通用查询失败：表={TableName}", tableName);
                throw new Exception($"查询表 {tableName} 失败: {ex.Message}", ex);
            }
        }
    }
}
