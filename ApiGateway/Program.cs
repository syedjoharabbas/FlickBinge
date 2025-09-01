using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);


var proxy = builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.Run();
