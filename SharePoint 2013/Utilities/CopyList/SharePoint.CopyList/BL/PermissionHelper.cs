using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePoint.CopyList.BL {
    class PermissionHelper {

        public static void CopyWebRoles(SPWeb sourceWeb, SPWeb destinationWeb) {

            //First copy Source Web Role Definitions to the Destination Web
            foreach (SPRoleDefinition roleDef in sourceWeb.RoleDefinitions) {
                //Skip WSS base permission levels
                if (roleDef.Type != SPRoleType.Administrator
                    && roleDef.Type != SPRoleType.Contributor
                    && roleDef.Type != SPRoleType.Guest
                    && roleDef.Type != SPRoleType.Reader
                    && roleDef.Type != SPRoleType.WebDesigner
                    ) {
                    //handle additon of existing  permission level error
                    try { destinationWeb.RoleDefinitions.Add(roleDef); }
                    catch (SPException) { }
                }
            }


        }


        public static void CopyWebRoleAssignments(SPWeb sourceWeb, SPWeb destinationWeb) {

            //Copy Role Assignments from source to destination web.
            foreach (SPRoleAssignment sourceRoleAsg in sourceWeb.RoleAssignments) {
                SPRoleAssignment destinationRoleAsg = null;

                //Get the source member object
                SPPrincipal member = sourceRoleAsg.Member;

                //Check if the member is a user 
                try {
                    SPUser sourceUser = (SPUser)member;
                    SPUser destinationUser = destinationWeb.AllUsers[sourceUser.LoginName];
                    if (destinationUser != null) {
                        destinationRoleAsg = new SPRoleAssignment(destinationUser);
                    }
                }
                catch { }

                if (destinationRoleAsg == null) {
                    //Check if the member is a group
                    try {
                        SPGroup sourceGroup = (SPGroup)member;
                        SPGroup destinationGroup = destinationWeb.SiteGroups[sourceGroup.Name];
                        destinationRoleAsg = new SPRoleAssignment(destinationGroup);
                    }
                    catch { }
                }

                //At this state we should have the role assignment established either by user or group
                if (destinationRoleAsg != null) {

                    foreach (SPRoleDefinition sourceRoleDefinition in sourceRoleAsg.RoleDefinitionBindings) {
                        try { destinationRoleAsg.RoleDefinitionBindings.Add(destinationWeb.RoleDefinitions[sourceRoleDefinition.Name]); }
                        catch { }
                    }

                    if (destinationRoleAsg.RoleDefinitionBindings.Count > 0) {
                        //handle additon of an existing  permission assignment error
                        try { destinationWeb.RoleAssignments.Add(destinationRoleAsg); }
                        catch (ArgumentException) { }
                    }

                }

            }

            //Finally update the destination web
            destinationWeb.Update();

        }


        public static void CopyListRoleAssignments(SPList sourceList, SPList destinationList) {
            //First check if the Source List has Unique permissions
            if (sourceList.HasUniqueRoleAssignments) {

                //Break List permission inheritance first
                destinationList.BreakRoleInheritance(true);

                //Remove current role assignemnts
                while (destinationList.RoleAssignments.Count > 0) {
                    destinationList.RoleAssignments.Remove(0);
                }


                //Copy Role Assignments from source to destination list.
                foreach (SPRoleAssignment sourceRoleAsg in sourceList.RoleAssignments) {
                    SPRoleAssignment destinationRoleAsg = null;

                    //Get the source member object
                    SPPrincipal member = sourceRoleAsg.Member;

                    //Check if the member is a user 
                    try {
                        SPUser sourceUser = (SPUser)member;
                        SPUser destinationUser = destinationList.ParentWeb.Users.GetByEmail(sourceUser.Email);
                        destinationRoleAsg = new SPRoleAssignment(destinationUser);
                    }
                    catch { }

                    if (destinationRoleAsg == null) {
                        //Check if the member is a group
                        try {
                            SPGroup sourceGroup = (SPGroup)member;
                            SPGroup destinationGroup = destinationList.ParentWeb.SiteGroups[sourceGroup.Name];
                            destinationRoleAsg = new SPRoleAssignment(destinationGroup);
                        }
                        catch { }
                    }

                    //At this state we should have the role assignment established either by user or group
                    if (destinationRoleAsg != null) {

                        foreach (SPRoleDefinition sourceRoleDefinition in sourceRoleAsg.RoleDefinitionBindings) {
                            try { destinationRoleAsg.RoleDefinitionBindings.Add(destinationList.ParentWeb.RoleDefinitions[sourceRoleDefinition.Name]); }
                            catch { }
                        }

                        if (destinationRoleAsg.RoleDefinitionBindings.Count > 0) {
                            //handle additon of an existing  permission assignment error
                            try { destinationList.RoleAssignments.Add(destinationRoleAsg); }
                            catch (ArgumentException) { }
                        }

                    }

                }

                //Does not require list update
                //destinationList.Update();
            }
            else
                //No need to assign permissions
                return;



        }

        public static void CopyListItemsRoleAssignments(SPList sourceList, SPList destinationList) {
            foreach (SPListItem sourceListitem in sourceList.Items) {
                CopyListItemRoleAssignments(sourceListitem, destinationList.GetItemById(sourceListitem.ID));
            }

        }
        public static void CopyListItemRoleAssignments(SPListItem sourceListItem, SPListItem destinationListItem) {
            //First check if the Source List has Unique permissions
            if (sourceListItem.HasUniqueRoleAssignments) {

                //Break List permission inheritance first
                destinationListItem.BreakRoleInheritance(true);
                destinationListItem.Update();

                //Remove current role assignemnts
                while (destinationListItem.RoleAssignments.Count > 0) {
                    destinationListItem.RoleAssignments.Remove(0);
                }
                destinationListItem.Update();

                //Copy Role Assignments from source to destination list.
                foreach (SPRoleAssignment sourceRoleAsg in sourceListItem.RoleAssignments) {
                    SPRoleAssignment destinationRoleAsg = null;

                    //Get the source member object
                    SPPrincipal member = sourceRoleAsg.Member;

                    //Check if the member is a user 
                    try {
                        SPUser sourceUser = (SPUser)member;
                        SPUser destinationUser = destinationListItem.ParentList.ParentWeb.AllUsers[sourceUser.LoginName];
                        if (destinationUser != null) {
                            destinationRoleAsg = new SPRoleAssignment(destinationUser);
                        }
                    }
                    catch { }

                    //Not a user, try check if the member is a Group
                    if (destinationRoleAsg == null) {
                        //Check if the member is a group
                        try {
                            SPGroup sourceGroup = (SPGroup)member;
                            SPGroup destinationGroup = destinationListItem.ParentList.ParentWeb.SiteGroups[sourceGroup.Name];
                            if (destinationGroup != null) {
                                destinationRoleAsg = new SPRoleAssignment(destinationGroup);
                            }
                        }
                        catch { }
                    }

                    //At this state we should have the role assignment established either by user or group
                    if (destinationRoleAsg != null) {

                        foreach (SPRoleDefinition sourceRoleDefinition in sourceRoleAsg.RoleDefinitionBindings) {
                            try { destinationRoleAsg.RoleDefinitionBindings.Add(destinationListItem.ParentList.ParentWeb.RoleDefinitions[sourceRoleDefinition.Name]); }
                            catch { }
                        }

                        if (destinationRoleAsg.RoleDefinitionBindings.Count > 0) {
                            //handle additon of an existing  permission assignment error
                            try { destinationListItem.RoleAssignments.Add(destinationRoleAsg); }
                            catch (ArgumentException) { }
                        }

                    }

                }

                //Ensure item update metadata is not affected.
                destinationListItem.SystemUpdate(false);
            }
            else
                //No need to assign permissions
                return;



        }
    }
}
