using HealthCheckAPI.Models;
using HealthCheckAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HealthCheckAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JwtService _jwtService;

        public AuthController(UserService userService, JwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _userService.GetUserByUsername(request.Username);
            if (user == null)
                return Unauthorized("Invalid username or password");

            if (!_userService.VerifyPassword(user, request.Password))
                return Unauthorized("Invalid username or password");

            var token = _jwtService.GenerateToken(user);
            return Ok(new { Token = token, user.Username, user.Email });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            var existingUser = _userService.GetUserByUsername(request.Username);
            if (existingUser != null)
                return BadRequest("Username already exists");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
            };

            _userService.CreateUser(user, request.Password);

            return Ok("User registered successfully");
        }

    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

}
