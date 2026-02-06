using System.Collections.ObjectModel;

namespace Domain.Authorization;

public static class PermissionConstants
{
    public const string PermissionClaimType = "permission";
    public const string PermissionGroupClaimType = "permission_group";

    public static class Permissions
    {
        public const string FullAccess = "full_access";
        public const string UsersView = "users.view";
        public const string UsersEdit = "users.edit";
        public const string UsersDelete = "users.delete";
        public const string UsersExport = "users.export";
        public const string RolesManage = "roles.manage";
        public const string PermissionsManage = "permissions.manage";
    }

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Permissions.FullAccess,
        Permissions.UsersView,
        Permissions.UsersEdit,
        Permissions.UsersDelete,
        Permissions.UsersExport,
        Permissions.RolesManage,
        Permissions.PermissionsManage
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> Groups =
        new ReadOnlyDictionary<string, IReadOnlyCollection<string>>(
            new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["UserManagement"] =
                [
                    Permissions.UsersView,
                    Permissions.UsersEdit,
                    Permissions.UsersDelete,
                    Permissions.UsersExport
                ],
                ["RoleAdministration"] =
                [
                    Permissions.RolesManage,
                    Permissions.PermissionsManage
                ],
                ["Reporting"] =
                [
                    Permissions.UsersExport
                ]
            });
}
