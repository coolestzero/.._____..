// Copyright (c) TBD 2026.
//固定的一些配置参数
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EOLTest.Models
{
    public partial class GeneralConfig : ObservableObject
    {
        [ObservableProperty]
        private string _linkType; //连接方式

        [ObservableProperty]
        private string _deviceNumber; //设备编号

        [ObservableProperty]
        private string _factory; //工厂选择

        [ObservableProperty]
        private string _stationName;//站号名

        [ObservableProperty]
        private string _lineId;//检测线名

        [ObservableProperty]
        private int _tester; //测试模式 0非测试 1测试模式

        [ObservableProperty]
        private int _vsnLength; //VSN长度

        [ObservableProperty]
        private int _language; //语言 0 中文 1 英文

        [ObservableProperty]
        private int _confMode; //生产 0 或者 返修模式1

        [ObservableProperty]
        private int _linkMode; //获取XML联网 0 或本地模式 1

        [ObservableProperty]
        private int _selectSystem; //IMMO 0 一体化 1

        [ObservableProperty]
        private int _waitUploadTime;//等待上传时间 ms

        [ObservableProperty]
        private int _checkerLength;//检测员号长度

        [ObservableProperty]
        private int _carFontSize;//设置字体大小

    }
}
