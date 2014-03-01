using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Mygod.Net;
using Mygod.Windows.Dialogs;

namespace Mygod.HideEar
{
    public sealed partial class YouTubeWindow
    {
        public YouTubeWindow(string link)
        {
            InitializeComponent();
            LinkBox.Text = link;
            var view = CollectionViewSource.GetDefaultView(videoLinks);
            view.GroupDescriptions.Add(new PropertyGroupDescription("Parent"));
            VideoDownloadList.ItemsSource = view;
            (analyzer = new Thread(() =>
            {
                var aborted = false;
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        videoLinks.Clear();
                        BusyBox.Visibility = Visibility.Visible;
                    });
                    foreach (var video in YouTube.Video.GetVideoFromLink(Settings.Proxy, link).SelectMany(video => video.FmtStreamMap))
                    {
                        var copy = video;
                        Dispatcher.Invoke(() => videoLinks.Add(copy));
                    }
                }
                catch (ThreadAbortException)
                {
                    aborted = true;
                }
                catch (Exception e)
                {
                    TaskDialog.Show(this, "发生错误：" + e.Message, "更多信息请见日志。", TaskDialogType.Error);
                    Log.Main.Write(e);
                }
                finally
                {
                    if (!aborted) Dispatcher.Invoke(() => BusyBox.Visibility = Visibility.Collapsed);
                }
            })).Start();
        }

        private void VideoClick(object sender, RoutedEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            if (hyperlink != null) Process.Start(hyperlink.NavigateUri.ToString());
        }

        private void VideoBrowse(object sender, RoutedEventArgs e)
        {
            Process.Start((((ContextMenu)((MenuItem)sender).Parent).Tag ?? string.Empty).ToString());
        }

        private void VideoAnalyze(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.AddToHideEarQueue(new AnalyzeYouTubeTask((((ContextMenu)((MenuItem)sender).Parent).Tag ?? string.Empty).ToString()));
        }

        private void VideoCopy(object sender, RoutedEventArgs e)
        {
            App.SetClipboardText((((ContextMenu)((MenuItem)sender).Parent).Tag ?? string.Empty).ToString());
        }

        private readonly ObservableCollection<YouTube.FmtStream> videoLinks = new ObservableCollection<YouTube.FmtStream>();
        private readonly Thread analyzer;

        private void VideoWannaDownload(object sender, EventArgs e)
        {
            var s = sender as MenuItem;
            VideoProcess((s == null ? "Copy" : s.Tag).ToString());
        }

        private void VideoProcess(string operation)
        {
            if (VideoDownloadList.SelectedItem == null) return;
            if (operation == "Copy") App.SetClipboardText((VideoDownloadList.SelectedItems.OfType<YouTube.FmtStream>())
                .Aggregate(string.Empty, (current, url) => current + (url.GetUrlExtended() + Environment.NewLine)));
            else
            {
                var links = VideoDownloadList.SelectedItems.OfType<YouTube.FmtStream>();
                foreach (var link in links)
                    try
                    {
                        switch (operation)
                        {
                            case "HideEar":
                                App.Current.MainWindow.AddToHideEarQueue(
                                    new DownloadTask(link.GetUrlExtended(), link.GetFileName(false), link.Parent.Url));
                                break;
                            case "Normal":
                                Process.Start(link.GetUrlExtended());
                                break;
                            case "Thunder":
                                Process.Start(LinkConverter.ThunderEncode(link.GetUrlExtended()));
                                break;
                            case "FlashGet":
                                Process.Start(LinkConverter.FlashGetEncode(link.GetUrlExtended()));
                                break;
                            case "QQDL":
                                Process.Start(LinkConverter.QQDLEncode(link.GetUrlExtended()));
                                break;
                            case "RayFile":
                                Process.Start(LinkConverter.RayFileEncode(link.GetUrlExtended()));
                                break;
                        }
                    }
                    catch (Win32Exception)
                    {
                        TaskDialog.Show(this, "您没有安装指定的软件，因此不能使用这项功能。", type: TaskDialogType.Error);
                        return;
                    }
            }
        }

        private void AbortTask(object sender, CancelEventArgs e)
        {
            try
            {
                analyzer.Abort();
            }
            catch
            {
            }
            IsClosed = true;
        }

        public bool IsClosed;
    }
}
