using Microsoft.EntityFrameworkCore;
using WatchlistService.Core.Interfaces;
using WatchlistService.Infrastructure.DBContext;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<WatchlistDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WatchlistConnection")));
builder.Services.AddScoped<IWatchlistService, WatchlistService.Infrastructure.Services.WatchlistService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
