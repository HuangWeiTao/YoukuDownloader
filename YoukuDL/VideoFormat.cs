using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoukuDL
{
    public enum VideoFormat
    {
        [Description(".mp4")]
        MP4 = 1,

        [Description(".flv")]
        FLV = 2
    }
}
