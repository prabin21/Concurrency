using Volo.Abp.Modularity;

namespace Concurrency;

public abstract class ConcurrencyApplicationTestBase<TStartupModule> : ConcurrencyTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
