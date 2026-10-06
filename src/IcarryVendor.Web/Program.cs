using IcarryVendor.Web.Auth;
using IcarryVendor.Web.Data;
using IcarryVendor.Web.Products;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
if (databaseOptions.Provider == DatabaseProvider.Sqlite)
{
    // Resolve a relative Sqlite file against the project folder, not the shell's current directory.
    var sqlite = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(databaseOptions.ConnectionString);
    if (!sqlite.DataSource.StartsWith(':') && !sqlite.DataSource.StartsWith("file:") && !Path.IsPathRooted(sqlite.DataSource))
    {
        sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);
        Directory.CreateDirectory(Path.GetDirectoryName(sqlite.DataSource)!);
        databaseOptions.ConnectionString = sqlite.ConnectionString;
    }
}
builder.Services.AddSingleton(databaseOptions);
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<VendorAuthService>();
builder.Services.AddScoped<ProductListQuery>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.Name = "icarry_vendor";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });

// Every page requires a signed-in vendor unless it opts out with [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddRazorPages();

// Write Chinese text as-is instead of &#x...; entities (still HTML-encodes <, >, &, quotes).
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options =>
    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

var app = builder.Build();

if (databaseOptions.Provider == DatabaseProvider.Sqlite)
{
    DevDatabase.EnsureCreated(app.Services.GetRequiredService<IDbConnectionFactory>(), databaseOptions.SeedDevelopmentData);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Static files are endpoints too, so the fallback policy would hide the login page's CSS from anonymous users.
app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Exposed for integration tests.
public partial class Program;
