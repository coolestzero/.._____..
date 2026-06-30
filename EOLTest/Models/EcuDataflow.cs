using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EOLTest.Models
{
    public partial class EcuDataflow : ObservableObject
    {
        [ObservableProperty]
        private string _ecuName;

        private string _engineName;

        private string _supplier;

        private int _checkNum;

        private string _sid;

        private string _lid;

        [ObservableProperty]
        private string _describe;

        [ObservableProperty]
        private string _engDescribe;

        private int _pos;

        private double _conv1;

        private double _offset;

        private int _denumerator;

        private int _size;

        private string _type;

        [ObservableProperty]
        private string _unit;

        [ObservableProperty]
        private string _min;

        [ObservableProperty]
        private string _max;

        public string Supplier { get => _supplier; set => _supplier = value; }
        public int CheckNum { get => _checkNum; set => _checkNum = value; }
        public string Sid { get => _sid; set => _sid = value; }
        public string Lid { get => _lid; set => _lid = value; }
        public int Pos { get => _pos; set => _pos = value; }
        public double Conv1 { get => _conv1; set => _conv1 = value; }
        public double Offset { get => _offset; set => _offset = value; }
        public int Denumerator { get => _denumerator; set => _denumerator = value; }
        public int Size { get => _size; set => _size = value; }
        public string Type { get => _type; set => _type = value; }
        public string EngineName { get => _engineName; set => _engineName = value; }
    }
}
