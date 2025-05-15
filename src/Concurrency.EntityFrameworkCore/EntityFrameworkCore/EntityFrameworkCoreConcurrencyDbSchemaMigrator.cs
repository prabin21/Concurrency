using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Concurrency.Data;
using Volo.Abp.DependencyInjection;

namespace Concurrency.EntityFrameworkCore;

public class EntityFrameworkCoreConcurrencyDbSchemaMigrator
    : IConcurrencyDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreConcurrencyDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the ConcurrencyDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<ConcurrencyDbContext>()
            .Database
            .MigrateAsync();
    }
}
