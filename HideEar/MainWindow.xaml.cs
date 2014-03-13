using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mygod.Net;
using Mygod.Windows.Dialogs;

namespace Mygod.HideEar
{
    public sealed partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Settings.MaxTasksData.DataChanged += (sender, e) => HideEarProcess(Settings.MaxTasks);
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #region 掩耳

        private readonly TaskFactory factory = new TaskFactory();
        private readonly Queue<Task> hideEarQueue = new Queue<Task>();
        private readonly HashSet<Task> runningItems = new HashSet<Task>();

        private string[] Links { get { return LinkBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); } }

        private void PopContextMenu(object sender, RoutedEventArgs e)
        {
            var s = (FrameworkElement)sender;
            s.ContextMenu.PlacementTarget = s;
            s.ContextMenu.Placement = PlacementMode.Bottom;
            s.ContextMenu.IsOpen = true;
        }

        private void Convert(object sender, RoutedEventArgs e)
        {
            Convert((LinkType) Enum.Parse(typeof(LinkType), ((FrameworkElement) sender).Tag.ToString()));
        }
        private void Convert(LinkType linkType)
        {
            LinkBox.Text = Links.Select(i => LinkConverter.ConvertUrl(linkType, i))
                .Aggregate(string.Empty, (current, i) => current + (i + Environment.NewLine));
        }

        private void HideEarProcess(object sender, RoutedEventArgs e)
        {
            AddToHideEarQueue(Links.Select(i => new DownloadTask(LinkConverter.ConvertUrl(LinkType.Normal, i))).ToList());
        }

        private void ToolDownload(object sender, RoutedEventArgs e)
        {
            var errorsCount = 0;
            foreach (var link in Links)
                try
                {
                    Process.Start(link);
                }
                catch (Win32Exception)
                {
                    errorsCount++;
                }
            if (errorsCount > 0)
                TaskDialog.Show(this, "完成", string.Format("已完成，但是有 {0} 项错误发生。", errorsCount),
                                type: TaskDialogType.Information);
        }

        private void AddToHideEarQueue(IEnumerable<Task> tasks)
        {
            var count = 0;
            foreach (var task in tasks)
            {
                count++;
                hideEarQueue.Enqueue(task);
            }
            Progress.Maximum += count;
            Finished.Maximum += count;
            Errors.Maximum += count;
            HideEarProcess();
            ProgressChanged();
        }

        public void AddToHideEarQueue(Task task)
        {
            Progress.Maximum++;
            Finished.Maximum++;
            Errors.Maximum++;
            hideEarQueue.Enqueue(task);
            HideEarProcess();
            ProgressChanged();
        }

        private void HideEarProcess(int? maxThreads = null)
        {
            var count = Math.Min((maxThreads ?? Settings.MaxTasks) - runningItems.Count, hideEarQueue.Count);
            for (var i = 0; i < count; i++) HideEarProcessNext();
        }
        private void HideEarProcessNext(Task pre = null)
        {
            try
            {
                Task t;
                lock (hideEarQueue)
                {
                    if (pre != null) runningItems.Remove(pre);
                    var count = Math.Min(Settings.MaxTasks - runningItems.Count, hideEarQueue.Count);
                    if (count <= 0) return;
                    t = hideEarQueue.Dequeue();
                    runningItems.Add(t);
                }
                factory.StartNew(() =>
                {
                    Dispatcher.Invoke((Action)(() => Progress.Value++));
                    try
                    {
                        t.DoIt(Dispatcher);
                        Dispatcher.Invoke(() =>
                        {
                            Finished.Value++;
                            Errors.Maximum--;
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Main.WriteLine(string.Format("掩耳处理时出现错误，详细信息：{0}{2}{1}", t, e.GetMessage(), Environment.NewLine
                            + (string.IsNullOrEmpty(t.AdditionalMessage) ? string.Empty : t.AdditionalMessage + Environment.NewLine)));
                        Dispatcher.Invoke(() =>
                        {
                            Errors.Value++;
                            Finished.Maximum--;
                        });
                    }
                    HideEarProcessNext(t);
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void BrowseDirectory(object sender, RoutedEventArgs e)
        {
            var fb = new System.Windows.Forms.FolderBrowserDialog { ShowNewFolderButton = true,
                                                                    Description = @"请选择掩耳下载存放目录" };
            if (fb.ShowDialog() != System.Windows.Forms.DialogResult.Cancel) PathBox.Text = fb.SelectedPath;
        }

        private void BrowseInExplorer(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", '"' + Settings.DownloadPath + '"');
        }

        private void ProgressChanged(object sender = null, RoutedPropertyChangedEventArgs<double> e = null)
        {
            if (Progress.Maximum <= 0)
            {
                TaskbarItem.ProgressState = TaskbarItemProgressState.Indeterminate;
                return;
            }
            TaskbarItem.ProgressState = Errors.Value > 0 ? TaskbarItemProgressState.Error : TaskbarItemProgressState.Normal;
            TaskbarItem.ProgressValue = (Finished.Value + Errors.Value) / Progress.Maximum;
        }

        private void AnalyzeYouTube(object sender, RoutedEventArgs e)
        {
            AddToHideEarQueue(Links.Select(link => new AnalyzeYouTubeTask(link)));
        }

        #endregion

        #region 设置

        private void ShowLog(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.log"));
        }

        private void CleanLog(object sender, RoutedEventArgs e)
        {
            TaskDialog.Show(this, "清理完毕", "清理日志成功。",
                            string.Format("共清理了 {0}。", R.GetSize(Log.Main.Clear())), TaskDialogType.Information);
        }

        private void Help(object sender, RoutedEventArgs e)
        {
            Process.Start("http://mygod.tk/product/hide-ear/");
        }

        private void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            WebsiteManager.CheckForUpdates(
                () => TaskDialog.Show(this, "检查更新完毕", "没有可用更新。", type: TaskDialogType.Information),
                exc => TaskDialog.Show(this, "错误", "检查更新失败。", type: TaskDialogType.Error,
                                       expandedInfo: exc.GetMessage()));
        }

        #endregion
    }

    public abstract class Task
    {
        public abstract void DoIt(Dispatcher dispatcher);

        public string AdditionalMessage { get; protected set; }
    }

    public sealed class DownloadTask : Task
    {
        public DownloadTask(string downloadPath, string fileName, string additional = null)
        {
            this.downloadPath = downloadPath;
            this.fileName = fileName;
            AdditionalMessage = additional;
        }
        public DownloadTask(string downloadPath)
        {
            this.downloadPath = downloadPath;
        }

        private readonly string downloadPath, fileName;

        public override void DoIt(Dispatcher dispatcher)
        {
            Directory.CreateDirectory(Settings.DownloadPath);
        retry:
            try
            {
                // ReSharper disable AssignNullToNotNullAttribute
                var request = WebRequest.Create(downloadPath);
                request.Proxy = Settings.Proxy;
                var response = request.GetResponse();
                var name = fileName;
                if (name == null)
                {
                    var disposition = (response.Headers["Content-Disposition"] ?? string.Empty).ToLowerInvariant();
                    var pos = disposition.IndexOf("filename=", StringComparison.Ordinal);
                    name = pos >= 0 ? disposition.Substring(pos + 9).Trim('"', '\'') : Path.GetFileName(downloadPath);
                }
                if (string.IsNullOrWhiteSpace(name)) name = "noname";
                if (name.Contains('?')) name = name.Substring(0, name.IndexOf('?'));
                if (name.Contains('#')) name = name.Substring(0, name.IndexOf('#'));
                var path = Path.Combine(Settings.DownloadPath, name);
                if (FileReallyExists(path))
                {
                    var extension = Path.GetExtension(path);
                    var start = path.Substring(0, path.Length - extension.Length);
                    var i = 0;
#pragma warning disable 642
                    while (FileReallyExists(path = start + " (" + ++i + ')' + extension))
                    {
                    }
#pragma warning restore 642
                }
                using (var reader = new BinaryReader(response.GetResponseStream()))
                using (var file = new FileStream(path, FileMode.Create))
                using (var writer = new BinaryWriter(file))
                {
                    var buffer = new byte[4096];
                    int i;
                    do
                    {
                        i = reader.Read(buffer, 0, 4096);
                        writer.Write(buffer, 0, i);
                    } while (i > 0);
                }
                // ReSharper restore AssignNullToNotNullAttribute
            }
            catch (WebException e)
            {
                if (e.Message.Contains("操作已超时") || e.Message.ToLowerInvariant().Contains("timeout")) goto retry;
                throw;
            }
        }

        private static bool FileReallyExists(string path)
        {
            return File.Exists(path) && new FileInfo(path).Length > 0;
        }

        public override string ToString()
        {
            return downloadPath;
        }
    }

    public sealed class AnalyzeYouTubeTask : Task
    {
        public AnalyzeYouTubeTask(string link)
        {
            this.link = link;
        }

        private readonly string link;

        public override void DoIt(Dispatcher dispatcher)
        {
            YouTubeWindow window = null;
            dispatcher.Invoke(() => (window = new YouTubeWindow(link)).Show());
            while (window == null || !window.IsClosed) Thread.Sleep(200);
        }

        public override string ToString()
        {
            return link;
        }
    }
}
