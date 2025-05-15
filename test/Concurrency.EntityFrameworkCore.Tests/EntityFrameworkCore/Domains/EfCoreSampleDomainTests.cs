using Concurrency.Samples;
using Xunit;

namespace Concurrency.EntityFrameworkCore.Domains;

[Collection(ConcurrencyTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<ConcurrencyEntityFrameworkCoreTestModule>
{

}
