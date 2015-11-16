using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharePoint.CopyList.BL;

namespace SharePoint.CopyList.CopyListOrders.BL
{
    public class CopyItems
    {
        SPListItem SourceItem { get; set; } 
        SPList DestinationList { get; set; }
        SPWeb Web { get; set; }

        public CopyItems(SPListItem sourceItem, SPList destinationList, SPWeb web)
        {
            SourceItem = sourceItem;
            DestinationList = destinationList;
            Web = web;
        }

        public SPListItem CopyItemInNewList()
        {
            SPListItem copiedItem = null;
            try
            {
                copiedItem = CopyItem(SourceItem, DestinationList.RootFolder);

                PermissionHelper.CopyListItemRoleAssignments(SourceItem, copiedItem); 
            }
            catch (Exception e)
            {
            }
            return copiedItem;             
        }

        public SPListItem CopyItem(SPListItem sourceItem, SPFolder destinationFolder)
        {
            SPListItem copiedItem = null;
            try
            {
                var destinationList = destinationFolder.ParentWeb.Lists[destinationFolder.ParentListId];

                if (destinationFolder.Item == null)
                {
                    copiedItem = destinationList.Items.Add();
                }
                else
                {
                    copiedItem = destinationFolder.Item.ListItems.Add(
                            destinationFolder.ServerRelativeUrl,
                            sourceItem.FileSystemObjectType);
                }         

                foreach (string fileName in sourceItem.Attachments)
                {
                    SPFile file = sourceItem.ParentList.ParentWeb.GetFile(sourceItem.Attachments.UrlPrefix + fileName);
                    byte[] data = file.OpenBinary();
                    copiedItem.Attachments.Add(fileName, data);
                }

                for (int i = sourceItem.Versions.Count - 1; i >= 0; i--)
                {
                    foreach (SPField destinationField in destinationList.Fields)
                    {
                        SPListItemVersion version = sourceItem.Versions[i];
                        
                        if (!version.Fields.ContainsField(destinationField.InternalName))
                            continue;

                        if ((!destinationField.ReadOnlyField) && (destinationField.Type != SPFieldType.Attachments) && !destinationField.Hidden
                            || destinationField.Id == SPBuiltInFieldId.Author || destinationField.Id == SPBuiltInFieldId.Created
                            || destinationField.Id == SPBuiltInFieldId.Editor || destinationField.Id == SPBuiltInFieldId.Modified)
                        {
                            if (destinationField.Type == SPFieldType.DateTime)
                            {
                                var dtObj = version[destinationField.InternalName];
                                copiedItem[destinationField.InternalName] = dtObj != null ?
                                    (object)destinationList.ParentWeb.RegionalSettings.TimeZone.UTCToLocalTime((DateTime)dtObj) : null;
                            }
                            else copiedItem[destinationField.Id] = version[destinationField.InternalName];
                        }
                    }
                    copiedItem.Update();
                }
                copiedItem[SPBuiltInFieldId.ContentTypeId] = sourceItem[SPBuiltInFieldId.ContentTypeId];
                copiedItem.SystemUpdate(false);                                                            
            }
            catch (Exception e)
            {}
            return copiedItem;
        } 
    }
}
