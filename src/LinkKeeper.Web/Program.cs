using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LinkKeeper.Core;
using LinkKeeper.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var password = builder.Configuration["LINKKEEPER_PASSWORD"];
if (string.IsNullOrWhiteSpace(password) || password.Length < 16)
    throw new InvalidOperationException("Configure LINKKEEPER_PASSWORD com pelo menos 16 caracteres.");
var database = builder.Configuration["LINKKEEPER_DB"] ?? "linkkeeper.db";
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16384);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton(new CategoryCatalog(builder.Configuration.GetSection("Categories").Get<CategoryRule[]>()));
builder.Services.AddSingleton(service => new LinkStore(database, service.GetRequiredService<TimeProvider>(), service.GetRequiredService<CategoryCatalog>()));
builder.Services.AddHostedService<ExpirationWorker>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "LinkKeeper.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 10; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 120; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0;
    });
});
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
    if (context.Request.Path.StartsWithSegments("/api")) context.Response.Headers.CacheControl = "no-store";
    try { await next(); }
    catch (ArgumentException error) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { error = error.Message }); }
    catch (Microsoft.Data.Sqlite.SqliteException) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { error = "Banco temporariamente indisponível. Tente novamente." }); }
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/csrf", (HttpContext context, Microsoft.AspNetCore.Antiforgery.IAntiforgery csrf) =>
    Results.Ok(new { token = csrf.GetAndStoreTokens(context).RequestToken }));
async Task<bool> Verify(HttpContext context, Microsoft.AspNetCore.Antiforgery.IAntiforgery csrf)
{
    try { await csrf.ValidateRequestAsync(context); return true; }
    catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException) { return false; }
}
app.MapPost("/api/login", async (Login input, HttpContext context, Microsoft.AspNetCore.Antiforgery.IAntiforgery csrf) =>
{
    if (!await Verify(context, csrf)) return Results.BadRequest(new { error = "Sessão inválida. Recarregue a página." });
    var actual = SHA256.HashData(Encoding.UTF8.GetBytes(input.Password ?? ""));
    var expected = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    if (!CryptographicOperations.FixedTimeEquals(actual, expected)) return Results.Unauthorized();
    var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "personal") }, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Ok(new { signedIn = true });
}).RequireRateLimiting("login");
var api = app.MapGroup("/api/links").RequireAuthorization().RequireRateLimiting("api");
api.AddEndpointFilter(async (invocation, next) =>
{
    if (!HttpMethods.IsGet(invocation.HttpContext.Request.Method))
    {
        var csrf = invocation.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
        if (!await Verify(invocation.HttpContext, csrf)) return Results.BadRequest(new { error = "Sessão inválida. Recarregue a página." });
    }
    return await next(invocation);
});
string Owner(HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
api.MapGet("/", (HttpContext context, LinkStore store, string? status, string? category, string? q) =>
    store.List(Owner(context), status ?? "Active", category, q));
api.MapGet("/{id}", (string id, HttpContext context, LinkStore store) =>
    store.Get(Owner(context), id) is { } item ? Results.Ok(item) : Results.NotFound());
api.MapPost("/", (CreateLink input, HttpContext context, LinkStore store, TimeProvider clock, CategoryCatalog categories) =>
{
    var parsed = new CommandParser(clock, categories).Parse(input.Command);
    var result = store.Add(Owner(context), parsed, input.Description ?? "");
    return Results.Json(new { item = result.Item, created = result.Created }, statusCode: result.Created ? 201 : 200);
});
api.MapPost("/{id}/archive", (string id, HttpContext context, LinkStore store) =>
    store.Archive(Owner(context), id) ? Results.NoContent() : Results.NotFound());
api.MapPost("/{id}/renew", (string id, RenewLink input, HttpContext context, LinkStore store) =>
    store.Renew(Owner(context), id, input.Days) ? Results.NoContent() : Results.Conflict(new { error = "O link precisa estar expirado para renovar." }));
app.MapPost("/api/logout", async (HttpContext context, Microsoft.AspNetCore.Antiforgery.IAntiforgery csrf) =>
{
    if (!await Verify(context, csrf)) return Results.BadRequest();
    await context.SignOutAsync(); return Results.NoContent();
}).RequireAuthorization();
app.Run();

public sealed record Login(string? Password);
public sealed record CreateLink(string Command, string? Description);
public sealed record RenewLink(int Days);
public sealed class ExpirationWorker(LinkStore store, ILogger<ExpirationWorker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = Math.Clamp(configuration.GetValue("ExpirationSeconds", 60), 5, 3600);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
        do
        {
            try { var count = store.Expire(); if (count > 0) logger.LogInformation("Expired {Count} links", count); }
            catch (Microsoft.Data.Sqlite.SqliteException) { logger.LogWarning("Expiration deferred: persistence unavailable"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
