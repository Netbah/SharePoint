using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharePoint.Tieto.SCRPortal.Core.Constants;
using SharePoint.Tieto.SCRPortal.Core.ExtensionMethods;
using SharePoint.Tieto.SCRPortal.Core.Data.Entities;
using SharePoint.Tieto.SCRPortal.Core.Data.Repositories;
using SharePoint.Tieto.SCRPortal.Core.Data.Repositories.Interfaces;
using SharePoint.Tieto.SCRPortal.Core.Helpers;
using SharePoint.Tieto.SCRPortal.Core.Security;

namespace SharePoint.Tieto.SCRPortal.CopyListOrders.BL
{
    public class ArchiveOrders
    {
        SPListItem SourceItem { get; set; } 
        SPList DestinationList { get; set; }
        SPWeb Web { get; set; }

        public ArchiveOrders(SPListItem sourceItem, SPList destinationList, SPWeb web)
        {
            SourceItem = sourceItem;
            DestinationList = destinationList;
            Web = web;
        }

        public SPListItem CopyItemToArchive()
        {
            SPListItem copiedItem = null;
            try
            {
                copiedItem = CopyItem(SourceItem, DestinationList.RootFolder);

                if (copiedItem.Fields.ContainsField(OrderCommonFields.WorkflowStatus.InternalName))
                {
                    copiedItem.Update();
                }
                
                if (SourceItem.HasUniqueRoleAssignments) {
                    copiedItem.BreakRoleInheritance(false);
                    foreach (SPRoleAssignment ra in SourceItem.RoleAssignments) {
                        copiedItem.RoleAssignments.Add(ra);
                    }                    
                }
            }
            catch (Exception e)
            {
                e.LogError(Web, "ArchiveOrders : CopyItemToArchive");
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
                            || destinationField.Id == SPBuiltInFieldId.Editor || destinationField.Id == SPBuiltInFieldId.Modified
                            || destinationField.InternalName == OrderCommonFields.TenantOrCompanyName.InternalName)
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
            {
                e.LogError(destinationFolder.ParentWeb, "ArchiveOrders : CopyItem");
            }
            return copiedItem;
        }

        public void AchiveComments(SPListItem archive)
        {
            try
            {
                var commentsRepo = RepositoryLocator.Get<IApprovalCommentsRepository>(Web);

                var publicComments = commentsRepo.GetComments(SourceItem.ID);
                var publicArchiveFolder = commentsRepo.GetFolder(true, true);
                foreach (var comment in publicComments)
                {
                    var archiveComment = CopyItem(comment.Item, publicArchiveFolder);
                    archiveComment[ApprovalCommentFields.OrderId.InternalName] = archive.ID;
                    archiveComment.SystemUpdate(false);
                    comment.Delete();
                }
            
                var privateComments = commentsRepo.GetComments(SourceItem.ID, false, false);
                var privateArchiveFolder = commentsRepo.GetFolder(true, false);
                foreach (var comment in privateComments)
                {
                    var archiveComment = CopyItem(comment.Item, privateArchiveFolder);
                    archiveComment[ApprovalCommentFields.OrderId.InternalName] = archive.ID;
                    archiveComment.SystemUpdate(false);
                    comment.Item.Delete();
                }
            }
            catch (Exception e)
            {
                e.LogError(Web, "ArchiveOrders : AchiveComments");
            }
        }
    }
}
