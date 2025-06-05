using System;
using Volo.Abp.Application.Dtos;

namespace Concurrency.Products;

public class ProductDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
} 