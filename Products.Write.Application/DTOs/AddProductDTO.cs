namespace Products.Write.Application.DTOs
{
    public class AddProductDTO
    {
        public string Name { get; init; } 
        public string Category { get; init; }
        public string Description { get; init; } 
        public decimal Price { get; init; }
        public string Currency { get; init; } 
        public string Status { get; init; }
        public int QuantityOnHand { get; init; }
        public string UOM { get; init; } 
        public int LowStockThreshold { get; init; }

        public AddProductDTO(string name, string category, string description, 
            decimal price, string currency, string status, int quantityOnHand,
            string uom, int lowStockThreshold)
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
        }
    }
}
