using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WEBAPI.DBContext;
using WEBAPI.DBContext.Models;

namespace WEBAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController(MyDBContext context, ILogger<UserController> logger) : ControllerBase
    {

        private readonly ILogger<UserController> _logger = logger;
        private readonly MyDBContext _context = context; // Your DbContext instance

        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }



        [HttpPost("register")]
        public IActionResult Register(RegisterUserModel model)
        {
             if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the username or email already exists
            if (_context.Users.Any(u => u.Username == model.Username))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            if (_context.Users.Any(u => u.Email == model.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Generate a GUID for the primary key
            var userId = Guid.NewGuid();

            // Generate a random salt
            byte[] salt = GenerateSalt();

            // Hash the password with the salt
            byte[] passwordHash = HashPassword(model.Password, salt);

            // Store the hashed password and salt in the database
            var newUser = new User
            {
                UserId = userId,
                Username = model.Username,
                Email = model.Email,
                PasswordHash = passwordHash,
                PasswordSalt = salt
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            var userDto = new
            {
                newUser.UserId,
                newUser.Username,
                newUser.Email
            };

            return Ok(new { message = "User registered successfully", userDto });
        }

        [HttpPut("update")]
        public IActionResult UpdateUser([FromBody] UserUpdateModel model)
        {
            try
            {
                Guid guidValue = new Guid(model.UserId);
                var userToUpdate = _context.Users.FirstOrDefault(u => u.UserId == guidValue);

                if (userToUpdate == null)
                {
                    return NotFound(); // Return 404 if user is not found
                }

                // Update user properties
                userToUpdate.Username = model.Username;
                userToUpdate.Email = model.Email;

                _context.SaveChanges(); // Save changes to the database

                return Ok( new { message = "User updated successfully!", userToUpdate }); // Return the updated user
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user.");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost("delete")]
        public IActionResult DeleteUsers([FromBody] List<string> ids)
        {
            var deletedUserIds = new List<string>();
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new { message = "IDs cannot be empty" });
            }

            var usersToDelete = _context.Users.Where(u => ids.Contains(u.UserId.ToString()) || ids.Contains(u.Username)).ToList();

            if (usersToDelete.Count == 0)
            {
                return NotFound(new { message = "No users found with the provided IDs" });
            }

            _context.Users.RemoveRange(usersToDelete);
            _context.SaveChanges();

            // Add IDs of deleted users to the list
            foreach (var user in usersToDelete)
            {
                deletedUserIds.Add(user.UserId.ToString());
            }

            return Ok(new { message = $"{usersToDelete.Count} user(s) deleted successfully", deletedUserIds });
        }



        [HttpPost("register-multiple")]
        public IActionResult RegisterMultiple(List<RegisterUserModel> models)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (var model in models)
            {
                // Check if the username or email already exists
                if (_context.Users.Any(u => u.Username == model.Username))
                {
                    _logger.LogWarning($"Username '{model.Username}' already exists. Skipping registration.");
                    continue;
                }

                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    _logger.LogWarning($"Email '{model.Email}' already exists. Skipping registration.");
                    continue;
                }

                // Generate a GUID for the primary key
                var userId = Guid.NewGuid();

                // Generate a random salt
                byte[] salt = GenerateSalt();

                // Hash the password with the salt
                byte[] passwordHash = HashPassword(model.Password, salt);

                // Store the hashed password and salt in the database
                var newUser = new User
                {
                    UserId = userId,
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = salt
                };

                _context.Users.Add(newUser);
            }

            _context.SaveChanges();

            return Ok(new { message = "Users registered successfully" });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found!" }); // User not found
            }

            // Verify password
            if (!VerifyPasswordHash(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                return Unauthorized(new { message = "Invalid Password!" }); // Invalid password
            }

            // Authentication successful, generate JWT token

            return Ok(new { message = "Login successful" });
        }

        // Method to verify password hash
        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var sha256 = SHA256.Create();
            // Compute hash of the provided password with the stored salt
            byte[] computedHash = HashPassword(password, storedSalt);

            // Compare the computed hash with the stored hash
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHash[i])
                {
                    return false; // Password hash doesn't match
                }
            }
            return true; // Password hash matches
        }




        // Method to generate a random salt
        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16]; // You can adjust the size of the salt
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        // Method to hash the password with the salt
        private static byte[] HashPassword(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] combinedBytes = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();
                return sha256.ComputeHash(combinedBytes);
            }
        }

        // Model for user registration
        public class RegisterUserModel
        {
            public required string Username { get; set; }

            public required string Email { get; set; }

            public required string Password { get; set; }
        }

        // Model for user registration
        public class LoginModel
        {
            public required string Email { get; set; }

            public required string Password { get; set; }
        }

        public class UserUpdateModel
        {
            public required string UserId { get; set; }
            public required string Username { get; set; }
            public required string Email { get; set; }
        }

    }
}