// Services/Impl/ControlUiService.cs
using System.Windows;

namespace EOLTest.Services.Impl
{
    public class ControlUiService : IControlUiService
    {
        // 提供设置MainWindow的方法，而不是构造函数注入
        private MainWindow? _mainWindow;

        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void FocusVinTextBox()
        {
            _mainWindow?.Dispatcher.Invoke(() =>
            {
                _mainWindow?.VinTextBox.Focus();
                //_mainWindow?.VinTextBox.SelectAll();
            });
        }

        public void FocusVsnTextBox()
        {
            _mainWindow?.Dispatcher.Invoke(() =>
            {
                _mainWindow?.VsnTextBox.Focus();
                _mainWindow?.VsnTextBox.SelectAll();
            });
        }

        /// <summary>
        /// 在 UI 线程上弹出模态消息框。
        /// 关键：使用 _mainWindow 作为 Owner，对话框居中显示在主窗口上。
        /// 用户点击"确定"后关闭对话框，软件继续运行不退出。
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">提示内容（支持换行符 \n）</param>
        public void ShowMessageBox(string title, string message)
        {
            // 优先使用主窗口所属线程的 Dispatcher，若不可用则使用 Application 的
            var dispatcher = _mainWindow?.Dispatcher ?? Application.Current?.Dispatcher;
            // Dispatcher.Invoke 确保在 UI 线程执行（ViewModel 可能在后台线程调用）
            dispatcher?.Invoke(() =>
            {
                MessageBox.Show(
                    _mainWindow,            // owner：对话框居中显示在主窗口上方
                    message,                // 正文
                    title,                  // 标题
                    MessageBoxButton.OK,    // 只有"确定"按钮（不阻塞程序退出）
                    MessageBoxImage.Warning // 黄色警告图标
                );
            });
        }
    }
}
