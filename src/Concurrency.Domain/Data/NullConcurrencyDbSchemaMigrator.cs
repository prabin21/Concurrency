using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Concurrency.Data;

/* This is used if database provider does't define
 * IConcurrencyDbSchemaMigrator implementation.
 */
public class NullConcurrencyDbSchemaMigrator : IConcurrencyDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
