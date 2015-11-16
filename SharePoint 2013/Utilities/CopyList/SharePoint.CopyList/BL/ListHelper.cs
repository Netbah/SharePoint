using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using System.Collections.Generic;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.Utilities;
using System.Web.UI.WebControls.WebParts;
using SharePoint.CopyList.Logging;
using System.Globalization;

namespace SharePoint.CopyList.BL
{
    public class ListHelper
    {
        static ILogger logger = DetailLog.GetLogger;

        public static SPList CreateNewListAsSourceList(SPList sourceList, SPWeb web) {
            var template = getListTemlateDefinition(web, sourceList.TemplateFeatureId);
            string title = sourceList.RootFolder.Name + System.DateTime.Now.ToLocalTime().ToString(); //TODO -  for test only 
            string description = sourceList.Description;
            // RenameSourceList(sourceList);
            Guid guid = CreateList(web, title, description, template);            
            SPList newList = web.Lists.GetList(guid, false);
            PermissionHelper.CopyListRoleAssignments(sourceList, newList);
            copyTitleResources(sourceList, newList);
            return newList;
        }

        public static SPListTemplate getListTemlateDefinition(SPWeb web, Guid templateId)
        {
            SPListTemplate tpl = null;
            foreach (SPListTemplate template in web.ListTemplates)
            {
                if (template.FeatureId == templateId)
                {
                    tpl = template;
                    break;
                }
            }
            return tpl;
        }

        public static void RenameSourceList(SPList list) {
            list.Title = list.Title + "Old";
            list.RootFolder.MoveTo(list.RootFolder.Url.Replace(list.RootFolder.Url, list.RootFolder.Url + "Old"));
        }

        public static void reverseChanges(SPList list){
           list.Title = list.Title.Replace("Old", "");
           list.RootFolder.MoveTo(list.RootFolder.Url.Replace(list.RootFolder.Url, list.RootFolder.Url.Replace("Old", "")));
        }

        private static Guid CreateList (SPWeb web, string title, string description, SPListTemplate template){
            try {
                return web.Lists.Add(title, description, template);
            }
            catch(Exception ex) {
                throw ex;
            }
        }

        private static void copyTitleResources(SPList sourceList, SPList newList) {
            foreach (CultureInfo c in sourceList.ParentWeb.SupportedUICultures) {
                newList.TitleResource.SetValueForUICulture(c, sourceList.TitleResource.GetValueForUICulture(c));
                newList.Update();
            }
        }

        public static SPList getListByUrl(SPWeb web, string listUrl){
           try {
               return web.GetList(web.ServerRelativeUrl + "/lists/" + listUrl);
           }
           catch (Exception ex) {
               logger.Error("List " + listUrl + " was not found", ex.Message.ToString());
               throw ex;
           }
        }

        public static void AssignJsLinkToListViews(SPWeb web, SPView view, string jsLink) {
            SPLimitedWebPartManager limitedWebPartManager = null;
            XsltListViewWebPart providerWP = null;
            try {
                limitedWebPartManager = web.GetLimitedWebPartManager(view.Url, PersonalizationScope.Shared);
                providerWP = (from Microsoft.SharePoint.WebPartPages.WebPart webPart in limitedWebPartManager.WebParts where webPart is XsltListViewWebPart select webPart).First() as XsltListViewWebPart;
                providerWP.JSLink = jsLink;
                limitedWebPartManager.SaveChanges(providerWP);
                logger.Trace("jsLink "+ jsLink +" for" + view.Title + " assigned successfully"); 
            }
            catch (Exception ex) {
                logger.Error("jsLink " + jsLink + " not assigned! With error: ", ex.Message); 
            }
            finally {
                if (limitedWebPartManager != null) {
                    if (limitedWebPartManager.Web != null) {
                        limitedWebPartManager.Web.Dispose();
                    }
                    limitedWebPartManager.Dispose();
                }
                providerWP.Dispose();
            }
    }

        internal static string getJSLinkFromView(SPWeb web, SPView view) {
            SPLimitedWebPartManager limitedWebPartManager = null;
            XsltListViewWebPart providerWP = null;
            try {
                limitedWebPartManager = web.GetLimitedWebPartManager(view.Url, PersonalizationScope.Shared);
                providerWP = (from Microsoft.SharePoint.WebPartPages.WebPart webPart in limitedWebPartManager.WebParts where webPart is XsltListViewWebPart select webPart).First() as XsltListViewWebPart;
                logger.Trace("jsLink for" + view.Title + " got"); 
                return providerWP.JSLink;
            }
            catch (Exception ex) {
                logger.Error("jsLink " + view.Title + " not got! With error: ", ex.Message); 
                return "";
            }
            finally {
                if (limitedWebPartManager != null) {
                    if (limitedWebPartManager.Web != null) {
                        limitedWebPartManager.Web.Dispose();
                    }
                    limitedWebPartManager.Dispose();
                }
                providerWP.Dispose();
            }
        }
    }
}
