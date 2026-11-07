using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using IndiaFinanceAI.Application.Leads;
using IndiaFinanceAI.Application.WhatsApp;
using IndiaFinanceAI.Infrastructure.Persistence;
using IndiaFinanceAI.Infrastructure.WhatsApp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FinanceDatabase")
    ?? throw new InvalidOperationException("Configure ConnectionStrings__FinanceDatabase through environment secrets.");
var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Configure Jwt__SigningKey through environment secrets.");
var signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);
if (signingKeyBytes.Length < 32)
{
    throw new InvalidOperationException("Jwt__SigningKey must contain at least 32 UTF-8 bytes.");
}

var adminUsername = builder.Configuration["Admin:Username"];
var adminPassword = builder.Configuration["Admin:Password"];
if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrEmpty(adminPassword) || adminPassword.Length < 16)
{
    throw new InvalidOperationException("Configure Admin__Username and an Admin__Password of at least 16 characters through environment secrets.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "IndiaFinanceAI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "IndiaFinanceAI.Admin";
var allowedHosts = builder.Configuration["AllowedHosts"];
if (!builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Split(';').Any(host => host.Trim() == "*")))
{
    throw new InvalidOperationException("Configure AllowedHosts with the production API host names; wildcard hosts are not allowed in production.");
}

var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
foreach (var origin in allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var parsedOrigin)
        || parsedOrigin.AbsolutePath != "/"
        || parsedOrigin.Query.Length > 0
        || parsedOrigin.Fragment.Length > 0
        || parsedOrigin.Host == "*"
        || (!builder.Environment.IsDevelopment() && parsedOrigin.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException("Cors__AllowedOrigins must contain exact origins; production origins must use HTTPS and cannot contain paths.");
    }
}

var trustedProxyAddresses = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
if (!builder.Environment.IsDevelopment() && trustedProxyAddresses.Length == 0)
{
    throw new InvalidOperationException("Configure ForwardedHeaders__KnownProxies with the exact production proxy IPs so HTTPS forwarding is trusted safely.");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    foreach (var address in trustedProxyAddresses)
    {
        if (!IPAddress.TryParse(address, out var parsedAddress))
        {
            throw new InvalidOperationException("ForwardedHeaders__KnownProxies must contain valid IP addresses.");
        }
        options.KnownProxies.Add(parsedAddress);
    }
});

builder.Services.AddDbContext<FinanceDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ICustomerLeadRepository, CustomerLeadRepository>();
builder.Services.AddScoped<ICustomerLeadService, CustomerLeadService>();
builder.Services.Configure<WhatsAppCloudOptions>(builder.Configuration.GetSection("WhatsApp"));
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<HstsOptions>(options => options.MaxAge = TimeSpan.FromDays(365));
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<HttpsRedirectionOptions>(options =>
    {
        options.HttpsPort = builder.Configuration.GetValue<int?>("HttpsRedirection:HttpsPort") ?? 443;
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    });
}
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
}
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.AddPolicy("public-leads", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.AddPolicy("whatsapp-admin", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.AddPolicy("whatsapp-webhook", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
});

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
if (allowedOrigins.Length > 0)
{
    app.UseCors("Frontend");
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (FinanceDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
