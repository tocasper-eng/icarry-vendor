using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IcarryVendor.Tests;

public sealed class VendorWebFactory : WebApplicationFactory<Program>
{
    private readonly TestDatabase _db = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:ConnectionString", _db.ConnectionString);
        builder.UseSetting("Database:SeedDevelopmentData", "false"); // TestDatabase already loaded the sample data
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _db.Dispose();
        }
    }
}

public class WebAppTests(VendorWebFactory factory) : IClassFixture<VendorWebFactory>
{
    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/")]
    [InlineData("/Products?status=1")]
    [InlineData("/Account/ChangePassword")]
    public async Task ProtectedPages_RedirectAnonymousUsersToLogin(string url)
    {
        var response = await NewClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_ThenProductList_ShowsOnlyTheVendorsProducts()
    {
        var client = NewClient();
        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = Regex.Match(loginPage, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Account"] = "demo",
            ["Password"] = "demo1234",
            ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location!.ToString());

        var list = await client.GetStringAsync("/Products?status=1");
        Assert.Contains("鳳梨酥禮盒", list);
        Assert.Contains("太陽餅", list);
        Assert.DoesNotContain("其他商家商品", list);
        Assert.DoesNotContain("已刪除商品", list);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsLegacyMessage()
    {
        var client = NewClient();
        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = Regex.Match(loginPage, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Account"] = "demo",
            ["Password"] = "wrong",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("帳號密碼錯誤！", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LoginPageStylesheets_AreServedToAnonymousUsers()
    {
        var client = NewClient();
        var loginPage = await client.GetStringAsync("/Account/Login");
        var stylesheets = Regex.Matches(loginPage, "<link rel=\"stylesheet\" href=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

        Assert.NotEmpty(stylesheets);
        foreach (var href in stylesheets)
        {
            var response = await client.GetAsync(href);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Logout_RequiresPost()
    {
        var response = await NewClient().GetAsync("/Account/Logout");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.ToString());
    }
}
