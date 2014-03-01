using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Mygod.Windows.Dialogs;

namespace Mygod.HideEar
{
    public sealed partial class App
    {
        private void Error(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Log.Main.Write(e.Exception);
            TaskDialog.Show(mainInstruction: "出现未知错误！详细信息请见日志。", content: e.Exception.Message, type: TaskDialogType.Error);
        }

        public static new App Current { get { return (App) Application.Current; } }
        public new MainWindow MainWindow { get { return (MainWindow)base.MainWindow; } }

        public static void SetClipboardText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (COMException)
            {
                TaskDialog.Show(mainInstruction: "复制链接时出错，请重试。", type: TaskDialogType.Error);
            }
        }
    }
}
