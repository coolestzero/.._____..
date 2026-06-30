using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Models
{
    public class EcuCanId
    {
        private string _ecuName;

        public string EcuName
        {
            get { return _ecuName; }
            set { _ecuName = value; }
        }

        private string _txId;

        public string TxId
        {
            get { return _txId; }
            set { _txId = value; }
        }

        private string _rxId;

        public string RxId
        {
            get { return _rxId; }
            set { _rxId = value; }
        }

        private string _linId;

        public string LinId
        {
            get { return _linId; }
            set { _linId = value; }
        }

        private string _doipId;

        public string DoipId
        {
            get { return _doipId; }
            set { _doipId = value; }
        }


    }
}
