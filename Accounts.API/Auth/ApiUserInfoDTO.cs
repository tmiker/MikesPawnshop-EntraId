using Microsoft.AspNetCore.Components.Authorization;

namespace Accounts.API.Auth
{
    public class ApiUserInfoDTO
    {
        // data from API Request Auth Header Value
        public string? ParsedApiAuthorizationHeaderValue { get; set; }                                                                  // trimmed 'Bearer' to just get the access token
        // public IDictionary<string, string> ApiAuthorizationHeaderClaimsDictionary { get; set; } = new Dictionary<string, string>();  // decoded from access token
        public IList<string> ApiAuthorizationHeaderClaimsList { get; set; } = new List<string>();                                       // decoded from access token
        public IList<string> ApiAuthorizationHeaderRolesList { get; set; } = new List<string>();                                        // decoded from access token

        // data from API User.Claims
        // public IDictionary<string, string> ApiUserClaimsDictionary { get; set; } = new Dictionary<string, string>();                 // from User.Claims
        public IList<string> ApiUserClaimsClaimsList { get; set; } = new List<string>();                                                      // from User.Claims
        public IList<string> ApiUserClaimsRolesList { get; set; } = new List<string>();                                                 // from User.Claims

        public List<ClaimDTO> AccessTokenClaims { get; set; } = new List<ClaimDTO>();
        public List<ClaimDTO> ClaimsPrincipalClaims { get; set; } = new List<ClaimDTO>();

        // station keeping
        public string? ErrorMessage { get; set; }
        public List<string> Remarks { get; set; } = new List<string>();
    }
}
