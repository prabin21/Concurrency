using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Concurrency.Products;

public class Product : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    protected Product()
    {
    }

    public Product(
        Guid id,
        string name,
        decimal price,
        int stockQuantity
    ) : base(id)
    {
        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public void UpdateStock(int newQuantity)
    {
        StockQuantity = newQuantity;
    }

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
    }
} 