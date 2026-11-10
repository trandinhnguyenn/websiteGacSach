using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using BanSach.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    // Tài khoản của chính người đang đăng nhập: hồ sơ (chỉ khách) và đổi mật khẩu (mọi tài khoản)
    [Route("api/taikhoan")]
    [Authorize]
    public class TaiKhoanApiController : ApiCoBanController
    {
        private readonly TaiKhoanBLL _taiKhoanBll;
        private readonly KhachHangBLL _khachHangBll;
        private readonly ChongDoMatKhau _chongDoMatKhau;

        public TaiKhoanApiController(TaiKhoanBLL taiKhoanBll, KhachHangBLL khachHangBll, ChongDoMatKhau chongDoMatKhau)
        {
            _taiKhoanBll = taiKhoanBll;
            _khachHangBll = khachHangBll;
            _chongDoMatKhau = chongDoMatKhau;
        }

        // GET /api/taikhoan/hoso - mã khách lấy từ cookie, không nhận từ client (không xem được hồ sơ người khác)
        [HttpGet("hoso")]
        [Authorize(Roles = VaiTro.KhachHang)]
        public ActionResult<KhachHang> LayHoSo()
        {
            return _khachHangBll.LayHoSo(NguoiHienTai);
        }

        // PUT /api/taikhoan/hoso
        [HttpPut("hoso")]
        [Authorize(Roles = VaiTro.KhachHang)]
        public ActionResult<KhachHang> CapNhatHoSo([FromBody] KhachHang dauVao)
        {
            return _khachHangBll.CapNhatHoSo(NguoiHienTai, dauVao);
        }

        // POST /api/taikhoan/doimatkhau
        // Đổi xong cấp lại cookie (tem mới) cho chính phiên này, giữ nguyên lựa chọn "ghi nhớ đăng nhập";
        // cookie ở máy / trình duyệt khác mang tem cũ nên bị đăng xuất ở request kế tiếp.
        // Nhập sai mật khẩu hiện tại 5 lần thì tạm khóa chức năng đổi mật khẩu của tài khoản này 5 phút (429)
        [HttpPost("doimatkhau")]
        public async Task<IActionResult> DoiMatKhau([FromBody] DoiMatKhauYeuCau yeuCau)
        {
            var nguoi = NguoiHienTai;
            if (_chongDoMatKhau.DoiMatKhauBiKhoa(nguoi.MaTaiKhoan))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new LoiApi($"Bạn đã nhập sai mật khẩu hiện tại {ChongDoMatKhau.SoLanSaiToiDa} lần. Vui lòng thử lại sau 5 phút."));
            }

            TaiKhoan taiKhoan;
            try
            {
                taiKhoan = _taiKhoanBll.DoiMatKhau(nguoi, yeuCau);
            }
            catch (SaiMatKhauHienTaiException)
            {
                _chongDoMatKhau.GhiDoiMatKhauSai(nguoi.MaTaiKhoan);
                throw;
            }
            _chongDoMatKhau.XoaBoDemDoiMatKhau(nguoi.MaTaiKhoan);

            var phienHienTai = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await PhienDangNhap.GhiAsync(HttpContext, taiKhoan, phienHienTai.Properties?.IsPersistent == true, matKhauYeu: false);
            return NoContent();
        }
    }
}
