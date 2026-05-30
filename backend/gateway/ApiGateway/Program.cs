using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [
        "http://localhost:5173",
        "http://127.0.0.1:5173",
    ];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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

app.UseCors("FrontendCors");

app.MapHealthChecks("/health");
app.MapReverseProxy();
app.Run();