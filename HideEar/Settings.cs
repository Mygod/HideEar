using System;
using System.IO;
using System.Net;
using Mygod.IO;

namespace Mygod.HideEar
{
    static class Settings
    {
        static Settings()
        {
            SettingsFile = new IniFile("Settings.ini");
            SettingsSection = new IniSection(SettingsFile, "Settings");
            ProxySection = new IniSection(SettingsFile, "Proxy");
            DownloadPathData = new StringData(SettingsSection, "DownloadPath",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads"));
            VideoFileNameData = new StringData(SettingsSection, "VideoFileName", "%T%E");
            ProxyHostData = new StringData(ProxySection, "Host", "127.0.0.1");
            MaxTasksData = new Int32Data(SettingsSection, "MaxTasks", 50);
            ProxyPortData = new Int32Data(ProxySection, "Port", 8087);
            UseProxyData = new BooleanData(ProxySection, "UseProxy", false);
            Client.Proxy = Proxy;
        }

        private static readonly IniFile SettingsFile;
        private static readonly IniSection SettingsSection, ProxySection;
        private static readonly StringData DownloadPathData, VideoFileNameData, ProxyHostData;
        private static readonly Int32Data ProxyPortData;
        private static readonly BooleanData UseProxyData;

        public static readonly Int32Data MaxTasksData;

        internal static string DownloadPath { get { return DownloadPathData.Get(); } set { DownloadPathData.Set(value); } }

        internal static string VideoFileName { get { return VideoFileNameData.Get(); } set { VideoFileNameData.Set(value); } }

        internal static int MaxTasks { get { return MaxTasksData.Get(); } set { MaxTasksData.Set(value); } }

        internal static string ProxyHost { get { return ProxyHostData.Get(); } set { ProxyHostData.Set(value); Client.Proxy = Proxy; } }

        internal static int ProxyPort { get { return ProxyPortData.Get(); } set { ProxyPortData.Set(value); Client.Proxy = Proxy; } }

        internal static bool UseProxy { get { return UseProxyData.Get(); } set { UseProxyData.Set(value); Client.Proxy = Proxy; } }

        internal static WebProxy Proxy { get { return UseProxy ? new WebProxy(ProxyHost, ProxyPort) : null; } }

        internal static readonly WebClient Client = new WebClient();
    }
}
