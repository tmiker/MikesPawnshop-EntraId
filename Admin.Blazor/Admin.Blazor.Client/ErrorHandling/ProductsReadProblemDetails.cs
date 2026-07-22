using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Blazor.Client.ErrorHandling
{
    public class ProductsReadProblemDetails
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object?>? Extensions { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(Type)}: {Type}");
            sb.AppendLine($"{nameof(Title)}: {Title}");
            sb.AppendLine($"{nameof(Status)}: {Status}");
            sb.AppendLine($"{nameof(Detail)}: {Detail}");

            if (Title == "Validation Error" && Extensions is not null)
            {
                if (Extensions.TryGetValue("errors", out object? errorJsonElement))
                {
                    Dictionary<string, string[]>? errorsDict = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorJsonElement?.ToString()!);
                    if (errorsDict is not null)
                    {
                        sb.AppendLine("Errors:");
                        foreach (var error in errorsDict!)
                        {
                            sb.AppendLine($"  {error.Key}: {string.Join(", ", error.Value)}");
                        }
                    }
                }
            }

            sb.AppendLine($"{nameof(Instance)}: {Instance}");

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
