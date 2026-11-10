using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers
{
    // Trang sách phía khách: Razor chỉ dựng khung, dữ liệu lấy qua /api/sach bằng JavaScript
    public class SachController : Controller
    {
        // /Sach?maTheLoai=TL001&tuKhoa=...
        public IActionResult Index()
        {
            return View();
        }

        // /Sach/ChiTiet/S001
        public IActionResult ChiTiet(string id)
        {
            ViewData["MaSach"] = id;
            return View();
        }
    }
}
