using System.ComponentModel.DataAnnotations;
using IcarryVendor.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IcarryVendor.Web.Pages.Account;

public class ChangePasswordModel(VendorAuthService auth) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "請輸入原來的密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "舊密碼")]
    public string CurrentPassword { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "請輸入新密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "新密碼")]
    public string NewPassword { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "請再次輸入新密碼")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "新密碼兩次輸入不相同，無法為您變更密碼。")]
    [Display(Name = "再次輸入新密碼")]
    public string ConfirmPassword { get; set; } = "";

    public VendorUser CurrentUser { get; private set; } = null!;

    public void OnGet()
    {
        CurrentUser = VendorUser.FromPrincipal(User)!;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CurrentUser = VendorUser.FromPrincipal(User)!;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var status = await auth.ChangePasswordAsync(CurrentUser.AccountId, CurrentPassword, NewPassword);
        if (status == ChangePasswordStatus.WrongCurrentPassword)
        {
            ModelState.AddModelError(nameof(CurrentPassword), "舊密碼錯誤，無法為您變更密碼。");
            return Page();
        }
        if (status == ChangePasswordStatus.AccountNotFound)
        {
            ModelState.AddModelError(string.Empty, "找不到您的帳號，請重新登入。");
            return Page();
        }

        // Same as the legacy page: after a password change the vendor signs in again.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData[LoginModel.NoticeKey] = "密碼已更新，請重新登入。";
        return RedirectToPage("/Account/Login");
    }
}
