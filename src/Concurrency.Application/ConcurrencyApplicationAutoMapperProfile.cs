using AutoMapper;
using Concurrency.Products;

namespace Concurrency;

public class ConcurrencyApplicationAutoMapperProfile : Profile
{
    public ConcurrencyApplicationAutoMapperProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<CreateUpdateProductDto, Product>();
    }
}
