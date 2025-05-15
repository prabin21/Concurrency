using Volo.Abp.Modularity;

namespace Concurrency;

/* Inherit from this class for your domain layer tests. */
public abstract class ConcurrencyDomainTestBase<TStartupModule> : ConcurrencyTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
