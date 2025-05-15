using Concurrency.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Concurrency.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(ConcurrencyEntityFrameworkCoreModule),
    typeof(ConcurrencyApplicationContractsModule)
    )]
public class ConcurrencyDbMigratorModule : AbpModule
{
}
