using Products.Read.API.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Products.Read.API.Domain.Models
{
    [Table("ImageData", Schema = "dbo")]
    public class ImageData
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Name { get; private set; }           // virtual directory plus filename
        public string? Caption { get; private set; }
        public int SequenceNumber { get; private set; }
        public string? ImageUrl { get; private set; }
        public string? ThumbnailUrl { get; private set; }

        [ForeignKey(nameof(ProductId))]
        public int ProductId { get; set; }
        [InverseProperty(nameof(Product.Images))]
        public Product Product { get; set; } = default!;

        private ImageData() { }

        public ImageData(string name, string caption, int sequenceNumber, string imageUrl, string thumbnailUrl)
        {
            Name = name;
            Caption = caption;
            SequenceNumber = sequenceNumber;
            ImageUrl = imageUrl;
            ThumbnailUrl = thumbnailUrl;
        }

        private void SetSequenceNumber(int sequenceNumber)
        {
            SequenceNumber = sequenceNumber;
        }

        public ImageDataDTO ToDTO()
        {
            return new ImageDataDTO
            {
                Name = this.Name,
                Caption = this.Caption,
                SequenceNumber = this.SequenceNumber,
                ImageUrl = this.ImageUrl,
                ThumbnailUrl = this.ThumbnailUrl
            };
        }
    }
}
