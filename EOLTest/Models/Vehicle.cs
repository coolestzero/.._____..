// Copyright (c) TBD 2026.
//车辆信息类
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EOLTest.Models
{
    public partial class Vehicle : ObservableObject
    {
        [ObservableProperty]
        private string _vin;

        [ObservableProperty]
        private string _vsn;

        [ObservableProperty]
        private string _cxName;

        [ObservableProperty]
        private string _engineName;

        [ObservableProperty]
        private ObservableCollection<EcuModule> _ecuModules;

        // 自动初始化，可以在构造函数中 
        public Vehicle()
        {
            _ecuModules = new ObservableCollection<EcuModule>();
        }
    }
}
