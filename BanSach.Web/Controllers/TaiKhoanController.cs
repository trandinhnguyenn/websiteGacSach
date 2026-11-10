using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers
{
    // Trang đăng nhập, đăng ký, hồ sơ (form gửi qua /api/auth, /api/taikhoan bằng JavaScript)
    public class TaiKhoanController : Controller
    {
        // hetPhien=1: bị đăng xuất vì mật khẩu vừa được đổi ở máy / trình duyệt khác (xem PhienDangNhap)
        public IActionResult DangNhap(string? returnUrl, int? hetPhien)
        {
            ViewData["QuayLai"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            ViewData["HetPhien"] = hetPhien == 1;
            return View();
        }

        public IActionResult DangKy(string? returnUrl)
        {
            ViewData["QuayLai"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            return View();
        }

        [Authorize]
        public IActionResult HoSo()
        {
            return View();
        }

        public IActionResult KhongCoQuyen()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }
    }
}
