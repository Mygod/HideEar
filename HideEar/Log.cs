using System;
using System.IO;
using Mygod.Windows;

namespace Mygod.HideEar
{
    class Log
    {
        private Log(string logFile)
        {
            fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFile);
            writer = new StreamWriter(fileName, true) { AutoFlush = true };
            WriteLine(string.Format("{0} 开始运行。（于 {1} 编译）", CurrentApp.Title, CurrentApp.CompilationTime));
            writer.WriteLine("\t\t\tOS：" + Environment.OSVersion);
        }

        private readonly string fileName;
        private StreamWriter writer;
        private readonly object locker = new object();

        public void WriteLine(string stuff)
        {
            lock (locker) writer.WriteLine("[{0}]\t" + stuff, DateTime.Now);
        }
        public void Write(Exception e)
        {
            if (e == null) return;
            WriteLine("出现错误，详细信息如下：");
            lock (locker)
            {
                writer.WriteLine(e.GetMessage());
                writer.WriteLine();
                writer.WriteLine();
            }
        }
        public long Clear()
        {
            long result;
            lock (locker)
            {
                writer.Close();
                result = new FileInfo(fileName).Length;
                writer = new StreamWriter(fileName) { AutoFlush = true };
            }
            return result;
        }

        public static readonly Log Main = new Log("log.log");
    }
}
