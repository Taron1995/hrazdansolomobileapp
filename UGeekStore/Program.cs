using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Api;
using UGeekStore.Data;
using UGeekStore.Hubs;
using UGeekStore.Services;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(cfg.GetConnectionString("Default") ?? "Data Source=ugeekstore.db"));

var auth = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/Denied";
        o.ExpireTimeSpan = TimeSpan.FromDays(30);
        o.SlidingExpiration = true;
    })
    .AddCookie(AuthSchemes.External);

// Sign in with Gmail (Google account). Enabled when Authentication:Google:ClientId/ClientSecret are configured.
if (!string.IsNullOrEmpty(cfg["Authentication:Google:ClientId"]))
{
    auth.AddGoogle(o =>
    {
        o.ClientId = cfg["Authentication:Google:ClientId"]!;
        o.ClientSecret = cfg["Authentication:Google:ClientSecret"]!;
        o.SignInScheme = AuthSchemes.External;
    });
}

builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AllowAnonymousToFolder("/Account");
});
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Clock>();
builder.Services.AddSingleton<GoogleSheetsService>();
builder.Services.AddSingleton<FileStorage>();
builder.Services.AddScoped<MessagingService>();
builder.Services.AddScoped<WorkloadService>();
builder.Services.AddScoped<CashService>();

// Allow video reports from phones (cleaning checklist).
const long maxUpload = 300L * 1024 * 1024;
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = maxUpload);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxUpload);

var app = builder.Build();

await Seed.RunAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<ChatHub>("/hubs/chat");
app.MapApi();

app.Run();

public static class AuthSchemes
{
    public const string External = "External";
}
