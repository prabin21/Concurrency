using Volo.Abp.Settings;

namespace Concurrency.Settings;

public class ConcurrencySettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(ConcurrencySettings.MySetting1));
    }
}
