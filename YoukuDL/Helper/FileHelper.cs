using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoukuDL.Helper
{
    public class FileHelper
    {       

        public static string GenerateRandomFilePath(string ext)
        {
            return System.IO.Path.Combine(ConfigHelper.TempDir, Guid.NewGuid() + ext);
        }

        public static void LocatingFile(string path)
        {
            string argument = "/select, \"" + path + "\"";

            Process.Start("explorer.exe", argument);
        }
    }
}
