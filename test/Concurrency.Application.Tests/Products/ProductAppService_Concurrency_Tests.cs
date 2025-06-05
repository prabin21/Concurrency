using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Concurrency.Products;

public class ProductAppService_Concurrency_Tests : ConcurrencyApplicationTestBase
{
    private readonly IProductAppService _productAppService;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ITestOutputHelper _testOutputHelper;

    public ProductAppService_Concurrency_Tests(ITestOutputHelper testOutputHelper)
    {
        _productAppService = GetRequiredService<IProductAppService>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Updates_Successfully()
    {
        // Arrange
        var product = await CreateTestProduct();

        // Act - Simulate concurrent updates
        var updateTasks = new List<Task<ProductDto>>
        {
            UpdateProductStock(product.Id, 40),
            UpdateProductPrice(product.Id, 120),
            UpdateProductStock(product.Id, 30),
            UpdateProductPrice(product.Id, 150)
        };

        var results = await Task.WhenAll(updateTasks);

        // Assert
        results.ShouldAllBe(r => r != null);
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.StockQuantity.ShouldBe(30); // Last stock update
        finalProduct.Price.ShouldBe(150); // Last price update
    }

    [Fact]
    public async Task Should_Retry_On_Concurrency_Conflict()
    {
        // Arrange
        var product = await CreateTestProduct();
        var staleProduct = await _productRepository.GetAsync(product.Id);

        // Act - Update through service first
        await _productAppService.UpdatePriceAsync(product.Id, 200);

        // Try to update using stale product
        var exception = await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
        {
            using var uow = _unitOfWorkManager.Begin();
            staleProduct.UpdatePrice(300);
            await _productRepository.UpdateAsync(staleProduct);
            await uow.CompleteAsync();
        });

        // Assert
        exception.ShouldNotBeNull();
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.Price.ShouldBe(200); // First update should be preserved
    }

    [Fact]
    public async Task Should_Handle_Multiple_Concurrent_Updates_With_Retry()
    {
        // Arrange
        var product = await CreateTestProduct();
        var updateCount = 0;
        var exceptions = new List<Exception>();

        // Act - Simulate multiple concurrent updates with delays
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            try
            {
                await Task.Delay(i * 100); // Stagger the updates
                var result = await _productAppService.UpdateStockAsync(product.Id, 50 - i);
                updateCount++;
                return result;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                throw;
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(r => r != null);
        updateCount.ShouldBe(5); // All updates should succeed
        exceptions.ShouldBeEmpty(); // No exceptions should be thrown
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.StockQuantity.ShouldBe(45); // Last update (50 - 5)
    }

    [Fact]
    public async Task Should_Maintain_Data_Consistency_Under_Concurrent_Load()
    {
        // Arrange
        var product = await CreateTestProduct();
        var initialStock = product.StockQuantity;
        var updateCount = 10;
        var successfulUpdates = 0;

        // Act - Simulate high concurrent load
        var tasks = Enumerable.Range(1, updateCount).Select(async i =>
        {
            try
            {
                var newStock = initialStock - i;
                await _productAppService.UpdateStockAsync(product.Id, newStock);
                successfulUpdates++;
                _testOutputHelper.WriteLine($"Update {i} succeeded: Stock = {newStock}");
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine($"Update {i} failed: {ex.Message}");
                throw;
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        successfulUpdates.ShouldBe(updateCount);
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.StockQuantity.ShouldBe(initialStock - updateCount);
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Create_And_Update()
    {
        // Arrange
        var product = await CreateTestProduct();
        var createAndUpdateTasks = new List<Task>();

        // Act - Simulate concurrent create and update operations
        for (int i = 0; i < 3; i++)
        {
            createAndUpdateTasks.Add(Task.Run(async () =>
            {
                // Create new product
                var newProduct = await _productAppService.CreateAsync(new CreateUpdateProductDto
                {
                    Name = $"Product {i}",
                    Price = 100 + i,
                    StockQuantity = 50 + i
                });

                // Update original product
                await _productAppService.UpdatePriceAsync(product.Id, 200 + i);
            }));
        }

        await Task.WhenAll(createAndUpdateTasks);

        // Assert
        var finalProduct = await _productAppService.GetAsync(product.Id);
        finalProduct.Price.ShouldBe(202); // Last price update (200 + 2)

        var allProducts = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 10 });
        allProducts.TotalCount.ShouldBe(4); // Original + 3 new products
    }

    private async Task<ProductDto> CreateTestProduct()
    {
        return await _productAppService.CreateAsync(new CreateUpdateProductDto
        {
            Name = "Test Product",
            Price = 100,
            StockQuantity = 50
        });
    }

    private async Task<ProductDto> UpdateProductStock(Guid productId, int newQuantity)
    {
        return await _productAppService.UpdateStockAsync(productId, newQuantity);
    }

    private async Task<ProductDto> UpdateProductPrice(Guid productId, decimal newPrice)
    {
        return await _productAppService.UpdatePriceAsync(productId, newPrice);
    }
} 