using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yarp.ReverseProxy;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Enable detailed logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Get JWT settings from configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Fail fast if JWT key missing
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("Jwt:Key configuration is required.");

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
                var reqLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                reqLogger.LogDebug("🔑 Token received (truncated): {TokenPreview}", token?.Substring(0, Math.Min(50, token?.Length ?? 0)));
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var reqLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                reqLogger.LogInformation("✅ Token validated successfully");
                var claims = context.Principal?.Claims?.Select(c => $"{c.Type}: {c.Value}");
                reqLogger.LogDebug("✅ Claims: {Claims}", string.Join(", ", claims ?? new string[0]));
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var reqLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                reqLogger.LogError(context.Exception, "❌ Authentication failed");

                if (context.Exception.InnerException != null)
                {
                    reqLogger.LogError(context.Exception.InnerException, "❌ Inner exception");
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

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ApiGatewayStartup");
logger.LogInformation("🔧 JWT Key set={HasKey}", !string.IsNullOrEmpty(jwtKey));
logger.LogInformation("🔧 JWT Issuer set={HasIssuer}", !string.IsNullOrEmpty(jwtIssuer));
logger.LogInformation("🔧 JWT Audience set={HasAudience}", !string.IsNullOrEmpty(jwtAudience));

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
    logger.LogInformation("📥 {Method} {Path}", context.Request.Method, context.Request.Path);
    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
    if (!string.IsNullOrEmpty(authHeader))
    {
        logger.LogDebug("📥 Auth header: {AuthPreview}", authHeader.Substring(0, Math.Min(30, authHeader.Length)));
    }

    await next();

    logger.LogInformation("📤 Response: {StatusCode}", context.Response.StatusCode);
});

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();