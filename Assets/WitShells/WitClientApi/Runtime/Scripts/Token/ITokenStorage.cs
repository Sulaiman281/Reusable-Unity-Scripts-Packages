using System.Threading.Tasks;

namespace WitShells.WitClientApi
{
    public interface ITokenStorage
    {
        Task SignInAsync(TokenResponse tokens);
        Task SignOutAsync();
        Task<string> GetAccessTokenAsync();
        Task<string> GetRefreshTokenAsync();
        Task<TokenResponse> GetTokensAsync();
    }
}
