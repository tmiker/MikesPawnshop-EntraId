using MediatR;
using Microsoft.Extensions.Primitives;
using Products.Write.Application.CQRS.CommandResults;
using Products.Write.Application.DTOs;

namespace Products.Write.Application.CQRS.Commands
{
    public class AddProduct : IRequest<AddProductResult>
    {
        public string Name { get; init; } = default!;
        public string Category { get; init; } = default!;
        public string Description { get; init; } = default!;
        public decimal Price { get; init; }
        public string Currency { get; init; } = default!;
        public string Status { get; init; } = default!;
        public int QuantityOnHand { get; init; }
        public string UOM { get; init; } = default!;
        public int LowStockThreshold { get; init; }
        public string? CorrelationId { get; set; } 

        public AddProduct(string name, string category, string description, decimal price, string currency, string status,
            int quantityOnHand, string uom, int lowStockThreshold, string? correlationId)
        {
            Name = name;
            Category = category;    
            Description = description;
            Price = price;
            Currency = currency;
            Status = status;        
            QuantityOnHand = quantityOnHand;
            UOM = uom;
            LowStockThreshold = lowStockThreshold;
            CorrelationId = correlationId;
        }

        public AddProduct(AddProductDTO dto, StringValues correlationId)
        {
            Name = dto.Name;
            Category = dto.Category;        
            Description = dto.Description;
            Price = dto.Price;
            Currency = dto.Currency;
            Status = dto.Status;            
            QuantityOnHand = dto.QuantityOnHand;
            UOM = dto.UOM;
            LowStockThreshold = dto.LowStockThreshold;
            CorrelationId = correlationId!;
        }
    }
}
