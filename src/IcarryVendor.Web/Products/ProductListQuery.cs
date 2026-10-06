using System.Globalization;
using System.Text.Json;
using Dapper;
using IcarryVendor.Web.Data;

namespace IcarryVendor.Web.Products;

public sealed class ProductModelRow
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public int SafeQuantity { get; set; }
}

public sealed class ProductRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? ServingSize { get; set; }
    public string? ModelName { get; set; }
    public int Status { get; set; }
    public decimal Price { get; set; }
    public decimal ServiceFeePercent { get; set; }
    public string? ParcelTaxCode { get; set; }
    public string? CategoryName { get; set; }
    public string? VendorName { get; set; }
    public string? VendorServiceFee { get; set; }

    public List<ProductModelRow> Models { get; } = [];
    public ParcelTaxCode? ParcelTax { get; set; }

    /// <summary>Platform fees from vendor.service_fee plus this product's adjustment (product.service_fee_percent).</summary>
    public IReadOnlyList<ServiceFee> ServiceFees => ServiceFee.Parse(VendorServiceFee, ServiceFeePercent, Price);
}

public sealed class ParcelTaxCode
{
    public string Hscode { get; set; } = "";
    public string? Name { get; set; }
    public string? UnitName { get; set; }
    public string? Tax { get; set; }

    /// <summary>Estimated parcel tax for one unit at the given price (legacy: tax * price * 0.01).</summary>
    public decimal? Estimate(decimal price) =>
        decimal.TryParse(Tax, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)
            ? Math.Round(rate * price / 100m, 2)
            : null;
}

public sealed record ServiceFee(string Platform, decimal BasePercent, decimal Adjustment, decimal Amount)
{
    public decimal Percent => BasePercent + Adjustment;

    /// <summary>vendor.service_fee is a JSON array like [{"name":"iCarry","percent":20}]. Bad JSON yields no fees.</summary>
    public static IReadOnlyList<ServiceFee> Parse(string? json, decimal adjustment, decimal price)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            // Legacy data sometimes has an empty percent ("percent":}); the PHP code patched it the same way.
            using var doc = JsonDocument.Parse(json.Replace("\"percent\":}", "\"percent\":0}"));
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var fees = new List<ServiceFee>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("name", out var name))
                {
                    continue;
                }

                var percent = 0m;
                if (item.TryGetProperty("percent", out var p))
                {
                    percent = p.ValueKind switch
                    {
                        JsonValueKind.Number => p.GetDecimal(),
                        JsonValueKind.String when decimal.TryParse(p.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) => v,
                        _ => 0m,
                    };
                }

                var amount = Math.Round((percent + adjustment) * price / 100m, 2);
                fees.Add(new ServiceFee(name.ToString(), percent, adjustment, amount));
            }
            return fees;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

public sealed record ProductPage(IReadOnlyList<ProductRow> Items, int Total, int Page, int PageSize)
{
    public int PageCount => Total == 0 ? 1 : (Total + PageSize - 1) / PageSize;
}

public sealed record ProductStatusCounts(IReadOnlyDictionary<int, int> ByStatus, int NeedsRestock, int OutOfStock)
{
    public int Count(int status) => ByStatus.TryGetValue(status, out var n) ? n : 0;
}

/// <summary>Product list for one vendor (legacy product.php). Every query is scoped to the vendor.</summary>
public sealed class ProductListQuery(IDbConnectionFactory db)
{
    private sealed class StatusCount
    {
        public int Status { get; set; }
        public int Count { get; set; }
    }

    public const int DefaultPageSize = 50;

    private const string NeedsRestockCondition =
        "p.status = 1 AND p.id IN (SELECT m.product_id FROM product_model m WHERE m.is_del = 0 AND m.quantity <= m.safe_quantity)";

    private const string OutOfStockCondition =
        "p.id IN (SELECT m.product_id FROM product_model m WHERE m.is_del = 0 AND m.quantity <= 0)";

    public async Task<ProductPage> GetPageAsync(int vendorId, ProductListFilter filter, int page, int pageSize = DefaultPageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var where = "p.is_del = 0 AND p.vendor_id = @vendorId AND " + filter.View switch
        {
            ProductListView.NeedsRestock => NeedsRestockCondition,
            ProductListView.OutOfStock => OutOfStockCondition,
            _ => "p.status = @status",
        };
        var args = new { vendorId, status = filter.Status, take = pageSize, skip = (page - 1) * pageSize };

        await using var connection = db.Create();
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM product p WHERE {where}", args);

        var items = (await connection.QueryAsync<ProductRow>(
            $"""
            SELECT p.id AS Id, p.name AS Name, p.serving_size AS ServingSize, p.model_name AS ModelName,
                   p.status AS Status, p.price AS Price, p.service_fee_percent AS ServiceFeePercent,
                   p.parcel_tax_code AS ParcelTaxCode, c.name AS CategoryName,
                   v.name AS VendorName, v.service_fee AS VendorServiceFee
            FROM product p
            LEFT JOIN vendor v ON v.id = p.vendor_id
            LEFT JOIN category c ON c.id = p.category_id
            WHERE {where}
            ORDER BY p.id DESC
            LIMIT @take OFFSET @skip
            """,
            args)).ToList();

        if (items.Count > 0)
        {
            var ids = items.Select(p => p.Id).ToArray();
            var models = await connection.QueryAsync<ProductModelRow>(
                """
                SELECT id AS Id, product_id AS ProductId, name AS Name, sku AS Sku,
                       quantity AS Quantity, safe_quantity AS SafeQuantity
                FROM product_model
                WHERE is_del = 0 AND product_id IN @ids
                ORDER BY id
                """,
                new { ids });
            var byProduct = items.ToDictionary(p => p.Id);
            foreach (var m in models)
            {
                byProduct[m.ProductId].Models.Add(m);
            }

            var codes = items.Select(p => p.ParcelTaxCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
            if (codes.Length > 0)
            {
                var taxes = (await connection.QueryAsync<ParcelTaxCode>(
                    "SELECT hscode AS Hscode, name AS Name, unit_name AS UnitName, tax AS Tax FROM parcel_tax_code WHERE hscode IN @codes",
                    new { codes }))
                    .GroupBy(t => t.Hscode)
                    .ToDictionary(g => g.Key, g => g.First());
                foreach (var item in items)
                {
                    item.ParcelTax = item.ParcelTaxCode is { } code && taxes.TryGetValue(code, out var t) ? t : null;
                }
            }
        }

        return new ProductPage(items, total, page, pageSize);
    }

    public async Task<ProductStatusCounts> GetStatusCountsAsync(int vendorId)
    {
        await using var connection = db.Create();
        var byStatus = (await connection.QueryAsync<StatusCount>(
                "SELECT status AS Status, COUNT(*) AS Count FROM product WHERE is_del = 0 AND vendor_id = @vendorId GROUP BY status",
                new { vendorId }))
            .ToDictionary(r => r.Status, r => r.Count);
        var needsRestock = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM product p WHERE p.is_del = 0 AND p.vendor_id = @vendorId AND {NeedsRestockCondition}",
            new { vendorId });
        var outOfStock = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM product p WHERE p.is_del = 0 AND p.vendor_id = @vendorId AND {OutOfStockCondition}",
            new { vendorId });
        return new ProductStatusCounts(byStatus, needsRestock, outOfStock);
    }
}
