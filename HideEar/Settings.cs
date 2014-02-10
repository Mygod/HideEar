using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using Mygod.IO;

namespace Mygod.HideEar
{
    public sealed class Settings
    {
        static Settings()
        {
            var settingsFile = new IniFile("Settings.ini");
            IniSection settingsSection = new IniSection(settingsFile, "Settings"), proxySection = new IniSection(settingsFile, "Proxy");
            DownloadPathData = new StringData(settingsSection, "DownloadPath",
                                              Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads"));
            VideoFileNameData = new StringData(settingsSection, "VideoFileName", "%T%E");
            ProxyHostData = new StringData(proxySection, "Host", "127.0.0.1");
            MaxTasksData = new Int32Data(settingsSection, "MaxTasks", 50);
            ProxyPortData = new Int32Data(proxySection, "Port", 8087);
            UseProxyData = new BooleanData(proxySection, "UseProxy", false);
        }

        private static readonly StringData DownloadPathData, VideoFileNameData, ProxyHostData;
        private static readonly Int32Data ProxyPortData;
        private static readonly BooleanData UseProxyData;
        
        public static readonly Int32Data MaxTasksData;

        public static string DownloadPath
            { get { return DownloadPathData.Get(); } set { DownloadPathData.Set(value); OnPropertyChanged("DownloadPath"); } }
        public static string VideoFileName
            { get { return VideoFileNameData.Get(); } set { VideoFileNameData.Set(value); OnPropertyChanged("VideoFileName"); } }
        public static int MaxTasks
            { get { return MaxTasksData.Get(); } set { MaxTasksData.Set(value); OnPropertyChanged("MaxTasks"); } }
        public static string ProxyHost
            { get { return ProxyHostData.Get(); } set { ProxyHostData.Set(value); OnPropertyChanged("ProxyHost"); } }
        public static int ProxyPort
            { get { return ProxyPortData.Get(); } set { ProxyPortData.Set(value); OnPropertyChanged("ProxyPort"); } }
        public static bool UseProxy
            { get { return UseProxyData.Get(); } set { UseProxyData.Set(value); OnPropertyChanged("UseProxy"); } }

        public static WebProxy Proxy { get { return UseProxy ? new WebProxy(ProxyHost, ProxyPort) : null; } }

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;
        private static void OnPropertyChanged(string propertyName)
        {
            var handler = StaticPropertyChanged;
            if (handler != null) handler(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
