using Concurrency.Samples;
using Xunit;

namespace Concurrency.EntityFrameworkCore.Applications;

[Collection(ConcurrencyTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<ConcurrencyEntityFrameworkCoreTestModule>
{

}
