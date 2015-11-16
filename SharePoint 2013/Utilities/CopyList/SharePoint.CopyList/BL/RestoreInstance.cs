using Microsoft.SharePoint;
using SharePoint.CopyList.Logging;
using SharePoint.CopyList.CopyListOrders.BL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NLog;



namespace SharePoint.CopyList.BL
{
    public class RestoreInstance
    {
        private SPWeb web;
        private SPList newList;
        private DBHelper db;
        private ILogger logger;


        public RestoreInstance(SPWeb web){
            this.web = web;
            this.db = new DBHelper(web.Site.ContentDatabase.DatabaseConnectionString);
            logger = DetailLog.GetLogger;
        }

        public void RestoreAsNewList(SPList list) {
            this.newList = ListHelper.CreateNewListAsSourceList(list, web);
            CopyListProperties(list);
            ViewHelper.RestoreViews(web, list, newList);
            CopyListItems(list);
            CopyOptionsNextItemId(list);
            CopyEventReceivers(list);
            ShowSummary();
        }

        private void ShowSummary() {
            logger.Info("List : " + newList.Title + " was restored");
            logger.Info("List Url: " + newList.RootFolder.Url);
            logger.Info("Count Items: " + newList.Items.Count);
            logger.Info("Count Event Reseivers: " + newList.EventReceivers.Count);
            logger.Info("Count Views: " + newList.Views.Count);
            Console.ReadKey();
        }

        private void CopyListItems(SPList sourceList) {
            var listItems = getAllListItems(sourceList);
            for (int count = 0; count < listItems.Count; count++) {
                if (db.SetListNextItemId(listItems[count].ID, newList.ID) > 0) {
                    RestoreListElements(listItems[count]);
                    logger.Debug("The item: " + listItems[count].Title + " was copied!");
                }
                else {
                    logger.Error("The item: " + listItems[count].Title + " was missing!");
                }

            }
        }

        private void CopyListProperties(SPList sourceList) {
            this.newList.EnableModeration = sourceList.EnableModeration; ;
            this.newList.EnableVersioning = sourceList.EnableVersioning;
            this.newList.Update();
        }

        private void CopyEventReceivers(SPList sourceList) {
            foreach (SPEventReceiverDefinition er in sourceList.EventReceivers) {
                this.newList.EventReceivers.Add(er.Type, er.Assembly, er.Class);  
            }
        }

        private void CopyOptionsNextItemId(SPList list){
            int id = db.GetListNextItemId(list.ID);
             if (id > 0) {
                 db.SetListNextItemId(id, newList.ID);
             }
             else {
                 logger.Error("Error in the method CopyOptionsNextItemId! Next item not set!"); 
             }
        }

        private void RestoreListElements(SPListItem item) {
            var copyItems = new CopyItems(item, this.newList, web);
            var listItem = copyItems.CopyItemInNewList();  
        }

        private SPListItemCollection getAllListItems(SPList list){
            var query = new SPQuery();
            query.ViewXml = "<Where><OrderBy><FieldRef Name='ID' /></OrderBy></Where>";
            return list.GetItems(query);    
        }
    }
}
