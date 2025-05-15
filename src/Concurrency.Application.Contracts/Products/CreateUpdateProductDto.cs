using System.ComponentModel.DataAnnotations;

namespace Concurrency.Products;

public class CreateUpdateProductDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
} 