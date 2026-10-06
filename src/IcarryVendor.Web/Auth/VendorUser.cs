using System.Globalization;
using System.Security.Claims;

namespace IcarryVendor.Web.Auth;

/// <summary>The signed-in vendor account. Replaces the legacy $_SESSION['vendor_*'] values.</summary>
public sealed record VendorUser(
    int AccountId,
    string Account,
    string Name,
    int VendorId,
    string VendorName,
    int? ShopId,
    bool ShippingSelf)
{
    private const string AccountClaim = "vendor:account";
    private const string VendorIdClaim = "vendor:id";
    private const string VendorNameClaim = "vendor:name";
    private const string ShopIdClaim = "vendor:shop_id";
    private const string ShippingSelfClaim = "vendor:shipping_self";

    public ClaimsPrincipal ToPrincipal(string authenticationType)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, AccountId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, Name),
            new(AccountClaim, Account),
            new(VendorIdClaim, VendorId.ToString(CultureInfo.InvariantCulture)),
            new(VendorNameClaim, VendorName),
            new(ShippingSelfClaim, ShippingSelf ? "1" : "0"),
        };
        if (ShopId is { } shopId)
        {
            claims.Add(new Claim(ShopIdClaim, shopId.ToString(CultureInfo.InvariantCulture)));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    public static VendorUser? FromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true
            || !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId)
            || !int.TryParse(principal.FindFirstValue(VendorIdClaim), out var vendorId))
        {
            return null;
        }

        int? shopId = int.TryParse(principal.FindFirstValue(ShopIdClaim), out var s) ? s : null;
        return new VendorUser(
            accountId,
            principal.FindFirstValue(AccountClaim) ?? "",
            principal.FindFirstValue(ClaimTypes.Name) ?? "",
            vendorId,
            principal.FindFirstValue(VendorNameClaim) ?? "",
            shopId,
            principal.FindFirstValue(ShippingSelfClaim) == "1");
    }
}
