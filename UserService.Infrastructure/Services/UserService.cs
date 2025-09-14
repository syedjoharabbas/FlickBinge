using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Core.DTOs;
using UserService.Core.Entities;
using UserService.Infrastructure.DBContext;
using UserService.Infrastructure.RabbitMQ;

namespace UserService.Infrastructure.Services
{
    public class UserService
    {
        private readonly UserDbContext _db;
        private readonly IPasswordHasher<User> _hasher;
        private readonly JwtSettings _jwt;
        private readonly RabbitMQPublisher _publisher;

        public UserService(UserDbContext db, IPasswordHasher<User> hasher, IOptions<JwtSettings> jwt, RabbitMQPublisher publisher)
        {
            _db = db;
            _hasher = hasher;
            _jwt = jwt.Value;
            _publisher = publisher;
        }

        // Core User Management
        public async Task<List<User>> GetAllUsersAsync() => await _db.Users.ToListAsync();
        public async Task<User?> GetUserByIdAsync(Guid id) => await _db.Users.FindAsync(id);

        public async Task<User> CreateUserAsync(User user)
        {
            user.PasswordHash = _hasher.HashPassword(user, user.PasswordHash);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _publisher.PublishUserCreatedAsync(user.Id);
            return user;
        }

        // AUTH
        public async Task<Result<object>> RegisterAsync(RegisterRequest request)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username))
                return Result<object>.Fail("Username or Email already exists.");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _hasher.HashPassword(new User(), request.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await _publisher.PublishUserCreatedAsync(user.Id);

            return Result<object>.Success(new { user.Id, user.Username, user.Email });
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null) return Result<AuthResponse>.Fail("Invalid credentials.");

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return Result<AuthResponse>.Fail("Invalid credentials.");

            var accessToken = GenerateJwtToken(user);
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays)
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return Result<AuthResponse>.Success(new AuthResponse(
                accessToken, refreshToken.Token,
                DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes),
                user.Id, user.Username, user.Email));
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshRequest request)
        {
            var refreshToken = await _db.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt =>
                rt.Token == request.RefreshToken && rt.ExpiresAt > DateTime.UtcNow && rt.RevokedAt == null);

            if (refreshToken == null) return Result<AuthResponse>.Fail("Invalid refresh token.");

            var newAccessToken = GenerateJwtToken(refreshToken.User);

            return Result<AuthResponse>.Success(new AuthResponse(
                newAccessToken, refreshToken.Token,
                DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes),
                refreshToken.User.Id, refreshToken.User.Username, refreshToken.User.Email));
        }

        public async Task<Result<bool>> LogoutAsync(RefreshRequest request)
        {
            var refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
            if (refreshToken == null) return Result<bool>.Fail("Not found.");

            refreshToken.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<object>> GetCurrentUserAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Result<object>.Fail("Unauthorized.");

            var u = await _db.Users.FindAsync(Guid.Parse(userId));
            return u != null
                ? Result<object>.Success(new { u.Id, u.Username, u.Email, u.Role })
                : Result<object>.Fail("Not found.");
        }

        // Helper
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Small Result wrapper for clean return values
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public string? Error { get; private set; }

        public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
        public static Result<T> Fail(string error) => new() { IsSuccess = false, Error = error };
    }
}
