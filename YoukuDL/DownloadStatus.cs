using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoukuDL
{
    public enum DownloadStatus
    {
        [Description("解析中")]
        Preparing = -1,

        [Description("待下载")]
        New = 0,

        [Description("下载中")]
        Downloading = 1,

        [Description("转换中")]
        Converting = 2,

        [Description("下载成功")]
        Success = 3,

        [Description("下载失败")]
        Error = 4
    }
}
