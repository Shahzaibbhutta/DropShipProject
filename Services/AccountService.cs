using DropShipProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DropShipProject.Services
{
    public class AccountService
    {
        private readonly DatabaseContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AccountService> _logger;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserManager<User> _userManager;

        public AccountService(
            DatabaseContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<AccountService> logger,
            RoleManager<IdentityRole<int>> roleManager,
            UserManager<User> userManager)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<List<User>> GetAllSuppliers()
        {
            return await _context.Users
                .Where(u => u.UserType == "Supplier")
                .OrderBy(u => u.CompanyName)
                .ToListAsync();
        }

        public async Task<(bool Success, string ErrorMessage, User User)> RegisterUserAsync(RegisterViewModel model)
        {
            try
            {
                if (model == null)
                    return (false, "Registration data cannot be null", null);

                if (await _context.Users.AnyAsync(u => u.UserName == model.Username))
                    return (false, "Username already exists", null);

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    return (false, "Email already registered", null);

                var user = new User
                {
                    UserName = model.Username,
                    Email = model.Email,
                    UserType = model.UserType,
                    CompanyName = model.CompanyName,
                    ContactPerson = model.ContactPerson,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    EmailConfirmed = true
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                if (!await _roleManager.RoleExistsAsync(model.UserType))
                {
                    await _roleManager.CreateAsync(new IdentityRole<int>(model.UserType));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, model.UserType);
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to assign role to user {Username}: {Errors}",
                        model.Username, string.Join(", ", roleResult.Errors));
                }

                _logger.LogInformation("New user registered: {Username} ({UserType})", model.Username, model.UserType);
                return (true, null, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Username}", model?.Username);
                return (false, "An error occurred during registration", null);
            }
        }

        public async Task<(bool Success, string ErrorMessage, User User)> AuthenticateAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return (false, "Username and password are required", null);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == username || u.Email == username);

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed for non-existent user: {Username}", username);
                    return (false, "Invalid credentials", null);
                }

                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

                if (result != PasswordVerificationResult.Success)
                {
                    _logger.LogWarning("Authentication failed for user: {Username}", username);
                    return (false, "Invalid credentials", null);
                }

                _logger.LogInformation("User authenticated: {Username}", username);
                return (true, null, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user {Username}", username);
                return (false, "An error occurred during authentication", null);
            }
        }

        public async Task<bool> UserExistsAsync(string usernameOrEmail)
        {
            return await _context.Users
                .AnyAsync(u => u.UserName == usernameOrEmail || u.Email == usernameOrEmail);
        }
    }
}