namespace IcarryVendor.Web.Products;

/// <summary>Values of product.status.</summary>
public static class ProductStatus
{
    public const int OffShelf = -9;   // 下架
    public const int Paused = -3;     // 停售中
    public const int Rejected = -2;   // 審核不通過
    public const int Draft = -1;      // 未送審（草稿）
    public const int InReview = 0;    // 送審中
    public const int OnShelf = 1;     // 上架中

    public static string Label(int status) => status switch
    {
        OffShelf => "下架",
        Paused => "停售中",
        Rejected => "審核不通過",
        Draft => "未送審(草稿)",
        InReview => "送審中",
        OnShelf => "上架中",
        _ => "",
    };

    /// <summary>Bootstrap 3 label class used by the legacy list.</summary>
    public static string LabelClass(int status) => status switch
    {
        OnShelf => "label-success",
        Paused or InReview => "label-danger",
        _ => "label-default",
    };
}

/// <summary>Which products the list shows. Mirrors the legacy sidebar links of product.php.</summary>
public enum ProductListView
{
    ByStatus,      // product.php?status=N
    NeedsRestock,  // product.php?status=2 : on shelf and some model at or below its safe quantity
    OutOfStock,    // product.php?below_zero_quantity=yes : some model with quantity <= 0
}

public sealed record ProductListFilter(ProductListView View, int Status = ProductStatus.OnShelf)
{
    public static ProductListFilter ForStatus(int status) => new(ProductListView.ByStatus, status);
    public static readonly ProductListFilter NeedsRestock = new(ProductListView.NeedsRestock);
    public static readonly ProductListFilter OutOfStock = new(ProductListView.OutOfStock);
}
