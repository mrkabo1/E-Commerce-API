using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace E_Commerce.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<JwtService> _logger;

        public JwtService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<AuthenticationResponseDTO> CreateJwtToken(ApplicationUser user)
        {
            _logger.LogInformation("Creating JWT token for user {Email}.", user.Email);

            var expiration = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:EXPIRATION_MINUTES"]));

            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogDebug("User {Email} has roles: {Roles}.", user.Email, string.Join(", ", roles));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim(ClaimTypes.Name, user.PersonName ?? ""),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenGenerator = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: expiration,
                signingCredentials: signingCredentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            string token = tokenHandler.WriteToken(tokenGenerator);

            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.Now.AddMinutes(
                Convert.ToInt32(_configuration["RefreshToken:EXPIRATION_MINUTES"]));

            _logger.LogInformation("JWT token created for user {Email}, expires at {Expiration}. Refresh token generated, expires at {RefreshExpiry}.",
                user.Email, expiration, refreshTokenExpiry);

            return new AuthenticationResponseDTO
            {
                Token = token,
                Email = user.Email,
                PersonName = user.PersonName,
                Expiration = expiration,
                RefreshToken = refreshToken,
                RefreshTokenExpirationDateTime = refreshTokenExpiry
            };
        }

        public ClaimsPrincipal? GetPrincipalFromJwtToken(string? token)
        {
            _logger.LogDebug("Attempting to extract claims principal from JWT token.");

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("GetPrincipalFromJwtToken called with null or empty token.");
                throw new SecurityTokenException("Token is null or empty.");
            }

            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateLifetime = false  // we allow expired tokens for refresh
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid JWT token algorithm or malformed token.");
                    throw new SecurityTokenException("Invalid Token");
                }

                _logger.LogDebug("Successfully extracted claims principal from JWT token.");
                return principal;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "SecurityTokenException while validating JWT token.");
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "ArgumentException while validating JWT token (malformed).");
                throw;
            }
        }

        private string GenerateRefreshToken()
        {
            byte[] bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var refreshToken = Convert.ToBase64String(bytes);
            _logger.LogDebug("New refresh token generated.");
            return refreshToken;
        }
    }
}