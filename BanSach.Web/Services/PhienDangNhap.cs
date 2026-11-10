using System.Security.Claims;
using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BanSach.Web.Services
{
    // Cookie đăng nhập: ghi (đăng nhập, đăng ký, đổi mật khẩu) và kiểm tra lại ở MỖI request.
    // Cookie đã được Data Protection mã hóa + ký nên client không sửa được, nhưng nó vẫn sống tới khi hết hạn;
    // vì vậy mỗi lần dùng phải đối chiếu với TaiKhoan.xml để đổi mật khẩu là đăng xuất được các máy khác.
    public static class PhienDangNhap
    {
        public const string ClaimTemBaoMat = "TemBaoMat";
        public const string ClaimMatKhauYeu = "MatKhauYeu";     // "1": mật khẩu đặt trước khi có quy tắc mới, nhắc đổi

        // Đánh dấu trong HttpContext.Items để OnRedirectToLogin báo đúng lý do bị đăng xuất
        public const string KhoaHetPhien = "BanSach.HetPhien";

        public static async Task GhiAsync(HttpContext http, TaiKhoan taiKhoan, bool ghiNho, bool matKhauYeu)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTaiKhoan),
                new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role, taiKhoan.VaiTro),
                new Claim(ClaimTemBaoMat, TaiKhoanBLL.TaoTemBaoMat(taiKhoan))
            };
            if (!string.IsNullOrEmpty(taiKhoan.MaKhachHang))
            {
                claims.Add(new Claim(NguoiDungExtensions.ClaimMaKhachHang, taiKhoan.MaKhachHang));
            }
            if (matKhauYeu)
            {
                claims.Add(new Claim(ClaimMatKhauYeu, "1"));
            }

            var danhTinh = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(danhTinh),
                new AuthenticationProperties { IsPersistent = ghiNho });
        }

        // CookieAuthenticationEvents.OnValidatePrincipal: tem không khớp (đã đổi mật khẩu ở nơi khác),
        // tài khoản không còn, hoặc vai trò đã đổi thì hủy cookie, request này coi như chưa đăng nhập.
        // Cookie cấp trước khi có tem (không có claim TemBaoMat) cũng bị hủy: người dùng đăng nhập lại một lần.
        public static async Task KiemTraAsync(CookieValidatePrincipalContext context)
        {
            var nguoi = context.Principal;
            var taiKhoanBll = context.HttpContext.RequestServices.GetRequiredService<TaiKhoanBLL>();
            var conHieuLuc = nguoi != null && taiKhoanBll.PhienConHieuLuc(
                nguoi.FindFirstValue(ClaimTypes.NameIdentifier),
                nguoi.FindFirstValue(ClaimTemBaoMat),
                nguoi.FindFirstValue(ClaimTypes.Role),
                nguoi.FindFirstValue(NguoiDungExtensions.ClaimMaKhachHang));

            if (!conHieuLuc)
            {
                context.RejectPrincipal();
                context.HttpContext.Items[KhoaHetPhien] = true;
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        public static bool VuaHetPhien(HttpContext http)
        {
            return http.Items.ContainsKey(KhoaHetPhien);
        }
    }
}
