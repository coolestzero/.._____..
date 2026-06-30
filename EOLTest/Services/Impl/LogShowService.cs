using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace EOLTest.Services.Impl
{
    /// <summary>
    /// 日志显示服务 —— 使用 RichTextBox 实现彩色日志
    /// 
    /// 设计说明：
    /// - 服务本身在 DI 容器中注册为单例
    /// - RichTextBox 是 UI 控件，不能通过构造函数注入
    /// - 采用"后期注入"模式：在 App.Startup 中调用 SetLogRichTextBox()
    /// </summary>
    public class LogShowService : ILogShowService
    {
        // ========== RichTextBox 引用（后期注入） ==========
        private RichTextBox? _logRichTextBox;

        /// <summary>
        /// 由外部（MainWindow）注入 RichTextBox 控件引用
        /// 这个方法在 Application_Startup 中调用
        /// </summary>
        public void SetLogRichTextBox(RichTextBox richTextBox)
        {
            _logRichTextBox = richTextBox ?? throw new ArgumentNullException(nameof(richTextBox));

            // 设置 RichTextBox 只读，防止用户编辑
            _logRichTextBox.IsReadOnly = true;

            // 给 FlowDocument 设置一个足够大的 PageWidth，防止自动换行
            // 这样每条日志都会在一行显示完整，不会因为窗口窄而断行
            if (_logRichTextBox.Document is FlowDocument doc)
            {
                doc.PageWidth = 1000;
            }
        }

        // ========== 核心方法：添加日志 ==========

        /// <summary>
        /// 添加一条彩色日志
        /// </summary>
        /// <param name="level">
        /// 日志级别，支持以下值（颜色自动匹配）：
        ///   "SEND"  → 蓝色（发送报文）
        ///   "RECV"  → 绿色（接收报文）
        ///   "ERROR" → 红色（错误）
        ///   "WARN"  → 橙色（警告）
        ///   "INFO"  → 浅灰（普通信息）
        ///   "DEBUG" → 暗灰（调试信息）
        /// </param>
        /// <param name="message">日志正文，如 "1003 → 02 10 03 00 00 00"</param>
        public void AddLog(string level, string message)
        {
            // 安全检查：如果服务在非 UI 线程被调用，必须切换到 UI 线程
            // 否则操作 RichTextBox 会抛异常
            if (Application.Current?.Dispatcher == null)
            {
                // 应用程序尚未初始化，直接返回（这种情况极少发生）
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_logRichTextBox == null)
                {
                    // RichTextBox 尚未注入，忽略此次日志
                    // 生产环境可以在这里打一个 Debug 日志
                    System.Diagnostics.Debug.WriteLine($"[LogShowService] RichTextBox 未注入，跳过日志: [{level}] {message}");
                    return;
                }

                string time = DateTime.Now.ToString("HH:mm:ss.fff");

                // 创建段落（Paragraph），每个段落 = 一行日志
                Paragraph paragraph = new Paragraph
                {
                    Margin = new Thickness(0, 1, 0, 0),  // 段间距 1px，紧凑排列
                    LineHeight = 1                        // 行高（相对值）
                };

                // --- ① 时间戳（灰色，字号小一点） ---
                paragraph.Inlines.Add(new Run($"[{time}] ")
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),  // 灰色
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas")
                });

                // --- ② 级别标签（根据级别变色，加粗） ---
                SolidColorBrush levelBrush = GetLevelBrush(level);
                paragraph.Inlines.Add(new Run($"[{level}] ")
                {
                    Foreground = levelBrush,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas")
                });

                // --- ③ 日志正文（默认浅色） ---
                paragraph.Inlines.Add(new Run(message)
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),  // 浅灰白
                    FontSize = 12,
                    FontFamily = new FontFamily("Consolas")
                });

                // --- 把段落加入文档 ---
                _logRichTextBox.Document.Blocks.Add(paragraph);

                // --- 限制日志行数（防止内存暴涨） ---
                // 保留最近 2000 行，超过则删除最旧的行
                const int maxBlocks = 2000;
                while (_logRichTextBox.Document.Blocks.Count > maxBlocks)
                {
                    _logRichTextBox.Document.Blocks.Remove(
                        _logRichTextBox.Document.Blocks.FirstBlock);
                }

                // --- 自动滚动到底部 ---
                _logRichTextBox.ScrollToEnd();
            });
        }

        /// <summary>
        /// 清空所有日志
        /// </summary>
        public void Clear()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _logRichTextBox?.Document.Blocks.Clear();
            });
        }

        // ========== 私有辅助方法 ==========

        /// <summary>
        /// 根据日志级别返回对应的画刷颜色
        /// </summary>
        private static SolidColorBrush GetLevelBrush(string level)
        {
            return level.ToUpperInvariant() switch
            {
                "SEND" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),   // 蓝色 —— 发送
                "RECV" => new SolidColorBrush(Color.FromRgb(39, 174, 96)),    // 绿色 —— 接收
                "ERROR" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),    // 红色 —— 错误
                "WARN" => new SolidColorBrush(Color.FromRgb(243, 156, 18)),   // 橙色 —— 警告
                "INFO" => new SolidColorBrush(Color.FromRgb(189, 195, 199)),  // 浅灰 —— 信息
                "DEBUG" => new SolidColorBrush(Color.FromRgb(100, 100, 100)),  // 暗灰 —— 调试
                _ => new SolidColorBrush(Color.FromRgb(180, 180, 180))   // 默认灰色
            };
        }
    }
}
