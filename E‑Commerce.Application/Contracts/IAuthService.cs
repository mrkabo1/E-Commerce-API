using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Identity;

namespace E_Commerce.Application.Contracts
{
    public interface IAuthService
    {
        public Task<List<ApplicationUser>> GetUsersAsync();
        public Task<AuthenticationResponseDTO> RegisterAsync(RegisterDTO registerDTO);
        public Task<AuthenticationResponseDTO> LoginAsync(LoginDTO loginDTO);
        public Task<AuthenticationResponseDTO> RefreshTokenAsync(TokenModel tokenModel);
    }
}
