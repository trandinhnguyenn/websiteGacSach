using BanSach.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers
{
    public class GioHangController : Controller
    {
        // /GioHang - khách và người chưa đăng nhập xem giỏ của mình; admin không mua hàng nên chuyển sang trang báo không có quyền
        public IActionResult Index()
        {
            if (User.IsInRole(VaiTro.Admin))
            {
                return RedirectToAction("KhongCoQuyen", "TaiKhoan");
            }
            return View();
        }

        // /GioHang/ThanhToan - phải đăng nhập bằng tài khoản khách hàng
        [Authorize(Roles = VaiTro.KhachHang)]
        public IActionResult ThanhToan()
        {
            return View();
        }
    }
}
