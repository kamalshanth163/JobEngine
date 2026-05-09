using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// YARP reads routes + clusters from appsettings.json ReverseProxy section
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

// Inject correlation ID on every inbound request
// All 6 services log this ID — lets you trace one request across services
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Headers.ContainsKey("X-Correlation-Id"))
        ctx.Request.Headers["X-Correlation-Id"] = Guid.NewGuid().ToString("N");
    await next();
});

app.MapHealthChecks("/health");
app.MapReverseProxy();
app.Run();