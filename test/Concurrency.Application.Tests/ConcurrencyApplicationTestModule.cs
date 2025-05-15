using Volo.Abp.Modularity;

namespace Concurrency;

[DependsOn(
    typeof(ConcurrencyApplicationModule),
    typeof(ConcurrencyDomainTestModule)
)]
public class ConcurrencyApplicationTestModule : AbpModule
{

}
