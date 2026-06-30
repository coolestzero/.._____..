using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace EOLTest.Services.Impl
{
    /// <summary>
    /// MDB 数据库帮助类（专为 .mdb 格式 + x86 进程优化）
    /// 提供程序自动选择：Jet 4.0（原生） → ACE 12.0（备选）
    /// </summary>
    public class MdbHelper : IDataBaseService
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly string _activeProvider; // 记录当前使用的提供程序名称

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbFilePath">.mdb 数据库文件完整路径</param>
        /// <param name="password">数据库密码（无密码传空字符串）</param>
        /// <param name="logger">Serilog 日志对象（可选）</param>
        /// <exception cref="InvalidOperationException">当未找到任何可用提供程序时抛出</exception>
        public MdbHelper(string dbFilePath, string password = "", ILogger logger = null)
        {
            _logger = logger;
            _logger?.Debug("初始化数据库连接：路径={DbPath}", dbFilePath);

            try
            {
                // 1. 自动检测可用的 OLEDB 提供程序
                _activeProvider = DetectProvider();

                // 2. 根据选定的提供程序构建连接字符串
                _connectionString = BuildConnectionString(_activeProvider, dbFilePath, password);

                _logger?.Information("数据库初始化成功，提供程序：{Provider}", _activeProvider);
            }
            catch (Exception ex)
            {
                _logger?.Fatal(ex, "数据库初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 按优先级检测可用的提供程序
        /// 1st: Microsoft.Jet.OLEDB.4.0 (Windows 自带，32位，.mdb完美支持)
        /// 2nd: Microsoft.ACE.OLEDB.12.0 (需额外安装 Redistributable)
        /// </summary>
        private static string DetectProvider()
        {
            // 优先级顺序：Jet 4.0 → ACE 12.0
            var candidates = new (string provider, string description)[]
            {
                ("Microsoft.Jet.OLEDB.4.0",  "Jet 4.0 (Windows 原生)"),
                ("Microsoft.ACE.OLEDB.12.0", "ACE 12.0 (需安装 Redistributable)"),
            };

            // 顺序尝试，哪个能创建连接对象就返回哪个
            foreach (var (provider, desc) in candidates)
            {
                if (IsProviderAvailable(provider))
                {
                    return provider;
                }
            }

            // 两个都不可用：给出明确的安装指引
            var sb = new StringBuilder();
            sb.AppendLine("未找到任何可用的 Access 数据库提供程序。");
            sb.AppendLine("当前程序要求 32 位进程（x86）。");
            sb.AppendLine();
            sb.AppendLine("解决方案（按优先级）：");
            sb.AppendLine("1. Jet 4.0 是 Windows 自带驱动，通常无需额外操作。如果缺失，请修复系统文件：");
            sb.AppendLine("   以管理员权限运行 cmd 并执行：sfc /scannow");
            sb.AppendLine();
            sb.AppendLine("2. 安装 Microsoft Access Database Engine 2016 Redistributable（32位）：");
            sb.AppendLine("   下载地址：https://www.microsoft.com/en-us/download/details.aspx?id=54920");
            sb.AppendLine("   请选择 AccessDatabaseEngine.exe（32位）进行安装。");
            throw new InvalidOperationException(sb.ToString());
        }

        /// <summary>
        /// 测试某个 OLEDB 提供程序是否已在系统中注册
        /// 原理：尝试用该提供程序构造一个 OleDbConnection（不实际连接），成功即表示可用
        /// </summary>
        private static bool IsProviderAvailable(string provider)
        {
            try
            {
                // 用一个不存在的路径测试，不会触发文件访问异常
                using (var conn = new OleDbConnection($"Provider={provider};Data Source=;"))
                {
                    // 能创建成功就说明提供程序已注册
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根据提供程序构建 OLEDB 连接字符串
        /// </summary>
        private static string BuildConnectionString(string provider, string dbFilePath, string password)
        {
            var connStr = $"Provider={provider};Data Source={dbFilePath};";
            if (!string.IsNullOrEmpty(password))
            {
                // Jet 和 ACE 都使用同样的密码参数
                connStr += $"Jet OLEDB:Database Password={password};";
            }
            return connStr;
        }

        // ═══════════════════════════════════════
        //  公开的数据库操作方法（兼容原接口）
        // ═══════════════════════════════════════

        /// <summary>
        /// 执行查询并返回 DataTable
        /// </summary>
        public async Task<DataTable> QueryAsync(string sql)
        {
            _logger?.Debug("执行查询SQL: {Sql}", sql);

            return await Task.Run(() =>
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();

                        using (var adapter = new OleDbDataAdapter(sql, conn))
                        {
                            var dt = new DataTable();
                            adapter.Fill(dt);
                            _logger?.Information("查询成功，返回 {RowCount} 行", dt.Rows.Count);
                            return dt;
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    _logger?.Error(ex, "数据库查询失败(OleDb错误): {Sql}", sql);
                    throw new Exception($"数据库查询失败: {ex.Message}", ex);
                }
                catch (InvalidOperationException ex)
                {
                    _logger?.Error(ex, "数据库连接失败(配置错误): {Sql}", sql);
                    throw new Exception($"数据库连接失败，请检查提供程序: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "查询发生未知错误: {Sql}", sql);
                    throw new Exception($"查询失败: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 执行增删改命令，返回受影响行数
        /// </summary>
        public async Task<int> ExecuteAsync(string sql)
        {
            _logger?.Debug("执行命令SQL: {Sql}", sql);

            return await Task.Run(() =>
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            int affectedRows = cmd.ExecuteNonQuery();
                            _logger?.Information("命令执行成功，影响 {RowCount} 行", affectedRows);
                            return affectedRows;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "命令执行失败: {Sql}", sql);
                    throw new Exception($"操作失败: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 执行标量查询（如 COUNT、MAX），返回单个值
        /// </summary>
        public async Task<object> ExecuteScalarAsync(string sql)
        {
            _logger?.Debug("执行标量查询: {Sql}", sql);

            return await Task.Run(() =>
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            object result = cmd.ExecuteScalar();
                            _logger?.Information("标量查询成功，结果: {Result}", result ?? "NULL");
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "标量查询失败: {Sql}", sql);
                    throw new Exception($"查询失败: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 参数化查询，返回 DataTable
        /// </summary>
        public async Task<DataTable> QueryWithParamsAsync(string sql, Dictionary<string, object> parameters)
        {
            _logger?.Debug("执行参数化查询: {Sql}, 参数数量: {ParamCount}", sql, parameters?.Count ?? 0);

            return await Task.Run(() =>
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            AddParameters(cmd, parameters);
                            using (var adapter = new OleDbDataAdapter(cmd))
                            {
                                var dt = new DataTable();
                                adapter.Fill(dt);
                                _logger?.Information("参数化查询成功，返回 {RowCount} 行", dt.Rows.Count);
                                return dt;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "参数化查询失败: {Sql}", sql);
                    throw new Exception($"查询失败: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 参数化执行命令，返回受影响行数
        /// </summary>
        public async Task<int> ExecuteWithParamsAsync(string sql, Dictionary<string, object> parameters)
        {
            _logger?.Debug("执行参数化命令: {Sql}", sql);

            return await Task.Run(() =>
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            AddParameters(cmd, parameters);
                            int affectedRows = cmd.ExecuteNonQuery();
                            _logger?.Information("参数化命令成功，影响 {RowCount} 行", affectedRows);
                            return affectedRows;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "参数化命令执行失败: {Sql}", sql);
                    throw new Exception($"操作失败: {ex.Message}", ex);
                }
            });
        }

        // ══════════════════════════════
        //  内部辅助方法
        // ══════════════════════════════

        /// <summary>
        /// 为 OleDbCommand 批量添加参数
        /// </summary>
        private void AddParameters(OleDbCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters == null) return;

            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }
    }
}
