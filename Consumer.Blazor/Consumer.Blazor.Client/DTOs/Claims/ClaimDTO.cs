namespace Consumer.Blazor.Client.DTOs.Claims
{
    public class ClaimDTO
    {
        public string? Type { get; set; }
        public string? Value { get; set; }

        public ClaimDTO(string? type, string? value)
        {
            Type = type;
            Value = value;
            if (Value is not null && Value.Length > 100) { Value = $"{Value.Substring(0, 100)} ..."; }
        }
    }
}
