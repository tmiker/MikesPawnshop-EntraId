namespace Admin.Blazor.Client.DTOs.Claims
{
    public class ClaimDTO
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public ClaimDTO(string type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
