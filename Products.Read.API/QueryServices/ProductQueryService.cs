using Microsoft.EntityFrameworkCore;
using Products.Read.API.Abstractions;
using Products.Read.API.Domain.Models;
using Products.Read.API.DTOs;
using Products.Read.API.Infrastructure.Data;
using Products.Read.API.Paging;
using Products.Read.API.QueryResponses;

namespace Products.Read.API.QueryServices
{
    /// <summary>
    /// ProductQueryService uses the EF DbContext directly versus implementing a repository for performance.
    /// State is not modified - queries only.
    /// </summary>
    public class ProductQueryService : IProductQueryService
    {
        private readonly ProductsReadDbContext _db;
        private readonly ILogger<ProductQueryService> _logger;

        public ProductQueryService(ProductsReadDbContext db, ILogger<ProductQueryService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IAsyncEnumerable<Product> GetProductsAsAsyncEnumerable()
        {
            return _db.Products.AsAsyncEnumerable();
        }

        public async Task<GetProductsResult> GetAllProductsAsync()
        {
            IEnumerable<Product> products = await _db.Products.Include(p => p.Images).Include(p => p.Documents).AsSplitQuery().ToListAsync();
            List<ProductDTO> productDTOs = MapProductsToDTOs(products); 
            return new GetProductsResult(true, productDTOs, null);
        }

        public async Task<GetPagedAndFilteredProductsResult> GetPagedAndFilteredProductsAsync(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10)
        {
            var query = _db.Products.Include(p => p.Images).Include(p => p.Documents).AsSplitQuery().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(c => c.Category.ToLower().Contains(category.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(p => p.Name.ToLower().Contains(filter.ToLower()) || p.Description.ToLower().Contains(filter.ToLower()));
            }

            switch (sortColumn?.ToLower())
            {
                case "id":
                    query = query.OrderBy(p => p.Id);
                    break;
                case "name":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "category":
                    query = query.OrderBy(p => p.Category);
                    break;
                case "price ascending":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price descending":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }

            List<Product> records = await query.ToListAsync();
            int totalCount = records.Count;

            var products = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            List<ProductDTO> productDTOs = MapProductsToDTOs(products);
            PaginationMetadata metadata = new PaginationMetadata(totalCount, pageSize, pageNumber);

            return new GetPagedAndFilteredProductsResult(true, productDTOs, metadata, null);
        }

        public async Task<GetProductByIdResult> GetProductByIdAsync(int id)
        {
            Product? product = await _db.Products.Include(p => p.Images).Include(p => p.Documents).AsSplitQuery().FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return new GetProductByIdResult(false, null, $"A product with Id {id} was not found.");

            ProductDTO productDTO = new ProductDTO
            {
                Id = product.Id,
                AggregateId = product.AggregateId,
                Name = product.Name,
                Category = product.Category,
                Description = product.Description,
                Price = product.Price,
                Currency = product.Currency,
                Status = product.Status,
                QuantityOnHand = product.QuantityOnHand,
                QuantityAllocated = product.QuantityAllocated,
                UOM = product.UOM,
                LowStockThreshold = product.LowStockThreshold,
                Version = product.Version,
                DateCreated = product.DateCreated,
                DateUpdated = product.DateUpdated,
                Images = MapImagesToDTOs(product.Images!),
                Documents = MapDocumentsToDTOs(product.Documents!)
            };

            return new GetProductByIdResult(true, productDTO, null);
        }

        private List<ProductDTO> MapProductsToDTOs(IEnumerable<Product> products)
        {
            List<ProductDTO> productDTOs = new List<ProductDTO>();

            foreach (var product in products)
            {
                ProductDTO dto = new ProductDTO
                {
                    Id = product.Id,
                    AggregateId = product.AggregateId,
                    Name = product.Name,
                    Category = product.Category,
                    Description = product.Description,
                    Price = product.Price,
                    Currency = product.Currency,
                    Status = product.Status,
                    QuantityOnHand = product.QuantityOnHand,
                    QuantityAllocated = product.QuantityAllocated,
                    UOM = product.UOM,
                    LowStockThreshold = product.LowStockThreshold,
                    Version = product.Version,
                    DateCreated = product.DateCreated,
                    DateUpdated = product.DateUpdated,
                    Images = MapImagesToDTOs(product.Images!),
                    Documents = MapDocumentsToDTOs(product.Documents!)
                };

                productDTOs.Add(dto);
            }

            return productDTOs;
        }

        private List<ImageDataDTO> MapImagesToDTOs(IEnumerable<ImageData> images)
        {
            List<ImageDataDTO> imageDTOs = new List<ImageDataDTO>();
            foreach (var image in images)
            {
                ImageDataDTO dto = new ImageDataDTO
                {
                    Id = image.Id,
                    ProductId = image.ProductId,
                    Name = image.Name,
                    Caption = image.Caption,
                    SequenceNumber = image.SequenceNumber,
                    ImageUrl = image.ImageUrl,
                    ThumbnailUrl = image.ThumbnailUrl
                };
                imageDTOs.Add(dto);
            }

            return imageDTOs;
        }

        private List<DocumentDataDTO> MapDocumentsToDTOs(IEnumerable<DocumentData> documents)
        {
            List<DocumentDataDTO> docDTOs = new List<DocumentDataDTO>();
            foreach (var doc in documents)
            {
                DocumentDataDTO dto = new DocumentDataDTO
                {
                    Id = doc.Id,
                    ProductId = doc.ProductId,
                    Name = doc.Name,
                    Title = doc.Title,
                    SequenceNumber = doc.SequenceNumber,
                    DocumentUrl = doc.DocumentUrl,
                };
                docDTOs.Add(dto);
            }
            return docDTOs;
        }

        public async Task<GetProductSummariesResult> GetAllProductSummariesAsync()
        {
            IEnumerable<Product> products = await _db.Products.Include(p => p.Images).Include(p => p.Documents).AsSplitQuery().ToListAsync();
            List<ProductSummaryDTO> summaryDTOs = new List<ProductSummaryDTO>();
            foreach (var product in products)
            {
                ProductSummaryDTO dto = new ProductSummaryDTO
                {
                    Id = product.Id,
                    AggregateId = product.AggregateId,
                    Name = product.Name,
                    Category = product.Category,
                    Description = product.Description,
                    Price = product.Price,
                    Currency = product.Currency,
                    Status = product.Status,
                    QuantityOnHand = product.QuantityOnHand,
                    QuantityAllocated = product.QuantityAllocated,
                    UOM = product.UOM,
                    LowStockThreshold = product.LowStockThreshold,
                    Version = product.Version,
                    DateCreated = product.DateCreated,
                    DateUpdated = product.DateUpdated,
                    DefaultImageUri = product.Images != null && product.Images.Any() ? product.Images.OrderBy(i => i.SequenceNumber).First().ImageUrl : null,
                    ImageCount = product.Images is null ? 0 : product.Images.Count(),               
                    DocumentCount = product.Documents is null ? 0 : product.Documents.Count()
                };
                summaryDTOs.Add(dto);
            }
            return new GetProductSummariesResult(true, summaryDTOs, null);
        }

        public async Task<GetPagedAndFilteredProductSummariesResult> GetPagedAndFilteredProductSummariesAsync(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10)
        {
            var query = _db.Products.Include(p => p.Images).Include(p => p.Documents).AsSplitQuery().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(c => c.Category.ToLower().Contains(category.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(p => p.Name.ToLower().Contains(filter.ToLower()) || p.Description.ToLower().Contains(filter.ToLower()));
            }

            switch (sortColumn?.ToLower())
            {
                case "id":
                    query = query.OrderBy(p => p.Id);
                    break;
                case "name":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "category":
                    query = query.OrderBy(p => p.Category);
                    break;
                case "price asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }

            List<Product> records = await query.ToListAsync();
            int totalCount = records.Count;

            var products = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            List<ProductSummaryDTO> summaryDTOs = new List<ProductSummaryDTO>();
            foreach (var product in products)
            {
                ProductSummaryDTO dto = new ProductSummaryDTO
                {
                    Id = product.Id,
                    AggregateId = product.AggregateId,
                    Name = product.Name,
                    Category = product.Category,
                    Description = product.Description,
                    Price = product.Price,
                    Currency = product.Currency,
                    Status = product.Status,
                    QuantityOnHand = product.QuantityOnHand,
                    QuantityAllocated = product.QuantityAllocated,
                    UOM = product.UOM,
                    LowStockThreshold = product.LowStockThreshold,
                    Version = product.Version,
                    DateCreated = product.DateCreated,
                    DateUpdated = product.DateUpdated,
                    DefaultImageUri = product.Images != null && product.Images.Any() ? product.Images.OrderBy(i => i.SequenceNumber).First().ImageUrl : null,
                    ImageCount = product.Images is null ? 0 : product.Images.Count(),
                    DocumentCount = product.Documents is null ? 0 : product.Documents.Count()

                };
                summaryDTOs.Add(dto);
            }

            PaginationMetadata metadata = new PaginationMetadata(totalCount, pageSize, pageNumber);
            return new GetPagedAndFilteredProductSummariesResult(true, summaryDTOs, metadata, null);
        }

        public async Task<GetProductSummaryByIdResult> GetProductSummaryByIdAsync(int id)
        {
            Product? product = await _db.Products.Include(p => p.Images).Include(p => p.Documents).AsSplitQuery().FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return new GetProductSummaryByIdResult(false, null, $"A product with Id {id} was not found.");

            ProductSummaryDTO dto = new ProductSummaryDTO
            {
                Id = product.Id,
                AggregateId = product.AggregateId,
                Name = product.Name,
                Category = product.Category,
                Description = product.Description,
                Price = product.Price,
                Currency = product.Currency,
                Status = product.Status,
                QuantityOnHand = product.QuantityOnHand,
                QuantityAllocated = product.QuantityAllocated,
                UOM = product.UOM,
                LowStockThreshold = product.LowStockThreshold,
                Version = product.Version,
                DateCreated = product.DateCreated,
                DateUpdated = product.DateUpdated,
                DefaultImageUri = product.Images != null && product.Images.Any() ? product.Images.OrderBy(i => i.SequenceNumber).First().ImageUrl : null,
                ImageCount = product.Images is null ? 0 : product.Images.Count(),
                DocumentCount = product.Documents is null ? 0 : product.Documents.Count()
            };
            
            return new GetProductSummaryByIdResult(true, dto, null);
        }

        public async Task<CarouselProductResult> GetCarouselProductsAsync()
        {
            try
            {
                var result = await (from p in _db.Products
                             join i in _db.ImageData on p.Id equals i.ProductId into productImages
                             select new CarouselProductDTO
                             {
                                 Name = p.Name, 
                                 Category = p.Category, 
                                 Description = p.Description,
                                 Price = p.Price,
                                 Currency = p.Currency,
                                 QuantityOnHand = p.QuantityOnHand,
                                 ImageData = p.Images != null && p.Images.Any() ? p.Images.First().ToDTO() : null
                             }).ToListAsync();

                return new CarouselProductResult(true, result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving carousel product images.");
                return new CarouselProductResult(false, null, "An error occurred connecting with the Products Read-Side Database. Please try again later.");
            }
        }

        public async Task<CarouselImageUrlResult> GetCarouselImageUrlsAsync()
        {
            try
            {
                var result = await (from p in _db.Products
                                    join i in _db.ImageData on p.Id equals i.ProductId into productImages
                                    select p.Images != null && p.Images.Any() ? p.Images.First().ImageUrl : string.Empty
                                    ).ToListAsync();

                List<string> urls = result.Where(url => !string.IsNullOrEmpty(url)).ToList();

                return new CarouselImageUrlResult(true, urls, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving carousel product image urls.");
                return new CarouselImageUrlResult(false, null, "An error occurred connecting with the Products Read-Side Database. Please try again later.");
            }
        }

        public async Task<(bool IsSuccess, int ProductCount, string? ErrorMessage)> GetProductCountAsync()
        {
            try
            {
                int count = _db.Products.Count();
                return (true, count, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while counting products.");
                return (false, 0, "An error occurred connecting with the Products Read-Side Database. Please try again later.");
            }
        }
    }
}
