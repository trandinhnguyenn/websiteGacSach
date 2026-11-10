using BanSach.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers
{
    // Đơn của tôi (phía khách). Dữ liệu lấy qua /api/donhang
    [Authorize(Roles = VaiTro.KhachHang)]
    public class DonHangController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // /DonHang/ChiTiet/DH011
        public IActionResult ChiTiet(string id)
        {
            ViewData["MaDonHang"] = id;
            return View();
        }
    }
}
