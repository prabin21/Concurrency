using Concurrency.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Concurrency.Permissions;

public class ConcurrencyPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(ConcurrencyPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(ConcurrencyPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ConcurrencyResource>(name);
    }
}
