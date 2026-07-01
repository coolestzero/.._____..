using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EOLTest.API;
using EOLTest.Models;
using EOLTest.Services;
using EOLTest.Services.Aggregators;
using EOLTest.Services.Function;
using EOLTest.Services.Impl;
using EOLTest.Services.Mdb;
using EOLTest.Utils;
using Microsoft.Win32;
using Serilog;
using Serilog.Core;

namespace EOLTest.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly DataAggregator _data;        // 数据相关
        private readonly IControlUiService _controlUiService;
        //VCI通讯控制 封装了J2534 DLL的调用
        private IVciControl _api;

        private string? _dllPath;

        [ObservableProperty]
        private bool _isVciConnected;   // true=已连接, false=未连接/失败
        [ObservableProperty]
        private string _connectionStatusText = "正在初始化...";  // 状态栏文字

        // 所有可用的功能（字典，用于快速查找）
        private readonly Dictionary<string, IFunction> _allFunctionMap;

        // 所有功能的名称列表
        public ObservableCollection<string> AllFunctionNames { get; } = new();

        // 当前工位要执行的功能名称
        public ObservableCollection<string> SelectedFunctionNames { get; } = new ObservableCollection<string>();

        [ObservableProperty]
        private Vehicle _currentVehicle = new Vehicle();

        // ── DTC 相关绑定属性 ──
        /// <summary>
        /// DTC 检测结果集合（每个元素对应一个 ECU）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<EcuDtcResult> _dtcResultList = new ObservableCollection<EcuDtcResult>();
        /// <summary>
        /// DTC 汇总文字，如 "✅ DTC 检测完成，当前故障 3 个"
        /// </summary>
        [ObservableProperty]
        private string _dtcSummaryText = "等待 DTC 检测...";
        /// <summary>
        /// 是否正在读取 DTC（可控制进度条显示）
        /// </summary>
        [ObservableProperty]
        private bool _isReadingDtc;
        /// <summary>
        /// 当前选中的 ECU 结果（ListView 选中项）
        /// </summary>
        [ObservableProperty]
        private EcuDtcResult _selectedEcuResult;
        /// <summary>
        /// 当前选中的 Tab 页索引（0=运行日志, 1=数据流监控, 2=DTC故障码）
        /// 绑定到 TabControl.SelectedIndex，实现程序控制 Tab 切换
        /// </summary>
        [ObservableProperty]
        private int _selectedTabIndex = 0;   // 默认显示"运行日志"

        public MainWindowViewModel(
            IVciControl api,
            IControlUiService controlUiService,
            DataAggregator dataService,
            IEnumerable<IFunction> allFunctions)  //容器注入的所有功能实现类实例的集合
        {
            _data = dataService;
            _controlUiService = controlUiService;
            _api = api;
            _data.sysLogger.Information("加载 MainWindowViewModel");
            // 构造函数 尝试从Windows注册表自动加载J2534 DLL路径
            RegistryKey Key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\PassThruSupport.04.04\Eucleia Intelligent Tech Inc. - wiScan T6");
            if (Key != null)
            {
                object value = Key.GetValue(@"FunctionLibrary");
                if (value != null)
                {
                    _dllPath = value.ToString(); // 从注册表获取DLL路径
                }
            }
            // 把所有功能存到字典里
            _allFunctionMap = new Dictionary<string, IFunction>();
            foreach (IFunction func in allFunctions)
            {
                _allFunctionMap[func.FunctionName] = func;
                AllFunctionNames.Add(func.FunctionName);
            }

            //测试用
            var ecuList = new List<EcuModule>
            {
                new EcuModule { EcuName = "VCU" },
                new EcuModule { EcuName = "VCU" },
                new EcuModule { EcuName = "ESP" },
                new EcuModule { EcuName = "MCU" },
                new EcuModule { EcuName = "BMS" },
            };
            var testVehicle = new Vehicle
            {
                Vin = "LK6ADBE45TF321400",
                Vsn = "PU70000000",
                CxName = "测试车型",
                EcuModules = new ObservableCollection<EcuModule>(ecuList) // ← 显式转换
            };
            CurrentVehicle = testVehicle;

            CheckVinIsTrueCommand.Execute(null);
            CheckVsnIsTrueCommand.Execute(null);
        }

        #region 打开关闭VCI

        public async Task VciOpenAsync()
        {
            try
            {
                // ---- 第一步：检查 DLL 路径是否已从注册表读取 ----
                if (string.IsNullOrEmpty(_dllPath))
                {
                    IsVciConnected = false;
                    ConnectionStatusText = "未找到驱动";
                    _data.sysLogger.Error("未在注册表中找到 J2534 DLL 路径");
                    _data.logshow.AddLog("FAIL", "❌ 未找到设备驱动，请检查注册表");
                    // 弹窗提示（不关闭软件）
                    _controlUiService.ShowMessageBox(
                        "设备初始化失败",
                        "未在注册表中找到 J2534 DLL 路径。\n\n" +
                        "可能原因：\n" +
                        "  1. 设备驱动未安装\n" +
                        "  2. 注册表键值缺失\n\n" +
                        "软件将继续运行，请在排查问题后重新连接。");
                    return;
                }
                // ---- 第二步：开始连接 ----
                ConnectionStatusText = "正在连接设备...";
                _data.logshow.AddLog("INFO", "▶ 正在连接设备板卡...");
                ApiResult apiResult = await _api.OpenVciAsync(_dllPath);
                IsVciConnected = apiResult.Success;
                if (IsVciConnected)
                {
                    // 成功分支
                    ConnectionStatusText = "设备已连接";
                    _data.sysLogger.Information("打开 VCI 成功");
                    _data.logshow.AddLog("PASS", "✅ 设备板卡连接成功");
                }
                else
                {
                    // 失败分支 —— 双重提示：状态栏 + 弹窗
                    ConnectionStatusText = "设备连接失败";
                    _data.sysLogger.Error("打开 VCI 失败，返回结果: {@Result}", apiResult);
                    _data.logshow.AddLog("FAIL", "❌ 连接设备板卡 执行失败");
                    // ★ 弹窗提示，不关闭软件
                    _controlUiService.ShowMessageBox(
                        "设备连接失败",
                        "无法连接到设备板卡。\n\n" +
                        "请检查：\n" +
                        "  1. 设备是否已通电\n" +
                        "  2. USB 线缆是否连接牢固\n" +
                        "  3. 驱动程序是否正常工作\n" +
                        "  4. 设备是否被其他程序占用\n\n" +
                        "软件将继续运行，排查问题后可重新连接。");
                }
            }
            catch (Exception ex)
            {
                // 异常分支
                IsVciConnected = false;
                ConnectionStatusText = "设备连接异常";
                _data.sysLogger.Error(ex, "打开 VCI 异常");
                _data.logshow.AddLog("ERROR", $"❌ 连接设备异常：{ex.Message}");
                _controlUiService.ShowMessageBox(
                    "设备连接异常",
                    $"连接设备时发生意外异常：\n\n{ex.Message}\n\n" +
                    "软件将继续运行，请排查问题后重试。");
            }
        }
        public async Task VciCloseAsync()
        {
            try
            {
                var apiResult = await _api.CloseVciAsync();
                IsVciConnected = false;
                ConnectionStatusText = "设备未连接";
                _data.sysLogger.Information($"关闭 VCI {(apiResult.Success ? "成功" : "失败")}");
            }
            catch (Exception ex)
            {
                _data.sysLogger.Error(ex, "关闭 VCI 异常");
            }
        }
        #endregion

        #region 校验VIN
        /// <summary>
        /// 校验VIN内容
        /// </summary>
        [RelayCommand]
        private void CheckVinIsTrue()
        {
            int maxLength = 17;
            try
            {
                string vin = _currentVehicle.Vin?.ToUpper() ?? "";

                // 如果为空，直接返回
                if (string.IsNullOrEmpty(vin))
                {
                    _controlUiService.FocusVinTextBox();
                    return;
                }

                // 校验逻辑
                bool isValid = true;

                switch (vin.Length)
                {
                    case 1:
                        if (!vin.Equals("L") && !vin.Equals("M") && !vin.Equals("9"))
                        {
                            isValid = false;
                        }
                        break;
                    case 2:
                        if (!vin.Equals("LZ") && !vin.Equals("MK") && !vin.Equals("LK") && !vin.Equals("90"))
                        {
                            isValid = false;
                        }
                        break;
                    case 3:
                        if (!vin.Equals("LZW") && !vin.Equals("LK6") && !vin.Equals("MK3") && !vin.Equals("906"))
                        {
                            isValid = false;
                        }
                        break;
                    case 17:
                        if (vin.Substring(0, 3).Equals("LZW") ||
                            vin.Substring(0, 3).Equals("LK6") ||
                            vin.Substring(0, 3).Equals("MK3") ||
                            vin.Substring(0, 3).Equals("906"))
                        {
                            VINCheck check = new VINCheck();
                            bool flag = check.VIN_Check_GB(vin);
                            if (flag)
                            {
                                // 校验成功，焦点移到VSN
                                _controlUiService.FocusVsnTextBox();
                                return;
                            }
                            else
                            {
                                isValid = false;
                            }
                        }
                        else
                        {
                            isValid = false;
                        }
                        break;
                }

                // 处理失败情况
                if (!isValid)
                {
                    _currentVehicle.Vin = "";
                    _controlUiService.FocusVinTextBox();
                }

                // 长度超过17位 
                if ((_currentVehicle.Vin?.Length ?? 0) > maxLength)

                {
                    _currentVehicle.Vin = "";
                    _controlUiService.FocusVinTextBox();
                }
            }
            catch (Exception ex)
            {
                _data.sysLogger.Error(ex, "VIN校验异常");
            }
            
        }
        #endregion

        #region 校验VSN，开始功能
        [RelayCommand]
        private async Task CheckVsnIsTrue()
        {
            int Length = 10;
            try
            {
                string vin = _currentVehicle.Vin?.ToUpper() ?? "";
                string vsn = _currentVehicle.Vsn?.ToUpper() ?? "";

                // 如果为空，直接返回
                if (string.IsNullOrEmpty(vsn))
                {
                    _controlUiService.FocusVsnTextBox();
                    return;
                }
                // 校验逻辑 必须先输入VIN
                bool isValid = true;
                if (vsn.Length == Length && vin != "")
                {
                    _currentVehicle.CxName = await _data.carService.GetCarNameAsync(vsn.Substring(0, 4));

                    if (string.IsNullOrEmpty(_currentVehicle.CxName))
                    {
                        isValid = false;
                        //startClass.ShowMsgCaption("[ERROR]未找到品种代码", 2);
                    }
                    else
                    {
                        // ====== 创建车辆专属日志 ======
                        _data.vehicleLogger = _data.loggerFactory.CreateVehicleLogger(vin);
                        try
                        {
                            await RunSomeTestAsync();
                            CurrentVehicle = new Vehicle();
                            return;                         // 直接退出，不执行后面所有校验
                        }
                        catch (Exception ex)
                        {
                            _data.sysLogger.Error(ex, "功能执行异常");
                            CurrentVehicle = new Vehicle();
                            return;
                        }
                        finally
                        {
                            // 无论成功失败，检测完成后关闭车辆日志
                            _data.loggerFactory.CloseLogger(_data.vehicleLogger);
                            _data.vehicleLogger = null;
                        }

                    }

                }

                // 处理失败情况
                if (!isValid)
                {
                    _currentVehicle.Vsn = "";
                    _controlUiService.FocusVsnTextBox();
                }

                // 长度超过
                if ((_currentVehicle.Vsn?.Length ?? 0) > Length)
                {
                    _currentVehicle.Vsn = "";
                    _controlUiService.FocusVsnTextBox();
                }
            }
            catch (Exception ex)
            {
                _data.sysLogger.Error(ex, "VSN校验异常");
            }
            

        }
        #endregion

        // 加载某个工位的配置（从数据库或配置文件读取）
        //public void LoadStationConfig(string stationId)
        //{
        //    SelectedFunctionNames.Clear();

        //    // 模拟：根据工位ID获取该工位需要执行的功能列表
        //    // 实际项目中，这个数据可能来自数据库或配置文件
        //    string[] functionList = GetFunctionListByStation(stationId);

        //    foreach (string funcName in functionList)
        //    {
        //        if (_allFunctionMap.ContainsKey(funcName))
        //        {
        //            SelectedFunctionNames.Add(funcName);
        //        }
        //    }
        //}

        ////开始执行功能流程
        //private void StartFunctions()
        //{
        //    foreach (string functionName in SelectedFunctionNames)
        //    {
        //        if (_allFunctionMap.TryGetValue(functionName, out IFunction function))  //TryGetValue字典安全获取值的方法，键存在返回true
        //        {

        //            string result = function.Execute();
        //            // 处理result...
        //        }
        //    }
        //}
        // ViewModels/MainWindowViewModel.cs

        /// <summary>
        /// VSN校验通过后，开始执行测试功能（最简单版：执行所有已注册的功能）
        /// </summary>
        private async Task RunSomeTestAsync()
        {
            await Task.Delay(200);

            _data.logshow.AddLog("INFO", "========== 开始执行检测流程 ==========");
            foreach (string funcName in AllFunctionNames)
            {
                if (_allFunctionMap.TryGetValue(funcName, out IFunction func))
                {
                    _data.logshow.AddLog("INFO", $"▶ 开始执行功能：{funcName}");
                    try
                    {
                        if (funcName == "DTC" && func is DtcFunction dtcFunc)   // DTC 需要传入 Vehicle
                        {
                            // ★ 自动切换到 DTC Tab（索引 2）
                            SelectedTabIndex = 2;
                            IsReadingDtc = true;
                            dtcFunc.SetVehicle(_currentVehicle);
                            bool success = await dtcFunc.ExecuteFunc();

                            // 更新绑定属性
                            DtcResultList = dtcFunc.DtcResults;

                            // 统计当前故障和历史故障个数（直接从 DtcList 展开计算）
                            int totalActive = DtcResultList
                                .Where(r => r.Success)
                                .SelectMany(r => r.DtcList)
                                .Count(d => d.IsActive);

                            int totalInactive = DtcResultList
                                .Where(r => r.Success)
                                .SelectMany(r => r.DtcList)
                                .Count(d => !d.IsActive);

                            DtcSummaryText = success
                                ? $"✅ DTC 检测完成，当前故障 {totalActive} 个，历史故障 {totalInactive} 个"
                                : $"⚠️ DTC 检测部分失败，当前故障 {totalActive} 个，历史故障 {totalInactive} 个";

                            IsReadingDtc = false;
                            _data.logshow.AddLog(success ? "PASS" : "FAIL",
                                $"{(success ? "✅" : "❌")} DTC 执行{(success ? "成功" : "失败")}");
                            // DTC 执行完成后
                            SelectedTabIndex = 0;  // 切回日志 Tab
                        }
                        else
                        {
                            bool success = await func.ExecuteFunc();
                            // 其他功能的处理...
                        }
                    }
                    catch (Exception ex)
                    {
                        _data.logshow.AddLog("ERROR", $"❌ {funcName} 执行异常：{ex.Message}");
                    }
                    
                }
            }
            _data.logshow.AddLog("INFO", "========== 检测流程结束 ==========");
        }
        private async Task TestFunction()
        {
            foreach (string functionName in AllFunctionNames)
            {
                if (_allFunctionMap.TryGetValue(functionName, out IFunction function))
                {

                    bool result =await function.ExecuteFunc();
                    _data.logshow.AddLog("INFO", "开始执行检测流程");
                    _data.logshow.AddLog("SEND", "发送扩展会话 10 03");
                    _data.logshow.AddLog("RECV", "收到正响应 50 03");
                    // 处理result...
                }
            }
        }
    }
}
