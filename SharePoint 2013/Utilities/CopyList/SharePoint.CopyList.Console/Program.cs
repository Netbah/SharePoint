using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharePoint.CopyList;
using Microsoft.SharePoint.Administration;
using SharePoint.CopyList.Logging;
using SharePoint.CopyList.BL;
using NLog;

namespace SharePoint.CopyList.Console
{
    class Program
    {

        static void Main(string[] args)
        {
            ILogger logger = DetailLog.GetLogger;
            using (var site = new SPSite("https://url-site-level/"))
            {
                try
                {
                    using (var web = site.OpenWeb("/subsite"))
                    {
                        SPList orders = ListHelper.getListByUrl(web, "Orders");
                        ListHelper.reverseChanges(orders);
                        try
                        {
                            RestoreInstance restore = new RestoreInstance(web);
                            restore.RestoreAsNewList(orders);
                        }
                        catch (Exception ex)
                        {
                            ListHelper.reverseChanges(orders);
                            logger.Fatal(ex.ToString());
                            System.Console.ReadKey();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex.ToString());
                    System.Console.ReadKey();
                }
            }
        }
    }
}
