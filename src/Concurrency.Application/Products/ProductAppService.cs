using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Concurrency.Products;

public class ProductAppService : 
    CrudAppService<
        Product,
        ProductDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateProductDto>,
    IProductAppService
{
    private readonly ILogger<ProductAppService> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public ProductAppService(
        IRepository<Product, Guid> repository,
        ILogger<ProductAppService> logger) 
        : base(repository)
    {
        _logger = logger;

        // Configure retry policy for concurrency conflicts
        _retryPolicy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(
                3, // Number of retries
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Concurrency conflict occurred. Retry {RetryCount} of 3 after {Delay}ms",
                        retryCount,
                        timeSpan.TotalMilliseconds);
                });
    }

    [UnitOfWork]
    public async Task<ProductDto> UpdateStockAsync(Guid id, int newQuantity)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var product = await Repository.GetAsync(id);
            product.UpdateStock(newQuantity);
            await Repository.UpdateAsync(product);
            return await MapToGetOutputDtoAsync(product);
        });
    }

    [UnitOfWork]
    public async Task<ProductDto> UpdatePriceAsync(Guid id, decimal newPrice)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var product = await Repository.GetAsync(id);
            product.UpdatePrice(newPrice);
            await Repository.UpdateAsync(product);
            return await MapToGetOutputDtoAsync(product);
        });
    }

    protected override async Task<Product> MapToEntityAsync(CreateUpdateProductDto createInput)
    {
        var product = await base.MapToEntityAsync(createInput);
        product.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        return product;
    }

    protected override async Task MapToEntityAsync(CreateUpdateProductDto updateInput, Product entity)
    {
        await base.MapToEntityAsync(updateInput, entity);
        entity.ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }
} 