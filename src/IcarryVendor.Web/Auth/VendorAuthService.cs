using Dapper;
using IcarryVendor.Web.Data;

namespace IcarryVendor.Web.Auth;

public enum SignInStatus
{
    Success,
    InvalidCredentials,
    AccountDisabled,
    NoShopPermission,
    VendorDisabled,
}

public sealed record SignInResult(SignInStatus Status, VendorUser? User = null);

public enum ChangePasswordStatus
{
    Success,
    WrongCurrentPassword,
    AccountNotFound,
}

/// <summary>Login and password change for vendor_account (legacy action/login.php and self_edit.php).</summary>
public sealed class VendorAuthService(IDbConnectionFactory db)
{
    // Settable properties (not a positional record) so Dapper can convert column types, e.g. TINYINT/BIGINT to int.
    private sealed class AccountRow
    {
        public int Id { get; set; }
        public string Account { get; set; } = "";
        public string? Name { get; set; }
        public string? Pwd { get; set; }
        public int? VendorId { get; set; }
        public int? ShopId { get; set; }
        public int ShopAdmin { get; set; }
        public int IsOn { get; set; }
        public string? VendorName { get; set; }
        public int? VendorIsOn { get; set; }
        public int? ShippingSelf { get; set; }
    }

    public async Task<SignInResult> SignInAsync(string account, string password)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrEmpty(password))
        {
            return new SignInResult(SignInStatus.InvalidCredentials);
        }

        await using var connection = db.Create();
        var rows = await connection.QueryAsync<AccountRow>(
            """
            SELECT a.id AS Id, a.account AS Account, a.name AS Name, a.pwd AS Pwd,
                   a.vendor_id AS VendorId, a.shop_id AS ShopId, a.shop_admin AS ShopAdmin, a.is_on AS IsOn,
                   v.name AS VendorName, v.is_on AS VendorIsOn, v.shipping_self AS ShippingSelf
            FROM vendor_account a
            LEFT JOIN vendor v ON v.id = a.vendor_id
            WHERE a.account = @account
            ORDER BY a.id
            """,
            new { account = account.Trim() });

        // The legacy query matched account AND pwd together, so with duplicate account names the row whose
        // password matches wins. Keep that behaviour.
        var row = rows.FirstOrDefault(r => LegacyPasswordHasher.Verify(password, r.Pwd));
        if (row is null)
        {
            return new SignInResult(SignInStatus.InvalidCredentials);
        }
        if (row.IsOn != 1)
        {
            return new SignInResult(SignInStatus.AccountDisabled);
        }
        if (row.ShopAdmin != 1)
        {
            return new SignInResult(SignInStatus.NoShopPermission);
        }
        if (row.VendorId is null || row.VendorIsOn != 1)
        {
            return new SignInResult(SignInStatus.VendorDisabled);
        }

        var user = new VendorUser(
            row.Id,
            row.Account,
            row.Name ?? row.Account,
            row.VendorId.Value,
            row.VendorName ?? "",
            row.ShopId,
            row.ShippingSelf == 1);
        return new SignInResult(SignInStatus.Success, user);
    }

    public async Task<ChangePasswordStatus> ChangePasswordAsync(int accountId, string currentPassword, string newPassword)
    {
        await using var connection = db.Create();
        var stored = await connection.QuerySingleOrDefaultAsync<string?>(
            "SELECT pwd FROM vendor_account WHERE id = @accountId",
            new { accountId });
        if (stored is null)
        {
            return ChangePasswordStatus.AccountNotFound;
        }
        if (!LegacyPasswordHasher.Verify(currentPassword, stored))
        {
            return ChangePasswordStatus.WrongCurrentPassword;
        }

        await connection.ExecuteAsync(
            "UPDATE vendor_account SET pwd = @pwd WHERE id = @accountId",
            new { pwd = LegacyPasswordHasher.Hash(newPassword), accountId });
        return ChangePasswordStatus.Success;
    }
}
