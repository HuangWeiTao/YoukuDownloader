using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoukuDL.Helper
{
    public class ProcessHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exePath"></param>
        /// <param name="cmd"></param>
        /// <param name="maxTime"></param>
        /// <returns>
        /// Item1为标准输出, Item2为标准错误
        /// </returns>
        public static async Task<Tuple<string, string>> Run(string exePath, string cmd, TimeSpan maxTime)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = exePath;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = cmd;
            p.Start();
            p.WaitForExit((int)maxTime.TotalMilliseconds);

            string output = await p.StandardOutput.ReadToEndAsync();
            string error = await p.StandardError.ReadToEndAsync();

            return new Tuple<string, string>(output, error);
        }
    }
}
