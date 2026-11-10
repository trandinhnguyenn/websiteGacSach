using BanSach.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers
{
    // Trang quản trị: Razor chỉ dựng khung, dữ liệu do JavaScript gọi /api/... lấy về.
    // [Authorize] ở đây chặn mở trang; API vẫn tự kiểm tra quyền riêng, không dựa vào việc ẩn trang.
    [Authorize(Roles = VaiTro.Admin)]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DonHang()
        {
            return View();
        }

        public IActionResult TheLoai()
        {
            return View();
        }

        public IActionResult Sach()
        {
            return View();
        }

        public IActionResult KhachHang()
        {
            return View();
        }

        public IActionResult NhatKy()
        {
            return View();
        }

        public IActionResult DuLieu()
        {
            return View();
        }
    }
}
