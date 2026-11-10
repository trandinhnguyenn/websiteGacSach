using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using BanSach.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BanSach.Web.Controllers.Api
{
    public class DangNhapYeuCau
    {
        public string? TenDangNhap { get; set; }
        public string? MatKhau { get; set; }
        public bool GhiNho { get; set; }
        public string? QuayLai { get; set; }    // trang cần quay lại sau khi đăng nhập
    }

    public class ThongTinDangNhap
    {
        public string MaTaiKhoan { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public string? MaKhachHang { get; set; }
        public string? ChuyenHuong { get; set; }
        public string? ThongBao { get; set; }
    }

    [Route("api/auth")]
    public class AuthApiController : ApiCoBanController
    {
        private readonly TaiKhoanBLL _taiKhoanBll;
        private readonly ChongDoMatKhau _chongDoMatKhau;

        public AuthApiController(TaiKhoanBLL taiKhoanBll, ChongDoMatKhau chongDoMatKhau)
        {
            _taiKhoanBll = taiKhoanBll;
            _chongDoMatKhau = chongDoMatKhau;
        }

        // POST /api/auth/dangnhap
        // Hai lớp chống dò mật khẩu: sai 5 lần thì khóa tài khoản đó trên IP đó 5 phút (ChongDoMatKhau),
        // và mỗi IP tối đa 30 lần gọi / phút (rate limiter)
        [HttpPost("dangnhap")]
        [AllowAnonymous]
        [EnableRateLimiting(ChinhSach.GioiHanDangNhap)]
        public async Task<IActionResult> DangNhap([FromBody] DangNhapYeuCau yeuCau)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "khong-ro";

            // Đang bị khóa thì từ chối luôn, không kiểm tra mật khẩu (kẻ dò không biết lần đó đúng hay sai)
            if (_chongDoMatKhau.DangBiKhoa(ip, yeuCau.TenDangNhap))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new LoiApi($"Bạn đã nhập sai {ChongDoMatKhau.SoLanSaiToiDa} lần. Tài khoản này tạm khóa đăng nhập trên máy bạn 5 phút."));
            }

            var taiKhoan = _taiKhoanBll.DangNhap(yeuCau.TenDangNhap, yeuCau.MatKhau);
            if (taiKhoan == null)
            {
                _chongDoMatKhau.GhiLanSai(ip, yeuCau.TenDangNhap);
                return Unauthorized(new LoiApi("Sai tên đăng nhập hoặc mật khẩu"));
            }

            _chongDoMatKhau.XoaBoDem(ip, yeuCau.TenDangNhap);

            // Chỉ lúc đăng nhập mới có mật khẩu gốc trong tay: mật khẩu đặt trước khi có quy tắc mới
            // (vd 6 ký tự toàn số) vẫn cho vào, nhưng đánh dấu trong cookie để trang hiện lời nhắc đổi
            var matKhauYeu = !ChinhSachMatKhau.DatYeuCau(yeuCau.MatKhau, taiKhoan.TenDangNhap);
            await PhienDangNhap.GhiAsync(HttpContext, taiKhoan, yeuCau.GhiNho, matKhauYeu);

            // Chỉ cho quay lại đường dẫn nội bộ, chặn kiểu ?QuayLai=https://trang-lua-dao.com (open redirect)
            var chuyenHuong = !string.IsNullOrEmpty(yeuCau.QuayLai) && Url.IsLocalUrl(yeuCau.QuayLai)
                ? yeuCau.QuayLai
                : (taiKhoan.VaiTro == VaiTro.Admin ? "/Admin" : "/");

            return Ok(TaoThongTin(taiKhoan, chuyenHuong));
        }

        // POST /api/auth/dangky - đăng ký xong đăng nhập luôn
        [HttpPost("dangky")]
        [AllowAnonymous]
        [EnableRateLimiting(ChinhSach.GioiHanDangNhap)]
        public async Task<IActionResult> DangKy([FromBody] DangKyYeuCau yeuCau, [FromQuery] string? quayLai)
        {
            var (taiKhoan, ganHoSoCu) = _taiKhoanBll.DangKy(yeuCau);
            await PhienDangNhap.GhiAsync(HttpContext, taiKhoan, false, matKhauYeu: false);

            var thongTin = TaoThongTin(taiKhoan, !string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai) ? quayLai : "/");
            thongTin.ThongBao = ganHoSoCu
                ? "Đã tạo tài khoản và gắn vào hồ sơ khách hàng có sẵn theo số điện thoại của bạn."
                : "Đã tạo tài khoản.";
            return Ok(thongTin);
        }

        private static ThongTinDangNhap TaoThongTin(TaiKhoan taiKhoan, string? chuyenHuong)
        {
            return new ThongTinDangNhap
            {
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                TenDangNhap = taiKhoan.TenDangNhap,
                VaiTro = taiKhoan.VaiTro,
                MaKhachHang = taiKhoan.MaKhachHang,
                ChuyenHuong = chuyenHuong
            };
        }

        // POST /api/auth/dangxuat
        [HttpPost("dangxuat")]
        [AllowAnonymous]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();    // máy dùng chung: đăng xuất thì xóa luôn giỏ hàng
            return NoContent();
        }

        // GET /api/auth/toi - ai đang đăng nhập
        [HttpGet("toi")]
        [Authorize]
        public IActionResult Toi()
        {
            var nguoi = NguoiHienTai;
            return Ok(new ThongTinDangNhap
            {
                MaTaiKhoan = nguoi.MaTaiKhoan,
                TenDangNhap = nguoi.TenDangNhap,
                VaiTro = nguoi.VaiTro,
                MaKhachHang = nguoi.MaKhachHang
            });
        }
    }
}
