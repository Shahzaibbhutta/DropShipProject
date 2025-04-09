using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DropShipProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropShipProject.Services
{
    public class AccountService
    {
        private readonly DatabaseContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AccountService> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public AccountService(
            DatabaseContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<AccountService> logger,
            RoleManager<IdentityRole> roleManager,
            UserManager<User> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<(bool Success, string ErrorMessage, User User)> RegisterUserAsync(RegisterViewModel model)
        {
            try
            {
                // Validate input
                if (model == null)
                    return (false, "Registration data cannot be null", null);

                if (await _context.Users.AnyAsync(u => u.UserName == model.Username))
                    return (false, "Username already exists", null);

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    return (false, "Email already registered", null);

                // Create user
                var user = new User
                {
                    UserName = model.Username,
                    Email = model.Email,
                    UserType = model.UserType,
                    CompanyName = model.CompanyName,
                    ContactPerson = model.ContactPerson,
                    PhoneNumber = model.PhoneNumber ?? string.Empty,
                    Address = model.Address ?? string.Empty,
                    EmailConfirmed = true // Set to false if using email confirmation
                };

                // Hash password
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

                // Save user
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Assign role
                if (!await _roleManager.RoleExistsAsync(model.UserType))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.UserType));
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

        public async Task<List<User>> GetAllSuppliers()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.UserType == "Supplier")
                    .OrderBy(u => u.CompanyName)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers");
                return new List<User>();
            }
        }

        public async Task<bool> UserExistsAsync(string usernameOrEmail)
        {
            return await _context.Users
                .AnyAsync(u => u.UserName == usernameOrEmail || u.Email == usernameOrEmail);
        }
    }
}