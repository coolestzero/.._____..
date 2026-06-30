// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Services
{
    public interface IControlUiService
    {
        /// <summary>焦点移到 VIN 输入框</summary>
        void FocusVinTextBox();
        /// <summary>焦点移到 VSN 输入框</summary>
        void FocusVsnTextBox();

        /// <summary>
        /// 在 UI 线程弹出模态消息框（不关闭软件，用户确认后继续）
        /// </summary>
        /// <param name="title">标题栏文字</param>
        /// <param name="message">消息正文</param>
        void ShowMessageBox(string title, string message);
    }
}
