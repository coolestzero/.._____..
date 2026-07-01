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

        public IDiagnosticService diagnostic { get; }

        public VehicleLoggerFactory loggerFactory { get; }
        // 全局日志（用于系统级日志）
        public  ILogger sysLogger { get; }
        // 当前车辆的日志（开始检测时创建，检测完关闭） 车辆日志由外部赋值，构造函数不注入
        public ILogger? vehicleLogger { get; set; }

        public ILogShowService logshow { get; set; }
        public DataAggregator(
            ICarDataService _carService,
            IDiagnosticService _diagnostic,
            VehicleLoggerFactory _loggerFactory,
            ILogger _sysLogger,
            ILogShowService _logshow)
        {
            carService = _carService;
            diagnostic = _diagnostic;
            loggerFactory = _loggerFactory;
            sysLogger = _sysLogger;
            logshow = _logshow;
        }
    }
}
