using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Models
{
    public class EcuRead
    {
        private string _cxName;
        private string _ecuName;
        private string _supplier;
        private string _supplier2;
        private string _configure;
        private string _mask;
        private string _mask2;
        private string _isAes;

        public string CxName { get => _cxName; set => _cxName = value; }
        public string EcuName { get => _ecuName; set => _ecuName = value; }
        public string Supplier { get => _supplier; set => _supplier = value; }
        public string Supplier2 { get => _supplier2; set => _supplier2 = value; }
        public string Configure { get => _configure; set => _configure = value; }
        public string Mask { get => _mask; set => _mask = value; }
        public string Mask2 { get => _mask2; set => _mask2 = value; }
        public string IsAes { get => _isAes; set => _isAes = value; }
    }
}
