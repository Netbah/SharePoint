using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using System.Collections.Generic;
using SharePoint.CopyList.Logging;
using System.Globalization;

namespace SharePoint.CopyList.BL {
    public class ViewHelper {
        static ILogger logger = DetailLog.GetLogger;

        public static void RestoreViews(SPWeb web, SPList sourceList, SPList newList) {
            foreach (var vg in newList.Views.OfType<SPView>().Select(v => v.ID).ToList()) {
                newList.Views.Delete(vg);
            }
            foreach (SPView view in sourceList.Views) {
                string title = getTitleView(view.Url);
                SPView newView = newList.Views.Add(title, view.ViewFields.ToStringCollection(), view.Query, view.RowLimit, view.Paged, view.DefaultView);
                copyTitleResources(view, newView);
                copyJSLinks(web, view, newView);
                newView.Update();
                logger.Debug("The view: " + view.Title + " was added!");
            }
        }

        private static void copyJSLinks(SPWeb web, SPView sourceView, SPView newView) {
            string jsLink = ListHelper.getJSLinkFromView(web, sourceView);
            ListHelper.AssignJsLinkToListViews(web, newView, jsLink);
        }

        private static void copyTitleResources(SPView sourceView, SPView newView) {
            foreach (CultureInfo c in newView.ParentList.ParentWeb.SupportedUICultures) {
                newView.TitleResource.SetValueForUICulture(c, sourceView.TitleResource.GetValueForUICulture(c));
                newView.Update();
            }
        }

        private static string getTitleView(string url) {
            return url.Split('/').Last().Replace(".aspx", "");
        }
    }
}
