
namespace Products.Write.API.Auth
{
    public interface ITokenDecoder
    {
        string? GetUserId(string token);
        ApiUserInfoDTO GetTokenData(string? token);
        UserClaimsDTO GetUserClaims(string token);
    }
}
