using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoukuDL.Infrastructure
{
    public class Log : ILog
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public void Debug(string msg)
        {
            logger.Debug(msg);
        }

        public void Error(string msg)
        {
            logger.Error(msg);
        }

        public void Error(string msg, Exception e)
        {
            logger.Error(e, msg);
        }

        public void Info(string msg)
        {
            logger.Info(msg);
        }
    }
}
