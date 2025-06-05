using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Concurrency.Products;

public class Product : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
} 