using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SAP_API.Models {
    public class Permission {
        public class PermissionsOutput {
            public string Label { get; set; }
            public string Value { get; set; }
        }

        public static class Warehouses {
            public const string Label = "Almacenes";
            public const string View = "Permissions.Warehouses.View";
            public const string Create = "Permissions.Warehouses.Create";
            public const string Edit = "Permissions.Warehouses.Edit";
        }

        public static class Users {
            public const string Label = "Usuarios";
            public const string View = "Permissions.Users.View";
            public const string Create = "Permissions.Users.Create";
            public const string Edit = "Permissions.Users.Edit";
        }

        public static List<PermissionsOutput> Get() {
            
            Type thisClass = (typeof(Permission));
            Type[] myTypeArray = thisClass.GetNestedTypes(BindingFlags.Public);
            List<PermissionsOutput> resultList = new List<PermissionsOutput>();
            foreach (Type cl in myTypeArray) {
                if (cl.Name == "PermissionsOutput") {
                    continue;
                }
                FieldInfo[] fields = cl.GetFields();
                FieldInfo fieldLabel = cl.GetField("Label");
                string ob = fieldLabel.GetValue(null).ToString();
                foreach (FieldInfo fi in fields) {
                    if (fi.Name != "Label") {
                        string action = "";
                        if (fi.Name == "View") {
                            action = "Ver ";
                        } else if (fi.Name == "Edit") {
                            action = "Editar ";
                        } else if (fi.Name == "Create") {
                            action = "Crear ";
                        }
                        resultList.Add(new PermissionsOutput { Label = action + ob, Value = fi.GetValue(null).ToString() });
                    }
                }
            }
            return resultList;
        }

        public static List<PermissionsOutput> Get(List<string> PermissionValueList) {
            Type thisClass = (typeof(Permission));
            List<PermissionsOutput> resultList = new List<PermissionsOutput>();

            foreach (string PermissionValue in PermissionValueList) {
                var a = PermissionValue.Split('.');

                var classType = thisClass.GetNestedType(a[1]);

                FieldInfo fieldLabel = classType.GetField("Label");
                string ob = fieldLabel.GetValue(null).ToString();

                string action = "";
                if (a[2] == "View") {
                    action = "Ver ";
                } else if (a[2] == "Edit") {
                    action = "Editar ";
                } else if (a[2] == "Create") {
                    action = "Crear ";
                }
                resultList.Add(new PermissionsOutput { Label = action + ob, Value = PermissionValue });

            }
            return resultList;
        }
    }

    public class CustomClaimTypes {
        public const string Permission = "permission";
    }

    internal class PermissionRequirement : IAuthorizationRequirement {
        public string Permission { get; private set; }

        public PermissionRequirement(string permission) {
            Permission = permission;
        }
    }

    // Permission Handler. Check Permissions in the user role.
    internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement> {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PermissionAuthorizationHandler(UserManager<User> userManager, RoleManager<IdentityRole> roleManager) {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement) {
            
            if (context.User == null) {
                return;
            }

            // Get all the roles the user belongs to and check if any of the roles has the permission required
            // for the authorization to succeed.
            var user = await _userManager.GetUserAsync(context.User);

            if (user == null) {
                return;
            }
            var userRoleNames = await _userManager.GetRolesAsync(user);
            var userRoles = _roleManager.Roles.Where(x => userRoleNames.Contains(x.Name));

            foreach (var role in userRoles) {
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                var permissions = roleClaims.Where(x => x.Type == CustomClaimTypes.Permission &&
                                                        x.Value == requirement.Permission &&
                                                        x.Issuer == "LOCAL AUTHORITY")
                                            .Select(x => x.Value);

                if (permissions.Any()) {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }

}
