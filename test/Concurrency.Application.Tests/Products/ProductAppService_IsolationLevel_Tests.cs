using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shouldly;
using System.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using Volo.Abp.Data;

namespace Concurrency.Products;

public class ProductAppService_IsolationLevel_Tests : ConcurrencyApplicationTestBase
{
    private readonly IProductAppService _productAppService;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ITestOutputHelper _testOutputHelper;

    public ProductAppService_IsolationLevel_Tests(ITestOutputHelper testOutputHelper)
    {
        _productAppService = GetRequiredService<IProductAppService>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        _testOutputHelper = testOutputHelper;
    }

    private async Task<ProductDto> CreateTestProduct()
    {
        var input = new CreateUpdateProductDto
        {
            Name = "Test Product",
            Price = 100,
            StockQuantity = 50
        };

        return await _productAppService.CreateAsync(input);
    }

    [Fact]
    public async Task Should_Handle_ReadUncommitted_IsolationLevel()
    {
        // Arrange
        var product = await CreateTestProduct();
        var initialPrice = product.Price;

        // Act - Start a transaction that updates the price but doesn't commit
        var updateTask = Task.Run(async () =>
        {
            var options = new AbpUnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.ReadUncommitted,
                IsTransactional = true
            };

            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var productToUpdate = await _productRepository.GetAsync(product.Id);
                productToUpdate.UpdatePrice(200);
                await _productRepository.UpdateAsync(productToUpdate);
                // Don't complete the transaction to test ReadUncommitted
                await Task.Delay(1000); // Give time for the read to occur
            }
        });

        // Act - Read with ReadUncommitted isolation level
        var products = await _productAppService.GetProductsWithReadUncommittedAsync();
        var readProduct = products.First(p => p.Id == product.Id);

        // Assert - Should see the uncommitted change
        readProduct.Price.ShouldBe(200);

        // Wait for the update task to complete
        await updateTask;

        // Verify the actual product wasn't updated (since the transaction wasn't committed)
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.Price.ShouldBe(initialPrice);
    }

    [Fact]
    public async Task Should_Handle_Serializable_IsolationLevel()
    {
        // Arrange
        var product = await CreateTestProduct();
        var initialStock = product.StockQuantity;

        // Act - Start two concurrent transactions with Serializable isolation
        var updateTask1 = Task.Run(async () =>
        {
            var options = new AbpUnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                IsTransactional = true
            };

            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var productToUpdate = await _productRepository.GetAsync(product.Id);
                productToUpdate.UpdateStock(initialStock + 10);
                await _productRepository.UpdateAsync(productToUpdate);
                await uow.CompleteAsync();
            }
        });

        var updateTask2 = Task.Run(async () =>
        {
            var options = new AbpUnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                IsTransactional = true
            };

            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var productToUpdate = await _productRepository.GetAsync(product.Id);
                productToUpdate.UpdateStock(initialStock + 20);
                await _productRepository.UpdateAsync(productToUpdate);
                await uow.CompleteAsync();
            }
        });

        // Assert - One transaction should succeed, the other should fail due to serialization
        await Should.ThrowAsync<AbpDbConcurrencyException>(async () =>
        {
            await Task.WhenAll(updateTask1, updateTask2);
        });
    }

    [Fact]
    public async Task Should_Handle_RepeatableRead_IsolationLevel()
    {
        // Arrange
        var product = await CreateTestProduct();
        var initialStock = product.StockQuantity;

        // Act - Start a transaction that reads the product twice
        var options = new AbpUnitOfWorkOptions
        {
            IsolationLevel = IsolationLevel.RepeatableRead,
            IsTransactional = true
        };

        using (var uow = _unitOfWorkManager.Begin(options))
        {
            // First read
            var product1 = await _productRepository.GetAsync(product.Id);
            product1.StockQuantity.ShouldBe(initialStock);

            // Update the product in a different transaction
            await _productAppService.UpdateStockAsync(product.Id, initialStock + 10, IsolationLevel.ReadCommitted);

            // Second read in the same transaction
            var product2 = await _productRepository.GetAsync(product.Id);
            
            // Assert - Should see the same value as the first read (RepeatableRead)
            product2.StockQuantity.ShouldBe(initialStock);
        }

        // Verify the actual update happened
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.StockQuantity.ShouldBe(initialStock + 10);
    }

    [Fact]
    public async Task Should_Handle_Snapshot_IsolationLevel()
    {
        // Arrange
        var product = await CreateTestProduct();
        var initialPrice = product.Price;

        // Act - Start a transaction that reads the product
        var options = new AbpUnitOfWorkOptions
        {
            IsolationLevel = IsolationLevel.Snapshot,
            IsTransactional = true
        };

        using (var uow = _unitOfWorkManager.Begin(options))
        {
            // First read
            var product1 = await _productRepository.GetAsync(product.Id);
            product1.Price.ShouldBe(initialPrice);

            // Update the product in a different transaction
            await _productAppService.UpdatePriceAsync(product.Id, initialPrice + 50, IsolationLevel.ReadCommitted);

            // Second read in the same transaction
            var product2 = await _productRepository.GetAsync(product.Id);
            
            // Assert - Should see the same value as the first read (Snapshot)
            product2.Price.ShouldBe(initialPrice);

            // Try to update in the snapshot transaction
            product2.UpdatePrice(initialPrice + 100);
            await _productRepository.UpdateAsync(product2);
            
            // This should fail due to snapshot isolation
            await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
            {
                await uow.CompleteAsync();
            });
        }

        // Verify the actual update happened
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.Price.ShouldBe(initialPrice + 50);
    }

    [Fact]
    public async Task Should_Handle_UpdateWithIsolationAsync()
    {
        // Arrange
        var product = await CreateTestProduct();
        var initialPrice = product.Price;
        var initialStock = product.StockQuantity;

        // Act - Update with ReadCommitted isolation level
        var updateInput = new CreateUpdateProductDto
        {
            Name = product.Name,
            Price = initialPrice + 50,
            StockQuantity = initialStock + 20
        };

        var updatedProduct = await _productAppService.UpdateWithIsolationAsync(
            product.Id, 
            updateInput, 
            IsolationLevel.ReadCommitted);

        // Assert
        updatedProduct.ShouldNotBeNull();
        updatedProduct.Price.ShouldBe(initialPrice + 50);
        updatedProduct.StockQuantity.ShouldBe(initialStock + 20);

        // Act - Try concurrent update with Serializable isolation
        var concurrentUpdateTask = Task.Run(async () =>
        {
            var options = new AbpUnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                IsTransactional = true
            };

            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var productToUpdate = await _productRepository.GetAsync(product.Id);
                productToUpdate.UpdatePrice(initialPrice + 100);
                await _productRepository.UpdateAsync(productToUpdate);
                await uow.CompleteAsync();
            }
        });

        // This should fail due to serialization
        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
        {
            await concurrentUpdateTask;
        });

        // Verify the original update is preserved
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.Price.ShouldBe(initialPrice + 50);
        finalProduct.StockQuantity.ShouldBe(initialStock + 20);
    }

    [Fact]
    public async Task Should_Handle_ReadCommitted_IsolationLevel()
    {
        // Arrange
        var product = await CreateTestProduct();
        var initialPrice = product.Price;
        var initialStock = product.StockQuantity;

        // Act - Start a transaction that reads and updates the product
        var readCommittedTask = Task.Run(async () =>
        {
            var options = new AbpUnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                IsTransactional = true
            };

            using (var uow = _unitOfWorkManager.Begin(options))
            {
                // First read
                var productToUpdate = await _productRepository.GetAsync(product.Id);
                productToUpdate.Price.ShouldBe(initialPrice);

                // Simulate some work

                // Second read - should see committed changes from other transactions
                var updatedProduct = await _productRepository.GetAsync(product.Id);
                
                // Update based on the latest read
                updatedProduct.UpdatePrice(initialPrice + 50);
                await _productRepository.UpdateAsync(updatedProduct);
                await Task.Delay(1000);

                await uow.CompleteAsync();

            }
        });

        // Act - Start another transaction that updates the product
        var concurrentUpdateTask = Task.Run(async () =>
        {
            var options = new AbpUnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                IsTransactional = true
            };

            using (var uow = _unitOfWorkManager.Begin(options))
            {
                var productToUpdate = await _productRepository.GetAsync(product.Id);
                productToUpdate.UpdatePrice(initialPrice + 100);
                await _productRepository.UpdateAsync(productToUpdate);
                await uow.CompleteAsync();
            }
        });

        // Wait for both transactions to complete
        await Task.WhenAll(readCommittedTask, concurrentUpdateTask);

        // Assert - Verify the final state
        var finalProduct = await _productAppService.GetAsync(product.Id);
        
        // The final price should be either initialPrice + 50 or initialPrice + 100
        // depending on which transaction committed last
        finalProduct.Price.ShouldBeOneOf(initialPrice, 150);
        
        // Verify that the stock quantity remains unchanged
        finalProduct.StockQuantity.ShouldBe(initialStock);
    }
} 