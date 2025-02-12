using System.IdentityModel.Tokens.Jwt;
using eMarket.Models;
using eMarket.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;

namespace eMarket.Controllers
{
    public class UsersController : BaseController<User>
    {
        private readonly MongoDBService<User> _userService;
        private readonly JwtService _jwtService;
        public UsersController(IOptions<MongoDBSettings> mongoDBSettings, JwtService jwtService)
            : base(mongoDBSettings, mongoDBSettings.Value.CollectionUserName)
        {
            _userService = new MongoDBService<User>(
                mongoDBSettings, mongoDBSettings.Value.CollectionUserName);
            _jwtService = jwtService;
        }

        private async Task<IActionResult?> ValidateUsername(string username)
        {
            var users = await _userService.GetAsync();

            if (users.Any(u => u.Username == username))
            {
                return BadRequest(new { error = "Username already exists." });
            }

            return null;
        }

        private IActionResult? ValidateAndHashPassword(User user)
        {
            string? passwordError = UserService.ValidatePassword(user.Password);
            if (passwordError != null)
            {
                return BadRequest(new { error = passwordError });
            }

            user.Password = UserService.HashPassword(user.Password);
            return null;
        }

        public override async Task<IActionResult> Create([FromBody] User user)
        {
            var usernameValidation = await ValidateUsername(user.Username);
            if (usernameValidation != null) return usernameValidation;

            var passwordValidation = ValidateAndHashPassword(user);
            if (passwordValidation != null) return passwordValidation;

            return await base.Create(user); 
        }

        [HttpPut("{id:length(24)}")]
        public override async Task<IActionResult> Update(string id, User user)
        {
            var usernameValidation = await ValidateUsername(user.Username);
            if (usernameValidation != null) return usernameValidation;

            var passwordValidation = ValidateAndHashPassword(user);
            if (passwordValidation != null) return passwordValidation;

            return await base.Update(id, user);
        }

        [HttpPut("management/{id:length(24)}")]
        public async Task<IActionResult> updateManage(string id, ManageUser model)
        {
            var user = await _userService.GetAsync(model.Id);

            if (user == null)
            {
                return Unauthorized(new { error = "User not found." });
            }

            if (!string.IsNullOrWhiteSpace(model.Username) && model.Username != user.Username)
            {
                var usernameValidation = await ValidateUsername(model.Username);
                if (usernameValidation != null) return usernameValidation;
                user.Username = model.Username;
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.Password = model.Password;
                var passwordValidation = ValidateAndHashPassword(user);
                if (passwordValidation != null) return passwordValidation;
            }

            user.IsAdmin = model.IsAdmin;

            return await base.Update(id, user);
        }

        // Update username and/or password
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUser model)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { error = "No authentication token provided." });
            }

            // Parse JWT token and extract user ID
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value
                         ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Invalid token or missing user ID." });
            }

            var user = await _userService.GetAsync(userId);
            var users = await _userService.GetAsync();

            if (user == null)
            {
                return Unauthorized(new { error = "User not found." });
            }

            // Update username if provided
            if (!string.IsNullOrWhiteSpace(model.NewUsername))
            {
                var usernameValidation = await ValidateUsername(model.NewUsername);
                if (usernameValidation == null)
                    user.Username = model.NewUsername;
                else
                    return usernameValidation;
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                    !UserService.VerifyPassword(model.CurrentPassword, user.Password))
                {
                    return BadRequest(new { error = "Current password is incorrect." });
                }

                string? passwordError = UserService.ValidatePassword(model.NewPassword);
                if (passwordError != null)
                {
                    return BadRequest(new { error = passwordError });
                }

                user.Password = UserService.HashPassword(model.NewPassword);
            }

            // Update and replace token
            await _userService.UpdateAsync(user.Id!, user);

            token = _jwtService.GenerateJwtToken(user);

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(1)
            });

            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthUser model)
        {
            var newUser = new User(model.Username, model.Password, false);
            return await Create(newUser);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthUser model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { error = "Username and password are required." });
            }

            var users = await _userService.GetAsync();
            var user = users.FirstOrDefault(u => u.Username == model.Username);

            if (user == null || !UserService.VerifyPassword(model.Password, user.Password!))
            {
                return Unauthorized(new { error = "Invalid username or password." });
            }

            var token = _jwtService.GenerateJwtToken(user);

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(1)
            });

            return Ok(new { message = "Login successful." });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return Ok(new { message = "Logged out successfully." });
        }

        //Debugging: Provide info from token
        [HttpGet("token-info")]
        public IActionResult GetTokenInfo()
        {
            if (!Request.Cookies.TryGetValue("AuthToken", out var token) || string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { error = "Token not found or expired." });
            }

            try
            {
                var claimsPrincipal = _jwtService.ValidateJwtToken(token);
                if (claimsPrincipal == null)
                {
                    return Unauthorized(new { error = "Invalid or expired token." });
                }

                var claims = claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);

                return Ok(new
                {
                    success = true,
                    message = "Token is valid.",
                    tokenInfo = claims
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Error processing token.", message = ex.Message });
            }
        }

        public class AuthUser
        {
            public required string Username { get; set; }
            public required string Password { get; set; }
        }

        public class ManageUser
        {
            public required string Id { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public bool IsAdmin { get; set; }
        }

        public class ProfileUser
        {
            public string? NewUsername { get; set; }
            public string? CurrentPassword { get; set; }
            public string? NewPassword { get; set; }
        }
    }
}