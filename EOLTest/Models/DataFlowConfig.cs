// Copyright (c) TBD 2026.
//数据流设置信息
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EOLTest.Models
{
    public partial class DataFlowConfig : ObservableObject
    {
        [ObservableProperty]
        private int _ecmTime; //读取ECM数据流时间 秒

        [ObservableProperty]
        private int _vcuTime;

        [ObservableProperty]
        private int _hcuTime;

        [ObservableProperty]
        private int _mcuTime;

        [ObservableProperty]
        private int _bmsTime;

        [ObservableProperty]
        private int _dcdcTime;

        [ObservableProperty]
        private int _cduTime;

        [ObservableProperty]
        private int _tcmTime;

        [ObservableProperty]
        private int _pcuTime;

        [ObservableProperty]
        private int _acTime;

        [ObservableProperty]
        private int _heatWait; //加热等待时间

        [ObservableProperty]
        private int _obcSlowWait;//慢充等待时间

        [ObservableProperty]
        private int _obcFastWait;//快充等待时间

        [ObservableProperty]
        private string _calMode;//计算数据流模式，平均 最大最小 最后一次
    }
}
