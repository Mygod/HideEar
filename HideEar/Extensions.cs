using Mygod.Net;

namespace Mygod.HideEar
{
    static class R
    {
        internal static string GetFileName(this YouTube.FmtStream link, bool ignoreExtensions = true)
        {
            return Settings.VideoFileName.Replace("%T", link.Parent.Title).Replace("%A", link.Parent.Author)
                .Replace("%E", ignoreExtensions ? string.Empty : link.Extension);
        }
        internal static string GetUrlExtended(this YouTube.FmtStream link)
        {
            return link.GetUrl(link.GetFileName());
        }

        private static readonly string[] Units = new[] { "字节", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "NB", "DB", "CB" };

        public static string GetSize(long size)
        {
            double byt = size;
            byte i = 0;
            while (byt > 1000)
            {
                byt /= 1024;
                i++;
            }
            var bytesstring = size.ToString("N");
            return byt.ToString("N") + " " + Units[i] + " (" + bytesstring.Remove(bytesstring.Length - 3) + ' ' + Units[0] + ')';
        }
    }
}
