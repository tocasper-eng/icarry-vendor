# iCarry Vendor 商家管理平台（.NET 版）

把舊版商家後台 Bitbucket `icarryfolks/icarry-vendor`（原生 PHP，SB Admin 2）改寫成 **ASP.NET Core 10 Razor Pages**。逐步搬移，目前完成第一階段。

> 舊版在 2024-08 已由 icarry-newvendor（Laravel）取代上線。兩者共用同一個 iCarry MySQL 資料庫。

## 進度

| 功能 | 舊版檔案 | 狀態 |
|---|---|---|
| 登入、登出 | `login.php`、`action/login.php`、`action/logout.php` | ✅ 完成 |
| 變更密碼 | `self_edit.php` | ✅ 完成 |
| 版面、選單 | `inc/*.inc.php` | ✅ 完成（未改寫的選單項目顯示「改寫中」） |
| 首頁 | `index.php` | ⚠️ 只有商品狀態統計，訂單統計等訂單管理改寫後再加 |
| 商品列表（依狀態、待補貨、建議補貨） | `product.php` | ✅ 唯讀列表完成 |
| 商品上下架、停售、複製、庫存調整 | `action/*_product.php`、`api/product_model_quantity*` | 未開始 |
| 商品編輯、所有商品、組合商品 | `product_edit.php`、`product_all.php`、`product_package.php` | 未開始 |
| 商家資料 | `vendor.php`、`vendor_edit.php` | 未開始 |
| 訂單（待出貨、已出貨、已取消、匯出 Excel） | `orders*.php`、`action/output_order*` | 未開始 |
| iCarryGo／mPOS、帳務、合作夥伴 API v1 | `machine_list*`、`mpos_record.php`、`accounting_management.php`、`api/v1` | 未開始（舊版選單已隱藏，先確認是否仍需要） |

## 結構

```
src/IcarryVendor.Web/
  Auth/       登入驗證、密碼雜湊、登入者資料（取代 $_SESSION）
  Products/   商品列表查詢
  Data/       資料庫連線；Dev/ 是開發用 Sqlite 的 schema 與示範資料
  Pages/      Razor Pages（Account/、Products/、Shared/ 版面）
  wwwroot/lib 前端套件：Bootstrap 3、SB Admin 2、jQuery、metisMenu、Font Awesome（皆為 MIT／OFL）
tests/IcarryVendor.Tests/  xUnit：登入規則、商品查詢、頁面整合測試
```

資料存取用 **Dapper**，SQL 全部參數化。沒有用 EF Core，因為正式資料庫的 schema 不在版控裡，Dapper 對欄位型別差異（例如 TINYINT／BIGINT 讀成 int）比較寬容。

## 執行

需要 .NET 10 SDK。

```powershell
dotnet run --project src/IcarryVendor.Web --launch-profile http
```

開發環境（`appsettings.Development.json`）使用 `App_Data/icarry-vendor-dev.db` 這個 Sqlite 檔，第一次啟動時會用 `Data/Dev/schema.sql` 和 `seed.sql` 自動建立。示範帳號與密碼寫在 `seed.sql` 開頭的註解。想重來就把 `App_Data/*.db` 刪掉。

測試：

```powershell
dotnet test
```

## 連正式資料庫（MySQL）

在環境變數或 user secrets 設定，**不要把連線字串寫進 appsettings.json**：

```powershell
$env:Database__Provider = "MySql"
$env:Database__ConnectionString = "Server=...;Database=icarry;User ID=...;Password=..."
```

連線時會自動加上 `TreatTinyAsBoolean=false`，讓 TINYINT 旗標欄位以數字讀取。

⚠️ `Data/Dev/schema.sql` 的欄位型別是從舊程式的 SQL 推測的，接正式資料庫前請先用 `mysqldump --no-data` 對照實際 schema。

## 和舊版的差異

**安全性**
- 所有 SQL 改為參數化查詢（舊版用字串拼接，有 SQL injection 風險）。
- 每一頁預設都要登入，登入頁與靜態檔例外（舊版有 44 支 action／api 沒有登入檢查）。
- 移除 `from_admin` 參數：舊版可以拿密碼的 SHA-1 值直接登入。
- 登出改為 POST 並驗證 antiforgery token。
- 商品列表的提示框內容先做 HTML 編碼再交給 Bootstrap 顯示。

**行為**
- 帳號正確但商家已停用（`vendor.is_on = 0`）時不能登入，顯示「商家已被停用！」。舊版雖然有這個錯誤訊息，但實際沒有檢查。
- 「待補貨」排除已刪除的商品（舊版會列出 `is_del = 1` 的商品）。
- 商品列表加上分頁，每頁 50 筆（舊版一次全部列出）。
- 拿掉已註解停用的 reCAPTCHA；「記住我」改由伺服器設定 HttpOnly cookie。

**沒有改的**
- 密碼仍是**無 salt 的 SHA-1**：`vendor_account` 和 icarry-newvendor、icarry-admin 共用，單獨換演算法會讓那兩個系統無法登入。要升級得三個系統一起處理。
