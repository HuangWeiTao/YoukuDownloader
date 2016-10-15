using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoukuDL.Infrastructure
{
    public interface ILog
    {
        void Debug(string msg);

        void Error(string msg, Exception e);

        void Error(string msg);

        void Info(string msg);
    }
}
