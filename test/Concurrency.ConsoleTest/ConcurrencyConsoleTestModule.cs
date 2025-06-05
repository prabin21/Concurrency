using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Concurrency.ConsoleTest;

[DependsOn(
    typeof(ConcurrencyApplicationModule),
    typeof(ConcurrencyEntityFrameworkCoreModule)
)]
public class ConcurrencyConsoleTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAlwaysDisableUnitOfWorkTransaction();
    }
} 