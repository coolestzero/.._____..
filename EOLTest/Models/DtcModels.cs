
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EOLTest.Models
{
    public class DtcInfo : INotifyPropertyChanged
    {
        private string _code = string.Empty;
        private bool _isActive;
        /// <summary>故障码字符串，如 "P0301"</summary>
        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// 当前故障（testFailed，bit0 = 1）
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class EcuDtcResult : INotifyPropertyChanged
    {
        private string _ecuName = string.Empty;
        private ObservableCollection<DtcInfo> _dtcList = new ObservableCollection<DtcInfo>();
        private bool _success;
        private string _errorMessage = string.Empty;

        public string EcuName
        {
            get => _ecuName;
            set { _ecuName = value; OnPropertyChanged(); }
        }
        public ObservableCollection<DtcInfo> DtcList
        {
            get => _dtcList;
            set
            {
                if (_dtcList != null)
                    _dtcList.CollectionChanged -= OnDtcChanged;
                _dtcList = value;
                if (_dtcList != null)
                    _dtcList.CollectionChanged += OnDtcChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DtcSummary));
            }
        }
        public bool Success
        {
            get => _success;
            set { _success = value; OnPropertyChanged(); }
        }
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 供 XAML 直接显示的汇总字符串。例：“当前故障 2 个 | 历史故障 1 个”
        /// 注意：这里内部使用了 LINQ 统计，因为实体已经不提供单独的计数属性。
        /// </summary>
        public string DtcSummary
        {
            get
            {
                if (DtcList == null)
                    return "当前故障 0 个 | 历史故障 0 个";
                int active = DtcList.Count(d => d.IsActive);
                int inactive = DtcList.Count - active;
                return $"当前故障 {active} 个 | 历史故障 {inactive} 个";
            }
        }

        private void OnDtcChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(DtcSummary));
            if (e.NewItems != null)
                foreach (DtcInfo item in e.NewItems)
                    item.PropertyChanged += OnDtcItemChanged;
            if (e.OldItems != null)
                foreach (DtcInfo item in e.OldItems)
                    item.PropertyChanged -= OnDtcItemChanged;
        }

        private void OnDtcItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DtcInfo.IsActive))
                OnPropertyChanged(nameof(DtcSummary));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
