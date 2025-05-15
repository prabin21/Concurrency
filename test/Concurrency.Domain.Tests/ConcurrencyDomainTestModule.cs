using Volo.Abp.Modularity;

namespace Concurrency;

[DependsOn(
    typeof(ConcurrencyDomainModule),
    typeof(ConcurrencyTestBaseModule)
)]
public class ConcurrencyDomainTestModule : AbpModule
{

}
