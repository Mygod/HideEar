using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Mygod.HideEar
{
    public sealed partial class App
    {
        private void Error(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Log.Main.Write(e.Exception);
            MessageBox.Show(e.Exception.Message);
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
                MessageBox.Show("复制链接时出错，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
