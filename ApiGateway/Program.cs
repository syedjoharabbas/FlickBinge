using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);


var proxy = builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7011")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();

app.MapReverseProxy();
app.UseCors();
app.Run();
