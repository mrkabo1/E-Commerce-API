using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace E_Commerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Get All Users (Admin Only).
        /// </summary>

        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> Get()
        {
            var users = await _authService.GetUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO registerDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var response = await _authService.RegisterAsync(registerDTO);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "The email is already registered.")
                    return BadRequest(ex.Message);

                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// login a exist user.
        /// </summary>

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            if (loginDTO == null)
            {
                return BadRequest("Invalid Model State");
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _authService.LoginAsync(loginDTO);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid email or password");
            }
        }

        /// <summary>
        /// Create new Jwt Token.
        /// </summary>

        [HttpPost("get-new-token")]
        public async Task<IActionResult> GenerateNewAccessToken([FromBody] TokenModel tokenModel)
        {
            if (tokenModel == null)
                return BadRequest("Invalid client request");

            try
            {
                var response = await _authService.RefreshTokenAsync(tokenModel);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

    }
}
