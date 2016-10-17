using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoukuDL.Helper
{

    public class ConfigHelper
    {
        public static string VideoResolveTool = ConfigurationManager.AppSettings["videoResolveTool"];
        public static string FFmpegTool = ConfigurationManager.AppSettings["ffmpegTool"];
        public static string DownloadDir = ConfigurationManager.AppSettings["downloadDir"];
        public static string TempDir = ConfigurationManager.AppSettings["tempDir"];
    }
}
