using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Products.Write.API.Auth
{
    public class TokenDecoder : ITokenDecoder
    {
        public string? GetUserId(string token)
        {
            JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
            JsonWebToken? jsonWebToken = tokenHandler.ReadToken(token) as JsonWebToken;
            if (jsonWebToken != null)
            {
                Claim? userClaim = jsonWebToken.Claims.FirstOrDefault(c => c.Type == "sub");
                if (userClaim != null) return userClaim.Value;
            }
            return null;
        }

        public ApiUserInfoDTO GetTokenData(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return new ApiUserInfoDTO() { ErrorMessage = $"The provided token was a null or empty string." };

            JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
            JsonWebToken? jsonWebToken = tokenHandler.ReadToken(token) as JsonWebToken;

            ApiUserInfoDTO apiUserInfoDTO = new ApiUserInfoDTO();
            if (jsonWebToken != null)
            {
                foreach (var claim in jsonWebToken.Claims)
                {
                    if (claim.Type == "roles") apiUserInfoDTO.ApiAuthorizationHeaderRolesList.Add(claim.Value);
                    else apiUserInfoDTO.ApiAuthorizationHeaderClaimsList.Add($"{claim.Type} : {claim.Value}");
                }
            }
            return apiUserInfoDTO;
        }

        public UserClaimsDTO GetUserClaims(string token)
        {
            JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
            JsonWebToken? jsonWebToken = tokenHandler.ReadToken(token) as JsonWebToken;
            UserClaimsDTO userClaimsDTO = new UserClaimsDTO() { UserClaims = jsonWebToken?.Claims.ToList() };
            return userClaimsDTO;
        }
    }
}
