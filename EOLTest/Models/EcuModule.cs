// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EOLTest.Models
{
    public partial class EcuModule : ObservableObject
    {
        [ObservableProperty]
        private string _ecuName;

        [ObservableProperty]
        private string _supplier;

        [ObservableProperty]
        private string _supplier2;//2级供应商代码

        [ObservableProperty]
        private string _partNumber;//零件号

        [ObservableProperty]
        private string _hwNumber;//ECU硬件号

        [ObservableProperty]
        private string _hwVersionNumber;//ECU硬件版本号

        [ObservableProperty]
        private string _swNumber;//ECU软件号

        [ObservableProperty]
        private string _swVersionNumber;//ECU软件版本号

        [ObservableProperty]
        private string _sgmwHwNumber;//ECU五菱硬件号

        [ObservableProperty]
        private string _sgmwSwNumber;//定义ECU五菱软件号

        [ObservableProperty]
        private string _vin;//定义vin

        [ObservableProperty]
        private string _onlineConfig;//定义在线配置值

    }
}
