using IcarryVendor.Web.Auth;

namespace IcarryVendor.Tests;

public class LegacyPasswordHasherTests
{
    [Fact]
    public void Hash_IsLowercaseSha1Hex_LikeThePhpSha1Function()
    {
        // php -r "echo sha1('demo1234');"
        Assert.Equal("6ea7ccdcf642953a24672d10b0d32cef576e0329", LegacyPasswordHasher.Hash("demo1234"));
    }

    [Theory]
    [InlineData("6ea7ccdcf642953a24672d10b0d32cef576e0329", true)]
    [InlineData("6EA7CCDCF642953A24672D10B0D32CEF576E0329", true)]
    [InlineData("6ea7ccdcf642953a24672d10b0d32cef576e0328", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Verify_ComparesAgainstStoredHash(string? stored, bool expected)
    {
        Assert.Equal(expected, LegacyPasswordHasher.Verify("demo1234", stored));
    }

    [Fact]
    public void Verify_DoesNotAcceptTheHashItselfAsPassword()
    {
        // The legacy from_admin parameter let a caller log in with the stored hash. That path is gone.
        Assert.False(LegacyPasswordHasher.Verify("6ea7ccdcf642953a24672d10b0d32cef576e0329", "6ea7ccdcf642953a24672d10b0d32cef576e0329"));
    }
}

public class VendorAuthServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly VendorAuthService _auth;

    public VendorAuthServiceTests() => _auth = new VendorAuthService(_db.Factory);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SignIn_WithValidShopAdmin_ReturnsVendorUser()
    {
        var result = await _auth.SignInAsync("demo", "demo1234");

        Assert.Equal(SignInStatus.Success, result.Status);
        Assert.NotNull(result.User);
        Assert.Equal(1, result.User.AccountId);
        Assert.Equal(1, result.User.VendorId);
        Assert.Equal("示範伴手禮商家", result.User.VendorName);
        Assert.Equal("示範管理員", result.User.Name);
    }

    [Theory]
    [InlineData("demo", "wrong", SignInStatus.InvalidCredentials)]
    [InlineData("nobody", "demo1234", SignInStatus.InvalidCredentials)]
    [InlineData("", "demo1234", SignInStatus.InvalidCredentials)]
    [InlineData("disabled", "demo1234", SignInStatus.AccountDisabled)]
    [InlineData("posonly", "demo1234", SignInStatus.NoShopPermission)]
    [InlineData("offvendor", "demo1234", SignInStatus.VendorDisabled)]
    public async Task SignIn_RejectsWithTheLegacyReason(string account, string password, SignInStatus expected)
    {
        var result = await _auth.SignInAsync(account, password);

        Assert.Equal(expected, result.Status);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task SignIn_DoesNotRevealDisabledAccountWithoutCorrectPassword()
    {
        var result = await _auth.SignInAsync("disabled", "wrong");

        Assert.Equal(SignInStatus.InvalidCredentials, result.Status);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_KeepsOldPassword()
    {
        var status = await _auth.ChangePasswordAsync(1, "wrong", "newpass99");

        Assert.Equal(ChangePasswordStatus.WrongCurrentPassword, status);
        Assert.Equal(SignInStatus.Success, (await _auth.SignInAsync("demo", "demo1234")).Status);
    }

    [Fact]
    public async Task ChangePassword_StoresNewSha1Password()
    {
        var status = await _auth.ChangePasswordAsync(1, "demo1234", "newpass99");

        Assert.Equal(ChangePasswordStatus.Success, status);
        Assert.Equal(SignInStatus.InvalidCredentials, (await _auth.SignInAsync("demo", "demo1234")).Status);
        Assert.Equal(SignInStatus.Success, (await _auth.SignInAsync("demo", "newpass99")).Status);
    }

    [Fact]
    public async Task ChangePassword_ForUnknownAccount_ReportsNotFound()
    {
        Assert.Equal(ChangePasswordStatus.AccountNotFound, await _auth.ChangePasswordAsync(999, "demo1234", "x"));
    }

    [Fact]
    public void VendorUser_RoundTripsThroughClaims()
    {
        var user = new VendorUser(7, "acc", "名稱", 3, "商家", 12, true);

        var restored = VendorUser.FromPrincipal(user.ToPrincipal("test"));

        Assert.Equal(user, restored);
    }
}
