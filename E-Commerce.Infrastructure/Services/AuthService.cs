using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace E_Commerce.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<List<ApplicationUser>> GetUsersAsync()
        {
            _logger.LogInformation("Fetching all users.");
            var users = await _userManager.Users.ToListAsync();
            _logger.LogDebug("Retrieved {UserCount} users.", users.Count);
            return users;
        }

        public async Task<AuthenticationResponseDTO> RegisterAsync(RegisterDTO registerDTO)
        {
            _logger.LogInformation("Registration attempt for email {Email}.", registerDTO.Email);

            var existingUser = await _userManager.FindByEmailAsync(registerDTO.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already registered.", registerDTO.Email);
                throw new InvalidOperationException("The email is already registered.");
            }

            var user = new ApplicationUser
            {
                PersonName = registerDTO.PersonName,
                PhoneNumber = registerDTO.PhoneNumber,
                Email = registerDTO.Email,
                UserName = registerDTO.Email,
                Address = registerDTO.Address
            };

            var createResult = await _userManager.CreateAsync(user, registerDTO.Password);
            if (!createResult.Succeeded)
            {
                var errorMessages = string.Join("; ", createResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Registration failed for {Email}: {Errors}", registerDTO.Email, errorMessages);
                throw new InvalidOperationException(errorMessages);
            }

            _logger.LogInformation("User {Email} created successfully.", registerDTO.Email);

            await _userManager.AddToRoleAsync(user, "Customer");
            _logger.LogDebug("Assigned 'Customer' role to user {Email}.", registerDTO.Email);

            var authenticationResponse = await _jwtService.CreateJwtToken(user);

            user.RefreshToken = authenticationResponse.RefreshToken;
            user.RefreshTokenExpirationDateTime = authenticationResponse.RefreshTokenExpirationDateTime;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} registered and authenticated successfully.", registerDTO.Email);
            return authenticationResponse;
        }

        public async Task<AuthenticationResponseDTO> LoginAsync(LoginDTO loginDTO)
        {
            _logger.LogInformation("Login attempt for email {Email}.", loginDTO.Email);

            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: email {Email} not found.", loginDTO.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, loginDTO.Password);
            if (!isPasswordCorrect)
            {
                _logger.LogWarning("Login failed: incorrect password for email {Email}.", loginDTO.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var authenticationResponse = await _jwtService.CreateJwtToken(user);

            user.RefreshToken = authenticationResponse.RefreshToken;
            user.RefreshTokenExpirationDateTime = authenticationResponse.RefreshTokenExpirationDateTime;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} logged in successfully.", loginDTO.Email);
            return authenticationResponse;
        }

        public async Task<AuthenticationResponseDTO> RefreshTokenAsync(TokenModel tokenModel)
        {
            _logger.LogInformation("Refresh token attempt.");

            if (string.IsNullOrWhiteSpace(tokenModel.Token) || string.IsNullOrWhiteSpace(tokenModel.RefreshToken))
            {
                _logger.LogWarning("Refresh token failed: missing token or refresh token.");
                throw new ArgumentException("Token and refresh token are required");
            }

            ClaimsPrincipal principal;
            try
            {
                principal = _jwtService.GetPrincipalFromJwtToken(tokenModel.Token);
            }
            catch (SecurityTokenException)
            {
                _logger.LogWarning("Refresh token failed: invalid JWT access token.");
                throw new ArgumentException("Invalid JWT access token");
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Refresh token failed: malformed JWT access token.");
                throw new ArgumentException("Malformed JWT access token");
            }

            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Refresh token failed: email claim missing in token.");
                throw new ArgumentException("Invalid token claims");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.RefreshToken != tokenModel.RefreshToken || user.RefreshTokenExpirationDateTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token failed: invalid refresh token for email {Email}.", email);
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            var authenticationResponse = await _jwtService.CreateJwtToken(user);
            user.RefreshToken = authenticationResponse.RefreshToken;
            user.RefreshTokenExpirationDateTime = authenticationResponse.RefreshTokenExpirationDateTime;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Refresh token succeeded for user {Email}.", email);
            return authenticationResponse;
        }
    }
}