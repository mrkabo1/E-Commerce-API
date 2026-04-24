using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Application.DTOs
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Person Name can't be blank")]
        public string PersonName { get; set; }
        [Required(ErrorMessage = "Email Name can't be blank")]
        [EmailAddress(ErrorMessage = "Email should be in a proper email address format")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Phone Name can't be blank")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Password Name can't be blank")]
        public string Address { get; set; }
        [Required(ErrorMessage = "Address Name can't be blank")]
        public string Password { get; set; }
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}
