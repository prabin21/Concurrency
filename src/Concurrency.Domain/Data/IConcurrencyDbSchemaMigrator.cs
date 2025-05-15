using System.Threading.Tasks;

namespace Concurrency.Data;

public interface IConcurrencyDbSchemaMigrator
{
    Task MigrateAsync();
}
