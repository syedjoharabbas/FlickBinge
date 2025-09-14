using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using UserService.Core.DTOs;
using UserService.Core.Entities;
using UserService.Core.Interfaces;
using UserService.Infrastructure.DBContext;
using UserService.Infrastructure.RabbitMQ;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Services & DI
// =======================
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService.Infrastructure.Services.UserService>();
builder.Services.AddSingleton<RabbitMQPublisher>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSettings);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// =======================
// User CRUD (via UserService)
// =======================
app.MapGet("/", async (UserService.Infrastructure.Services.UserService service) =>
{
    var users = await service.GetAllUsersAsync();
    return Results.Ok(users.Select(u => new { u.Id, u.Username, u.Email }));
});

app.MapGet("/{id:guid}", async (Guid id, UserService.Infrastructure.Services.UserService service) =>
{
    var user = await service.GetUserByIdAsync(id);
    return user is not null
        ? Results.Ok(new { user.Id, user.Username, user.Email })
        : Results.NotFound();
});

app.MapPost("/", async (User user, UserService.Infrastructure.Services.UserService service) =>
{
    var created = await service.CreateUserAsync(user);
    return Results.Created($"/{created.Id}", new { created.Id, created.Username, created.Email });
});

// =======================
// AUTH ENDPOINTS (delegate to UserService)
// =======================
app.MapPost("/auth/register", async (RegisterRequest request, UserService.Infrastructure.Services.UserService service) =>
{
    var result = await service.RegisterAsync(request);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

app.MapPost("/auth/login", async (LoginRequest request, UserService.Infrastructure.Services.UserService service) =>
{
    var result = await service.LoginAsync(request);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
});

app.MapPost("/auth/refresh", async (RefreshRequest request, UserService.Infrastructure.Services.UserService service) =>
{
    var result = await service.RefreshTokenAsync(request);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
});

app.MapPost("/auth/logout", async (RefreshRequest request, UserService.Infrastructure.Services.UserService service) =>
{
    var result = await service.LogoutAsync(request);
    return result.IsSuccess ? Results.Ok() : Results.NotFound();
});

app.MapGet("/me", async (ClaimsPrincipal user, UserService.Infrastructure.Services.UserService service) =>
{
    var result = await service.GetCurrentUserAsync(user);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
}).RequireAuthorization();

app.Run();

