using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Products.Write.Application.CQRS.CommandResults;
using Products.Write.Application.DTOs;
using System.Text;

namespace Products.Write.Application.CQRS.Commands
{
    public class AddImage : IRequest<AddImageResult>
    {
        public Guid ProductId { get; init; }
        public string Name { get; private set; } = default!;       // virtual directory plus filename
        public string Caption { get; init; } = default!;
        public string? CorrelationId { get; set; }
        public IFormFile? ImageBlob { get; set; }
        public string? BlobFileName { get; set; }

        public AddImage(AddImageDTO addImageDTO, StringValues correlationId)
        {
            ProductId = Guid.TryParse(addImageDTO.ProductId, out Guid parsedId) ? parsedId : Guid.Empty;
            Name = addImageDTO.Name!;
            Caption = addImageDTO.Caption!;
            CorrelationId = correlationId;
            ImageBlob = addImageDTO.ImageBlob;
            BlobFileName = addImageDTO.BlobFileName;
        }

        public void CleanFileName()
        {
            // best performance is using string builder loop vs using regex
            if (string.IsNullOrEmpty(Name))
            {
                string randomFileName = Path.GetRandomFileName();
                string randomName = Path.GetFileNameWithoutExtension(randomFileName);
                Name = randomName;
            }
            StringBuilder sb = new StringBuilder();
            foreach (char c in Name)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_') sb.Append(c);
            }
            Name = sb.ToString();
        }
    }
}
