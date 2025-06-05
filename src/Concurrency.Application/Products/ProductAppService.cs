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
using System.Linq;

namespace Concurrency.Products;

public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly ILogger<ProductAppService> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Product, Guid> _productRepository;

    public ProductAppService(
        ILogger<ProductAppService> logger,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Product, Guid> productRepository)
    {
        _logger = logger;
        _unitOfWorkManager = unitOfWorkManager;
        _productRepository = productRepository;

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

    public async Task<List<ProductDto>> GetProductsWithReadUncommittedAsync()
    {
        var options = new AbpUnitOfWorkOptions
        {
            IsolationLevel = IsolationLevel.ReadUncommitted,
            IsTransactional = true
        };

        using (var uow = _unitOfWorkManager.Begin(options))
        {
            var products = await _productRepository.GetListAsync();
            await uow.CompleteAsync();
            return ObjectMapper.Map<List<Product>, List<ProductDto>>(products);
        }
    }

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
                var product = await _productRepository.GetAsync(id);
                ObjectMapper.Map(input, product);
                await _productRepository.UpdateAsync(product);
                await uow.CompleteAsync();
                return ObjectMapper.Map<Product, ProductDto>(product);
            }
        });
    }

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
                var product = await _productRepository.GetAsync(id);
                product.StockQuantity = newQuantity;
                await _productRepository.UpdateAsync(product);
                await uow.CompleteAsync();
                return ObjectMapper.Map<Product, ProductDto>(product);
            }
        });
    }

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
                var product = await _productRepository.GetAsync(id);
                product.Price = newPrice;
                await _productRepository.UpdateAsync(product);
                await uow.CompleteAsync();
                return ObjectMapper.Map<Product, ProductDto>(product);
            }
        });
    }

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
                var product = await _productRepository.GetAsync(id);
                ObjectMapper.Map(input, product);
                await _productRepository.UpdateAsync(product);
                await uow.CompleteAsync();
                return ObjectMapper.Map<Product, ProductDto>(product);
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
                var product = await _productRepository.GetAsync(id);
                product.StockQuantity = newQuantity;
                await _productRepository.UpdateAsync(product);
                await uow.CompleteAsync();
                return ObjectMapper.Map<Product, ProductDto>(product);
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
                var product = await _productRepository.GetAsync(id);
                product.Price = newPrice;
                await _productRepository.UpdateAsync(product);
                await uow.CompleteAsync();
                return ObjectMapper.Map<Product, ProductDto>(product);
            }
        });
    }
} 