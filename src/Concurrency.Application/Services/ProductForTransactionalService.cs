using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Concurrency.Products;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.Linq;

namespace Concurrency.Application.Services
{
    /// <summary>
    /// Example service demonstrating various Unit of Work patterns using UnitOfWorkManager.
    /// This service shows both transactional and non-transactional operations.
    /// </summary>
    public class ProductForTransactionalService
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly ILogger<ProductForTransactionalService> _logger;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public ProductForTransactionalService(
            IUnitOfWorkManager unitOfWorkManager,
            IRepository<Product, Guid> productRepository,
            ILogger<ProductForTransactionalService> logger,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _productRepository = productRepository;
            _logger = logger;
            _asyncExecuter = asyncExecuter;
        }

        #region Transactional Operations

        /// <summary>
        /// Example 1: Simple transactional operation using UnitOfWorkManager
        /// </summary>
        public async Task<Product> CreateProductAsync(CreateUpdateProductDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var uow = _unitOfWorkManager.Begin())
                {
                    var product = new Product
                    {
                        Name = input.Name,
                        Price = input.Price,
                        StockQuantity = input.StockQuantity
                    };

                    product = await _productRepository.InsertAsync(product, cancellationToken: cancellationToken);
                    await uow.CompleteAsync(cancellationToken);
                    return product;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        /// <summary>
        /// Example 2: Complex transactional operation with multiple steps
        /// </summary>
        public async Task BulkUpdateProductsAsync(List<CreateUpdateProductDto> updates, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var uow = _unitOfWorkManager.Begin())
                {
                    // Validate all updates first
                    foreach (var update in updates)
                    {
                        if (update.Price < 0)
                            throw new InvalidOperationException($"Invalid price for product {update.Name}");
                    }

                    // Perform updates
                    var products = await _productRepository.GetListAsync(cancellationToken: cancellationToken);
                    foreach (var update in updates)
                    {
                        var product = products.FirstOrDefault(p => p.Name == update.Name);
                        if (product != null)
                        {
                            product.Price = update.Price;
                            product.StockQuantity = update.StockQuantity;
                            await _productRepository.UpdateAsync(product, cancellationToken: cancellationToken);
                        }
                    }

                    await uow.CompleteAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk update");
                throw;
            }
        }

        /// <summary>
        /// Example 3: Multi-step transactional operation with nested unit of work
        /// </summary>
        public async Task TransferProductStockAsync(Guid productId, int sourceLocationId, int targetLocationId, int quantity, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var uow = _unitOfWorkManager.Begin())
                {
                    var product = await _productRepository.GetAsync(productId, cancellationToken: cancellationToken);
                    if (product == null)
                        throw new InvalidOperationException($"Product {productId} not found");

                    if (product.StockQuantity < quantity)
                        throw new InvalidOperationException("Insufficient stock");

                    // Update source location
                    product.StockQuantity -= quantity;
                    await _productRepository.UpdateAsync(product, cancellationToken: cancellationToken);

                    // Create target product
                    var targetProduct = new Product
                    {
                        Name = product.Name,
                        Price = product.Price,
                        StockQuantity = quantity
                    };
                    await _productRepository.InsertAsync(targetProduct, cancellationToken: cancellationToken);

                    await uow.CompleteAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in product transfer");
                throw;
            }
        }

        #endregion

        #region Non-Transactional Operations

        /// <summary>
        /// Example 1: Non-transactional bulk price update
        /// Uses explicit non-transactional Unit of Work
        /// Each update is independent and won't roll back others if one fails
        /// </summary>
        public async Task<List<Product>> BulkUpdatePricesNonTransactionalAsync(Dictionary<Guid, decimal> priceUpdates, CancellationToken cancellationToken = default)
        {
            var updatedProducts = new List<Product>();
            using (var uow = _unitOfWorkManager.Begin(isTransactional: false))
            {
                foreach (var update in priceUpdates)
                {
                    try
                    {
                        var product = await _productRepository.GetAsync(update.Key, cancellationToken: cancellationToken);
                        if (product != null)
                        {
                            product.Price = update.Value;
                            updatedProducts.Add(await _productRepository.UpdateAsync(product, cancellationToken: cancellationToken));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to update product {update.Key}");
                        // Continue with other updates even if one fails
                    }
                }
                await uow.CompleteAsync(cancellationToken);
            }
            return updatedProducts;
        }

        /// <summary>
        /// Example 2: Non-transactional bulk product creation
        /// Uses explicit non-transactional Unit of Work
        /// Products are created independently, failures don't affect other creations
        /// </summary>
        public async Task<List<Product>> BulkCreateProductsNonTransactionalAsync(List<CreateUpdateProductDto> inputs, CancellationToken cancellationToken = default)
        {
            var createdProducts = new List<Product>();
            using (var uow = _unitOfWorkManager.Begin(isTransactional: false))
            {
                foreach (var input in inputs)
                {
                    try
                    {
                        var product = new Product
                        {
                            Name = input.Name,
                            Price = input.Price,
                            StockQuantity = input.StockQuantity
                        };

                        createdProducts.Add(await _productRepository.InsertAsync(product, cancellationToken: cancellationToken));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to create product {input.Name}");
                        // Continue with other creations even if one fails
                    }
                }
                await uow.CompleteAsync(cancellationToken);
            }
            return createdProducts;
        }

        /// <summary>
        /// Example 3: Non-transactional stock adjustment
        /// Uses explicit non-transactional Unit of Work
        /// Stock updates are independent and won't roll back others if one fails
        /// </summary>
        public async Task<List<Product>> BulkAdjustStockNonTransactionalAsync(Dictionary<Guid, int> stockAdjustments, CancellationToken cancellationToken = default)
        {
            var updatedProducts = new List<Product>();
            using (var uow = _unitOfWorkManager.Begin(isTransactional: false))
            {
                foreach (var adjustment in stockAdjustments)
                {
                    try
                    {
                        var product = await _productRepository.GetAsync(adjustment.Key, cancellationToken: cancellationToken);
                        if (product != null)
                        {
                            var newStock = product.StockQuantity + adjustment.Value;
                            if (newStock >= 0)
                            {
                                product.StockQuantity = newStock;
                                updatedProducts.Add(await _productRepository.UpdateAsync(product, cancellationToken: cancellationToken));
                            }
                            else
                            {
                                _logger.LogWarning($"Cannot reduce stock below zero for product {product.Name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to adjust stock for product {adjustment.Key}");
                        // Continue with other adjustments even if one fails
                    }
                }
                await uow.CompleteAsync(cancellationToken);
            }
            return updatedProducts;
        }

        #endregion

        #region Read Operations (These are non-transactional by default in ABP)

        /// <summary>
        /// Example 1: Read operation - Get products by price range
        /// Note: Read operations are non-transactional by default in ABP
        /// </summary>
        public async Task<List<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
        {
            return await _productRepository.GetListAsync(
                p => p.Price >= minPrice && p.Price <= maxPrice,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Example 2: Read operation - Get product statistics
        /// Note: Read operations are non-transactional by default in ABP
        /// </summary>
        public async Task<ProductStatistics> GetProductStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var products = await _productRepository.GetListAsync(cancellationToken: cancellationToken);

            return new ProductStatistics
            {
                TotalProducts = products.Count,
                AveragePrice = products.Average(p => p.Price),
                TotalStock = products.Sum(p => p.StockQuantity),
                LowStockProducts = products.Count(p => p.StockQuantity < 10)
            };
        }

        /// <summary>
        /// Example 3: Read operation - Search products with pagination
        /// Note: Read operations are non-transactional by default in ABP
        /// </summary>
        public async Task<(List<Product> Products, int TotalCount)> SearchProductsAsync(
            string searchTerm,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = await _productRepository.GetQueryableAsync();
            query = query.Where(p => p.Name.Contains(searchTerm));

            var totalCount = await _asyncExecuter.CountAsync(query, cancellationToken);
            var products = await _asyncExecuter.ToListAsync(
                query.Skip((pageNumber - 1) * pageSize).Take(pageSize),
                cancellationToken
            );

            return (products, totalCount);
        }

        #endregion
    }

    /// <summary>
    /// Helper class for product statistics
    /// </summary>
    public class ProductStatistics
    {
        public int TotalProducts { get; set; }
        public decimal AveragePrice { get; set; }
        public int TotalStock { get; set; }
        public int LowStockProducts { get; set; }
    }
} 