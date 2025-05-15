using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Concurrency.Products;

public interface IProductAppService : 
    ICrudAppService<
        ProductDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateProductDto>
{
    Task<ProductDto> UpdateStockAsync(Guid id, int newQuantity);
    Task<ProductDto> UpdatePriceAsync(Guid id, decimal newPrice);
} 