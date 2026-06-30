// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOLTest.Services.Mdb;
using EOLTest.Utils;
using Serilog;

namespace EOLTest.Services.Aggregators
{
    /// <summary>
    /// 数据相关的服务聚合（数据库 + 配置 + 数据导出）
    /// </summary>
    public class DataAggregator
    {
        public ICarDataService carService { get; }  // 依赖接口

        public VehicleLoggerFactory loggerFactory { get; }
        // 全局日志（用于系统级日志）
        public  ILogger sysLogger { get; }
        // 当前车辆的日志（开始检测时创建，检测完关闭）
        public ILogger vehicleLogger { get; set; }

        public ILogShowService logshow { get; set; }
        public DataAggregator(
            ICarDataService _carService,
            VehicleLoggerFactory _loggerFactory,
            ILogger _sysLogger,
            ILogger _vehicleLogger,
            ILogShowService _logshow)
        {
            carService = _carService;
            loggerFactory = _loggerFactory;
            sysLogger = _sysLogger;
            vehicleLogger = _vehicleLogger;
            logshow = _logshow;
        }
    }
}
