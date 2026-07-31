namespace Orders.API.Auth
{
    public class ApiUserInfoDTO
    {
        public List<ClaimDTO> AccessTokenClaims { get; set; } = new List<ClaimDTO>();
        public List<ClaimDTO> ClaimsPrincipalClaims { get; set; } = new List<ClaimDTO>();

        public string? ErrorMessage { get; set; }
        public List<string> Remarks { get; set; } = new List<string>();
    }
}
