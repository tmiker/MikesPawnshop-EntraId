using System.Security.Claims;

namespace Accounts.API.Auth
{
    public class ClaimDTO
    {
        public string? Type { get; set; }
        public string? Value { get; set; }

        public ClaimDTO(string? type, string? value)
        {
            Type = type;
            Value = value;
        }

        public ClaimDTO(Claim claim)
        {
            Type = claim.Type;
            Value = claim.Value;
        }
    }
}
