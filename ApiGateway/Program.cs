using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// Enable detailed logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Get JWT settings from configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Debug: Log the configuration values
Console.WriteLine($"🔧 JWT Key: {jwtKey}");
Console.WriteLine($"🔧 JWT Issuer: {jwtIssuer}");
Console.WriteLine($"🔧 JWT Audience: {jwtAudience}");

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
                Console.WriteLine($"🔑 Token received: {token?.Substring(0, Math.Min(50, token?.Length ?? 0))}...");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("✅ Token validated successfully");
                var claims = context.Principal?.Claims?.Select(c => $"{c.Type}: {c.Value}");
                Console.WriteLine($"✅ Claims: {string.Join(", ", claims ?? new string[0])}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ Authentication failed: {context.Exception.Message}");
                Console.WriteLine($"❌ Exception type: {context.Exception.GetType().Name}");

                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner exception: {context.Exception.InnerException.Message}");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5022") // Blazor WASM
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

// Handle preflight requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.CompleteAsync();
    }
    else
    {
        await next();
    }
});

// Debug middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"📥 {context.Request.Method} {context.Request.Path}");
    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
    if (!string.IsNullOrEmpty(authHeader))
    {
        Console.WriteLine($"📥 Auth header: {authHeader.Substring(0, Math.Min(30, authHeader.Length))}...");
    }

    await next();

    Console.WriteLine($"📤 Response: {context.Response.StatusCode}");
});

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();