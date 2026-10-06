using IcarryVendor.Web.Products;

namespace IcarryVendor.Tests;

public class ProductListQueryTests : IDisposable
{
    private const int DemoVendor = 1;
    private const int OtherVendor = 2;

    private readonly TestDatabase _db = new();
    private readonly ProductListQuery _query;

    public ProductListQueryTests() => _query = new ProductListQuery(_db.Factory);

    public void Dispose() => _db.Dispose();

    private async Task<int[]> Ids(int vendorId, ProductListFilter filter) =>
        (await _query.GetPageAsync(vendorId, filter, 1)).Items.Select(p => p.Id).ToArray();

    [Theory]
    [InlineData(ProductStatus.OnShelf, new[] { 102, 101 })]
    [InlineData(ProductStatus.Paused, new[] { 103 })]
    [InlineData(ProductStatus.InReview, new[] { 104 })]
    [InlineData(ProductStatus.Draft, new[] { 105 })]
    [InlineData(ProductStatus.Rejected, new[] { 106 })]
    [InlineData(ProductStatus.OffShelf, new[] { 107 })]
    public async Task ByStatus_ReturnsVendorsUndeletedProducts_NewestFirst(int status, int[] expected)
    {
        Assert.Equal(expected, await Ids(DemoVendor, ProductListFilter.ForStatus(status)));
    }

    [Fact]
    public async Task NeedsRestock_IsOnShelfWithAModelAtOrBelowSafeQuantity()
    {
        // 101: 蔓越莓 15 <= 20; 102: 0 <= 10. 103 is low too but paused, so excluded.
        Assert.Equal(new[] { 102, 101 }, await Ids(DemoVendor, ProductListFilter.NeedsRestock));
    }

    [Fact]
    public async Task OutOfStock_IgnoresDeletedModelsAndDeletedProducts()
    {
        // 107 has a deleted model with 0 stock; 108 is a deleted product with 0 stock. Only 102 counts.
        Assert.Equal(new[] { 102 }, await Ids(DemoVendor, ProductListFilter.OutOfStock));
    }

    [Fact]
    public async Task Queries_NeverReturnAnotherVendorsProducts()
    {
        Assert.Equal(new[] { 201 }, await Ids(OtherVendor, ProductListFilter.ForStatus(ProductStatus.OnShelf)));
        Assert.DoesNotContain(201, await Ids(DemoVendor, ProductListFilter.OutOfStock));
    }

    [Fact]
    public async Task Page_AttachesModelsTaxCodeAndServiceFees()
    {
        var page = await _query.GetPageAsync(DemoVendor, ProductListFilter.ForStatus(ProductStatus.OnShelf), 1);
        var product = page.Items.Single(p => p.Id == 101);

        Assert.Equal(new[] { "原味", "蔓越莓" }, product.Models.Select(m => m.Name));
        Assert.Equal("糕餅點心", product.CategoryName);
        Assert.NotNull(product.ParcelTax);
        Assert.Equal(67.5m, product.ParcelTax.Estimate(product.Price)); // 15% of 450
        Assert.Equal(new[] { ("iCarry", 90m), ("momo", 112.5m) },
            product.ServiceFees.Select(f => (f.Platform, f.Amount)));
    }

    [Fact]
    public async Task Paging_SplitsResultsAndReportsTotal()
    {
        var first = await _query.GetPageAsync(DemoVendor, ProductListFilter.ForStatus(ProductStatus.OnShelf), 1, pageSize: 1);
        var second = await _query.GetPageAsync(DemoVendor, ProductListFilter.ForStatus(ProductStatus.OnShelf), 2, pageSize: 1);

        Assert.Equal(2, first.Total);
        Assert.Equal(2, first.PageCount);
        Assert.Equal(102, first.Items.Single().Id);
        Assert.Equal(101, second.Items.Single().Id);
    }

    [Fact]
    public async Task StatusCounts_MatchTheLists()
    {
        var counts = await _query.GetStatusCountsAsync(DemoVendor);

        Assert.Equal(2, counts.Count(ProductStatus.OnShelf));
        Assert.Equal(1, counts.Count(ProductStatus.Paused));
        Assert.Equal(2, counts.NeedsRestock);
        Assert.Equal(1, counts.OutOfStock);
    }
}

public class ServiceFeeTests
{
    [Fact]
    public void Parse_AppliesProductAdjustmentToEachPlatform()
    {
        var fees = ServiceFee.Parse("""[{"name":"iCarry","percent":20}]""", adjustment: 2, price: 100);

        var fee = Assert.Single(fees);
        Assert.Equal(22, fee.Percent);
        Assert.Equal(22, fee.Amount);
    }

    [Theory]
    [InlineData("""[{"name":"iCarry","percent":"18"}]""", 18)]
    [InlineData("""[{"name":"iCarry","percent":}]""", 0)]
    public void Parse_ToleratesLegacyPercentFormats(string json, decimal expectedPercent)
    {
        Assert.Equal(expectedPercent, Assert.Single(ServiceFee.Parse(json, 0, 100)).Percent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"name":"iCarry"}""")]
    public void Parse_ReturnsNothingForMissingOrInvalidData(string? json)
    {
        Assert.Empty(ServiceFee.Parse(json, 0, 100));
    }
}
