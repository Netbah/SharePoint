using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace SharePoint.CopyList.Logging {
    public class DetailLog {

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static Logger GetLogger { 
            get {
                return logger;   
            }
        }


        static void Main() { }
    }
}
