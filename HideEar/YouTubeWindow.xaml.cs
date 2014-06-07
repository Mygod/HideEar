using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mygod.Net;

namespace Mygod.HideEar
{
    public sealed partial class YouTubeWindow
    {
        public YouTubeWindow(string link)
        {
            InitializeComponent();
            LinkBox.Text = link;
            var view = CollectionViewSource.GetDefaultView(downloads);
            view.GroupDescriptions.Add(new PropertyGroupDescription("Parent"));
            VideoDownloadList.ItemsSource = view;
            (analyzer = new Thread(() =>
            {
                var aborted = false;
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        downloads.Clear();
                        BusyBox.Visibility = Visibility.Visible;
                    });
                    foreach (var video in YouTube.Video.GetVideoFromLink(link, Settings.Proxy)
                                                       .SelectMany(video => video.Downloads))
                    {
                        var copy = video;
                        Dispatcher.Invoke(() => downloads.Add(copy));
                    }
                }
                catch (ThreadAbortException)
                {
                    aborted = true;
                }
                catch (Exception e)
                {
                    TaskDialog.Show(this, "错误", "发生错误：" + e.Message, "更多信息请见日志。", TaskDialogType.Error);
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
            App.Current.MainWindow.AddToHideEarQueue
                (new AnalyzeYouTubeTask((((ContextMenu)((MenuItem)sender).Parent).Tag ?? string.Empty).ToString()));
        }

        private void VideoCopy(object sender, RoutedEventArgs e)
        {
            App.SetClipboardText((((ContextMenu)((MenuItem)sender).Parent).Tag ?? string.Empty).ToString());
        }

        private readonly ObservableCollection<YouTube.Downloadable>
            downloads = new ObservableCollection<YouTube.Downloadable>();
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
                var links = VideoDownloadList.SelectedItems.OfType<YouTube.Downloadable>();
                foreach (var link in links)
                    try
                    {
                        switch (operation)
                        {
                            case "HideEar":
                                App.Current.MainWindow.AddToHideEarQueue(new DownloadTask(link.GetUrlExtended(),
                                    link.GenerateFileName(false).ToValidPath(), link.Parent.Url));
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
                        TaskDialog.Show(this, "错误", "您没有安装指定的软件，因此不能使用这项功能。",
                                        type: TaskDialogType.Error);
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

    [ValueConversion(typeof(YouTube.Video), typeof(string))]
    public sealed class VideoPropertiesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var video = value as YouTube.Video;
            if (video == null) return null;
            return string.Format("标题：{0}{8}上传者：{1}{8}关键字：{2}{8}平均评分：{3}{8}观看次数：{4}{8}" +
                                 "上传时间：{5}{8}时长：{6}{8}地址：{7}", video.Title, video.Author,
                                 string.Join(", ", video.Keywords), video.AverageRating, video.ViewCount,
                                 video.UploadTime, video.Length, video.Url, Environment.NewLine);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
