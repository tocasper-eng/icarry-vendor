using IcarryVendor.Web.Auth;
using IcarryVendor.Web.Products;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IcarryVendor.Web.Pages;

public class IndexModel(ProductListQuery products) : PageModel
{
    public VendorUser CurrentUser { get; private set; } = null!;
    public ProductStatusCounts Counts { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        CurrentUser = VendorUser.FromPrincipal(User)!;
        Counts = await products.GetStatusCountsAsync(CurrentUser.VendorId);
    }
}
