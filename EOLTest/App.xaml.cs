using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using EOLTest.API;
using EOLTest.Services;
using EOLTest.Services.Aggregators;
using EOLTest.Services.Function;
using EOLTest.Services.Impl;
using EOLTest.Services.Impl.Mdb;
using EOLTest.Services.Mdb;
using EOLTest.Utils;
using EOLTest.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;

namespace EOLTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            #region 全局异常处理
            // 捕获 UI 线程未处理异常（弹窗并记录，不退出）
            this.DispatcherUnhandledException += (s, e) =>
            {
                GlobalLogger?.Fatal(e.Exception, "UI线程未处理异常");
                MessageBox.Show($"发生未处理异常：\n{e.Exception.Message}", "系统错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // 阻止程序退出（调试用，正式可设为 false）
            };
            // 捕获非 UI 线程未处理异常
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                GlobalLogger?.Fatal(ex, "非UI线程未处理异常");
            };
            // 捕获 Task 未观察异常
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                GlobalLogger?.Fatal(e.Exception, "未观察任务异常");
                e.SetObserved();
            };
            #endregion
            Services = ConfigureServices();
        }
        public new static App Current => (App)Application.Current;

        public IServiceProvider Services { get; }

        // 全局日志实例（静态，方便任何地方使用）
        public static ILogger GlobalLogger { get; private set; }
        private static IServiceProvider ConfigureServices()
        {

            var services = new ServiceCollection();
            //注册实例
            #region 注册日志实例
            // Serilog有6个日志级别，从低到高：
            // Verbose(最详细) < Debug(调试) < Information(信息) < Warning(警告) < Error(错误) < Fatal(致命)
            var logger = new LoggerConfiguration().MinimumLevel
                .Debug()  // 只记录Debug及以上级别的日志（不记录Verbose）
                .WriteTo.File("Log/System/log-.txt",
                rollingInterval: RollingInterval.Day,           // 每天生成新文件
                retainedFileCountLimit: 30, // 保留最近30个文件（可选）
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                // 输出格式：2026-05-07 14:30:25.123 [INF] 查询成功，返回 10 行数据
                )
                .CreateLogger();
            GlobalLogger = logger;
            services.AddSingleton<ILogger>(logger);

            // 注册车辆日志工厂 VehicleLoggerFactory 负责为每台车创建独立的日志文件
            services.AddSingleton<VehicleLoggerFactory>();
            // 注册日志显示服务 注册为单例，整个应用程序生命周期内只有一个实例 所有模块通过 ILogShowService 接口写入日志
            services.AddSingleton<ILogShowService, LogShowService>();
            #endregion

            GlobalLogger.Information("========== 软件启动 ==========");

            #region 注册数据库服务
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MDB"); //存放的文件夹

            // 4个数据库用同一个MdbHelper，但不同实例
            var carDb = new MdbHelper(Path.Combine(basePath, "车辆信息数据库(database).mdb"), "111", logger);
            var diagnosticDb = new MdbHelper(Path.Combine(basePath, "DiagnosticData.mdb"), "37gm50", logger);
            var immoDb = new MdbHelper(Path.Combine(basePath, "IMMODataBase.mdb"), "9527", logger);

            // 注册接口映射
            services.AddSingleton<ICarDataService>(new CarDataService(carDb));
            services.AddSingleton<IDiagnosticService>(new DiagnosticService(diagnosticDb));
            services.AddSingleton<IImmoService>(new ImmoService(immoDb));
            #endregion

            #region 注册功能服务
            // 所有功能类型集中放在一个列表里
            Type[] allFunctionTypes = new Type[]
            {
            typeof(ImmoFunction),
            typeof(VinReadWriteFunc),
            typeof(DtcFunction),
                // 新增功能在这里加一行即可
            };

            // 循环注册
            foreach (Type type in allFunctionTypes)
            {
                services.AddSingleton(typeof(IFunction), type);
            }

            services.AddSingleton<DataAggregator>();
            #endregion

            #region Http TCP  相关
            // ── 在线配置相关服务 ──
            var onlineSettings = new OnlineConfigSettings
            {
                Factory = "FW"   // TODO: 从 appsettings.json 或数据库读取
            };
            services.AddSingleton(onlineSettings);

            // HttpService 
            services.AddSingleton<IHttpService, HttpService>();
            // OnlineConfigService 
            services.AddSingleton<OnlineConfigService>();
            #endregion


            //注册VCI实例
            services.AddSingleton<APITester>();
            //services.AddSingleton<IVciControl, VciControlOuKe>();
            services.AddSingleton<VciControlOuKe>();   // 注册具体类（以供装饰器使用）
            // 2. 注册装饰器作为 IVciControl 的实例
            services.AddSingleton<IVciControl>(sp =>
            {
                // 手动创建原始实现，并注入 DataAggregator 和原始 VciControlOuKe
                var inner = sp.GetRequiredService<VciControlOuKe>();
                var dataAggregator = sp.GetRequiredService<DataAggregator>();
                return new LoggedVciControl(inner, dataAggregator);
            });

            services.AddSingleton<IControlUiService, ControlUiService>();
            services.AddSingleton<MainWindowViewModel>();
            // 注册MainWindow（不注入其他服务，避免循环依赖）
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 先获取所有服务
            var mainWindow = Services.GetRequiredService<MainWindow>();
            var viewModel = Services.GetRequiredService<MainWindowViewModel>();
            var controlService = Services.GetRequiredService<IControlUiService>();
            var logShowService = Services.GetRequiredService<ILogShowService>();
            // 手动设置DataContext
            mainWindow.DataContext = viewModel;

            // 记录系统启动日志 程序版本是界面的日期版本
            GlobalLogger.Information("========== 界面启动 ==========");
            GlobalLogger.Information($"程序版本: {mainWindow.Title.Substring(mainWindow.Title.Length-8)}");


            // 如果是ControlUiService，设置MainWindow引用
            if (controlService is ControlUiService concreteService)
            {
                concreteService.SetMainWindow(mainWindow);
            }
            // mainWindow.LogRichTextBox 是 x:Name 对应的字段
            if (logShowService is LogShowService concreteLogService)
            {
                concreteLogService.SetLogRichTextBox(mainWindow.LogRichTextBox);
            }
            // 显示窗口
            mainWindow.Show();
            // 窗口显示后再执行异步初始化（设备连接等）
            viewModel.VciOpenAsync();
        }

        
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // 记录系统关闭日志
            GlobalLogger.Information("========== 系统关闭 ==========");
            var viewModel = Services.GetRequiredService<MainWindowViewModel>();

            // 同步等待关闭（退出时不涉及 UI 交互，GetAwaiter().GetResult() 可接受）
            viewModel.VciCloseAsync().GetAwaiter().GetResult();
            // 关闭所有日志（释放文件锁）
            Log.CloseAndFlush();
        }


    }

}
