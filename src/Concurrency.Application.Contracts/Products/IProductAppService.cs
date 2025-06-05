using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using System.Data;

namespace Concurrency.Products;

public interface IProductAppService : 
    ICrudAppService<
        ProductDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateProductDto>
{
    Task<ProductDto> UpdateStockAsync(Guid id, int newQuantity, IsolationLevel? isolationLevel = null);
    Task<ProductDto> UpdatePriceAsync(Guid id, decimal newPrice, IsolationLevel? isolationLevel = null);
    Task<ProductDto> UpdateWithIsolationAsync(Guid id, CreateUpdateProductDto input, IsolationLevel? isolationLevel = null);
    
    // New methods for different isolation levels
    Task<List<ProductDto>> GetProductsWithReadUncommittedAsync();
    Task<ProductDto> UpdateProductWithSerializableAsync(Guid id, CreateUpdateProductDto input);
    Task<ProductDto> UpdateStockWithRepeatableReadAsync(Guid id, int newQuantity);
    Task<ProductDto> UpdatePriceWithSnapshotAsync(Guid id, decimal newPrice);
} 