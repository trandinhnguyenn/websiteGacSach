using System.Security.Claims;
using BanSach.DTO;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Api
{
    // Lớp cha của mọi API controller:
    // [ApiController] tự trả 400 khi dữ liệu sai kiểu, [LoiApiFilter] đổi exception sang mã HTTP
    [ApiController]
    [LoiApiFilter]
    public abstract class ApiCoBanController : ControllerBase
    {
        // Người đang đăng nhập, đọc từ claims trong cookie
        protected NguoiThucHien NguoiHienTai => User.LayNguoiThucHien();
    }

    public static class NguoiDungExtensions
    {
        public const string ClaimMaKhachHang = "MaKhachHang";

        public static NguoiThucHien LayNguoiThucHien(this ClaimsPrincipal user)
        {
            return new NguoiThucHien
            {
                MaTaiKhoan = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                TenDangNhap = user.Identity?.Name ?? string.Empty,
                VaiTro = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                MaKhachHang = user.FindFirstValue(ClaimMaKhachHang)
            };
        }
    }
}
