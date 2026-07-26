using System.Text;
using System.Text.Json.Serialization;

namespace Admin.Blazor.Client.ErrorHandling
{
    public class ProductsWriteProblemDetails
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }

        public IDictionary<string, string[]>? Errors { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object?>? Extensions { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(Type)}: {Type}");
            sb.AppendLine($"{nameof(Title)}: {Title}");
            sb.AppendLine($"{nameof(Status)}: {Status}");
            sb.AppendLine($"{nameof(Detail)}: {Detail}");
            sb.AppendLine($"{nameof(Instance)}: {Instance}");

            if (Errors != null)
            {
                sb.AppendLine("Errors:");
                foreach (var error in Errors)
                {
                    sb.AppendLine($"  {error.Key}: [{string.Join(", ", error.Value)}]");
                }
            }

            if (Extensions != null)
            {
                sb.AppendLine("Extensions:");
                foreach (var extension in Extensions)
                {
                    sb.AppendLine($"  {extension.Key}: {extension.Value}");
                }
            }

            return sb.ToString();
        }

    }
}
