using Microsoft.EntityFrameworkCore;
using UserService.Core.Entities;
using UserService.Core.Interfaces;
using UserService.Infrastructure.DBContext;
using UserService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService.Infrastructure.Services.UserService>();

var app = builder.Build();

app.UseHttpsRedirection();


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
    return Results.Created($"/api/users/{created.Id}", new { created.Id, created.Username, created.Email });
});

app.MapPut("/{id:guid}", async (Guid id, User user, UserService.Infrastructure.Services.UserService service) =>
{
    var updated = await service.UpdateUserAsync(id, user);
    return updated is not null
        ? Results.Ok(new { updated.Id, updated.Username, updated.Email })
        : Results.NotFound();
});

app.MapDelete("/{id:guid}", async (Guid id, UserService.Infrastructure.Services.UserService service) =>
{
    var deleted = await service.DeleteUserAsync(id);
    return deleted
        ? Results.NoContent()
        : Results.NotFound();
});

app.Run();
