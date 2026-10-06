using System.ComponentModel.DataAnnotations;
using IcarryVendor.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IcarryVendor.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel(VendorAuthService auth) : PageModel
{
    public const string RememberAccountCookie = "remember_account";
    public const string NoticeKey = "LoginNotice";

    [BindProperty]
    [Required(ErrorMessage = "帳號和密碼皆為必填！")]
    public string Account { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "帳號和密碼皆為必填！")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [BindProperty]
    public bool RememberAccount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        if (Request.Cookies.TryGetValue(RememberAccountCookie, out var remembered) && !string.IsNullOrEmpty(remembered))
        {
            Account = remembered;
            RememberAccount = true;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "帳號和密碼皆為必填！";
            return Page();
        }

        var result = await auth.SignInAsync(Account, Password);
        if (result.Status != SignInStatus.Success || result.User is null)
        {
            ErrorMessage = result.Status switch
            {
                SignInStatus.AccountDisabled => "帳號已被停用！",
                SignInStatus.NoShopPermission => "帳號沒有使用商家平台的權限！",
                SignInStatus.VendorDisabled => "商家已被停用！",
                _ => "帳號密碼錯誤！",
            };
            return Page();
        }

        if (RememberAccount)
        {
            Response.Cookies.Append(RememberAccountCookie, Account.Trim(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
            });
        }
        else
        {
            Response.Cookies.Delete(RememberAccountCookie);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            result.User.ToPrincipal(CookieAuthenticationDefaults.AuthenticationScheme));

        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Index");
    }
}
