namespace Consumer.Blazor.Client.DTOs.Accounts
{
    public class AddAccountDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public List<AddressDTO> Addresses { get; set; } = new List<AddressDTO>();
        public string? PhoneNumber { get; set; }
    }
}
