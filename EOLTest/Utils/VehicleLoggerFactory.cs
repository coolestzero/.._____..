// Copyright (c) TBD 2026.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace EOLTest.Utils
{
    /// <summary>
    /// 车辆日志工厂：为每台车创建独立的日志文件
    /// </summary>
    public class VehicleLoggerFactory
    {
        private readonly string _vehicleLogFolder;

        public VehicleLoggerFactory()
        {
            // 车辆日志存放目录
            _vehicleLogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Vehicles");

            // 确保目录存在
            if (!Directory.Exists(_vehicleLogFolder))
            {
                Directory.CreateDirectory(_vehicleLogFolder);
            }
        }

        /// <summary>
        /// 为指定VIN的车辆创建日志实例
        /// </summary>
        /// <param name="vin">车辆VIN码</param>
        /// <returns>该车辆的专属日志记录器</returns>
        public ILogger CreateVehicleLogger(string vin)
        {
            // 生成日志文件名：VIN_年月日_时分秒.log
            // 例如：LSVAA4180E2123456_20260511_143025.log
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logFileName = $"{vin}_{timestamp}.log";
            string logFilePath = Path.Combine(_vehicleLogFolder, logFileName);

            // 创建Serilog日志实例
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logFilePath,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            // 记录到全局日志
            App.GlobalLogger.Information("为车辆 {VIN} 创建日志文件: {LogFile}", vin, logFilePath);

            return logger;
        }

        /// <summary>
        /// 关闭车辆日志（释放文件锁）
        /// </summary>
        public void CloseLogger(ILogger logger)
        {
            if (logger != null)
            {
                (logger as IDisposable)?.Dispose();
                App.GlobalLogger.Information("车辆日志已关闭");
            }
        }
    }
}
