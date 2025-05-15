using Xunit;

namespace Concurrency.EntityFrameworkCore;

[CollectionDefinition(ConcurrencyTestConsts.CollectionDefinitionName)]
public class ConcurrencyEntityFrameworkCoreCollection : ICollectionFixture<ConcurrencyEntityFrameworkCoreFixture>
{

}
