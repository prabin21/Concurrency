using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using System.Threading;
using Volo.Abp.Data;
using System.Data;
using System.Collections.Generic;

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
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public ProductAppService(
        IRepository<Product, Guid> repository,
        ILogger<ProductAppService> logger,
        IUnitOfWorkManager unitOfWorkManager) 
        : 
        base(repository)
    {
        _logger = logger;
        _unitOfWorkManager = unitOfWorkManager;

        // Configure retry policy for concurrency conflicts
        _retryPolicy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .Or<AbpDbConcurrencyException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Concurrency conflict occurred. Retry {RetryCount} of 3 after {Delay}ms",
                        retryCount,
                        delay.TotalMilliseconds);
                });
    }

    // New method to demonstrate ReadUncommitted isolation level
    public async Task<List<ProductDto>> GetProductsWithReadUncommittedAsync()
    {
        var options = new AbpUnitOfWorkOptions
        {
            IsolationLevel = IsolationLevel.ReadUncommitted,
            IsTransactional = true
        };

        using (var uow = _unitOfWorkManager.Begin(options))
        {
            var products = await Repository.GetListAsync();
            await uow.CompleteAsync();
            return ObjectMapper.Map<List<Product>, List<ProductDto>>(products);
        }
    }

    // New method to demonstrate Serializable isolation level
    public async Task<ProductDto> UpdateProductWithSerializableAsync(Guid id, CreateUpdateProductDto input)
    {
        var options = new AbpUnitOfWorkOptions
        {
            IsolationLevel = IsolationLevel.Serializable,
            IsTransactional = true
        };

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var product = await Repository.GetAsync(id);
                await MapToEntityAsync(input, product);
                await Repository.UpdateAsync(product);
                await uow.CompleteAsync();
                return await MapToGetOutputDtoAsync(product);
            }
        });
    }

    // New method to demonstrate RepeatableRead isolation level
    public async Task<ProductDto> UpdateStockWithRepeatableReadAsync(Guid id, int newQuantity)
    {
        var options = new AbpUnitOfWorkOptions
        {
            IsolationLevel = IsolationLevel.RepeatableRead,
            IsTransactional = true
        };

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var product = await Repository.GetAsync(id);
                product.UpdateStock(newQuantity);
                await Repository.UpdateAsync(product);
                await uow.CompleteAsync();
                return await MapToGetOutputDtoAsync(product);
            }
        });
    }

    // New method to demonstrate Snapshot isolation level
    public async Task<ProductDto> UpdatePriceWithSnapshotAsync(Guid id, decimal newPrice)
    {
        var options = new AbpUnitOfWorkOptions
        {
            IsolationLevel = IsolationLevel.Snapshot,
            IsTransactional = true
        };

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var product = await Repository.GetAsync(id);
                product.UpdatePrice(newPrice);
                await Repository.UpdateAsync(product);
                await uow.CompleteAsync();
                return await MapToGetOutputDtoAsync(product);
            }
        });
    }

    // Modified existing methods to support isolation level
    [UnitOfWork]
    public async Task<ProductDto> UpdateWithIsolationAsync(Guid id, CreateUpdateProductDto input, IsolationLevel? isolationLevel = null)
    {
        var options = new AbpUnitOfWorkOptions
        {
            IsTransactional = true
        };
        
        if (isolationLevel.HasValue)
        {
            options.IsolationLevel = isolationLevel.Value;
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var product = await Repository.GetAsync(id);
                await MapToEntityAsync(input, product);
                await Repository.UpdateAsync(product);
                await uow.CompleteAsync();
                return await MapToGetOutputDtoAsync(product);
            }
        });
    }

    [UnitOfWork]
    public async Task<ProductDto> UpdateStockAsync(Guid id, int newQuantity, IsolationLevel? isolationLevel = null)
    {
        var options = new AbpUnitOfWorkOptions
        {
            IsTransactional = true
        };
        
        if (isolationLevel.HasValue)
        {
            options.IsolationLevel = isolationLevel.Value;
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var product = await Repository.GetAsync(id);
                product.UpdateStock(newQuantity);
                await Repository.UpdateAsync(product);
                await uow.CompleteAsync();
                return await MapToGetOutputDtoAsync(product);
            }
        });
    }

    [UnitOfWork]
    public async Task<ProductDto> UpdatePriceAsync(Guid id, decimal newPrice, IsolationLevel? isolationLevel = null)
    {
        var options = new AbpUnitOfWorkOptions
        {
            IsTransactional = true
        };
        
        if (isolationLevel.HasValue)
        {
            options.IsolationLevel = isolationLevel.Value;
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var product = await Repository.GetAsync(id);
                product.UpdatePrice(newPrice);
                await Repository.UpdateAsync(product);
                await uow.CompleteAsync();
                return await MapToGetOutputDtoAsync(product);
            }
        });
    }

    protected override async Task<Product> MapToEntityAsync(CreateUpdateProductDto createInput)
    {
        var product = await base.MapToEntityAsync(createInput);
        return product;
    }

    protected override async Task MapToEntityAsync(CreateUpdateProductDto updateInput, Product entity)
    {
        await base.MapToEntityAsync(updateInput, entity);
    }
} 