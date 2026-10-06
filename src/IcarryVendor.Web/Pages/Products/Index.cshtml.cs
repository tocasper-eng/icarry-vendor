using IcarryVendor.Web.Auth;
using IcarryVendor.Web.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IcarryVendor.Web.Pages.Products;

public class IndexModel(ProductListQuery products) : PageModel
{
    /// <summary>product.status; 2 is the legacy alias for 建議補貨, kept so old bookmarks still work.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Status { get; set; }

    /// <summary>"restock" (建議補貨) or "out-of-stock" (待補貨).</summary>
    [BindProperty(SupportsGet = true)]
    public string? View { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public ProductListFilter Filter { get; private set; } = ProductListFilter.ForStatus(ProductStatus.OnShelf);
    public ProductPage Result { get; private set; } = null!;
    public string Heading { get; private set; } = "";
    public string Hint { get; private set; } = "";

    public async Task OnGetAsync()
    {
        Filter = (View, Status) switch
        {
            ("restock", _) or (_, 2) => ProductListFilter.NeedsRestock,
            ("out-of-stock", _) => ProductListFilter.OutOfStock,
            (_, { } s) => ProductListFilter.ForStatus(s),
            _ => ProductListFilter.ForStatus(ProductStatus.OnShelf),
        };

        (Heading, Hint) = Filter.View switch
        {
            ProductListView.NeedsRestock => ("建議補貨", "庫存量低於建議庫存時會顯示於此"),
            ProductListView.OutOfStock => ("待補貨", "商品庫存量小於等於0"),
            _ => Filter.Status switch
            {
                ProductStatus.OffShelf => ("下架中", "下架中商品可編輯，然後重新送審"),
                ProductStatus.Paused => ("停售中", "暫時停止銷售中"),
                ProductStatus.Rejected => ("審核不通過", "審核不通過商品可編輯"),
                ProductStatus.Draft => ("未送審-草稿", "尚未送審-草稿中商品可編輯"),
                ProductStatus.InReview => ("送審中", "送審中商品仍可編輯"),
                ProductStatus.OnShelf => ("上架中", "上架中商品如欲編輯，需先下架，下架編輯後，然後重新送審"),
                _ => ("", ""),
            },
        };

        var vendor = VendorUser.FromPrincipal(User)!;
        Result = await products.GetPageAsync(vendor.VendorId, Filter, PageNumber);
    }

    public object PageRoute(int page) => new { status = Status, view = View, page };
}
