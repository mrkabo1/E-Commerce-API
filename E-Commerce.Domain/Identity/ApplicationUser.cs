using Microsoft.AspNetCore.Identity;

namespace E_Commerce.Domain.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? PersonName { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpirationDateTime { get; set; }
        public string? Address { get; set; }
    }
}
