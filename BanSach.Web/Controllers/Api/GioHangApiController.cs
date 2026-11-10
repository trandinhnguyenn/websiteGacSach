using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using BanSach.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    // Giỏ hàng: khách và người chưa đăng nhập đều dùng được (đặt hàng mới bắt đăng nhập); tài khoản quản trị bị chặn.
    // Session chỉ giữ (mã sách, số lượng); giá và tồn kho luôn đọc mới từ Sach.xml qua BLL.
    [Route("api/giohang")]
    [AllowAnonymous]
    public class GioHangApiController : ApiCoBanController
    {
        private readonly GioHangBLL _gioHangBll;

        public GioHangApiController(GioHangBLL gioHangBll)
        {
            _gioHangBll = gioHangBll;
        }

        // GET /api/giohang
        [HttpGet]
        public ActionResult<GioHang> Xem()
        {
            return TinhVaLuu(GioHangSession.Lay(HttpContext));
        }

        // POST /api/giohang  { maSach, soLuong } - thêm (cộng dồn)
        [HttpPost]
        public ActionResult<GioHang> Them([FromBody] GioHangYeuCau yeuCau)
        {
            ChanQuanTri();
            var dong = GioHangSession.Lay(HttpContext);
            _gioHangBll.Them(dong, yeuCau.MaSach, yeuCau.SoLuong);
            return TinhVaLuu(dong);
        }

        // PUT /api/giohang  { maSach, soLuong } - đặt lại số lượng (0 = bỏ khỏi giỏ)
        [HttpPut]
        public ActionResult<GioHang> CapNhat([FromBody] GioHangYeuCau yeuCau)
        {
            ChanQuanTri();
            var dong = GioHangSession.Lay(HttpContext);
            _gioHangBll.CapNhat(dong, yeuCau.MaSach, yeuCau.SoLuong);
            return TinhVaLuu(dong);
        }

        // DELETE /api/giohang/S001
        [HttpDelete("{maSach}")]
        public ActionResult<GioHang> Xoa(string maSach)
        {
            ChanQuanTri();
            var dong = GioHangSession.Lay(HttpContext);
            _gioHangBll.Xoa(dong, maSach);
            return TinhVaLuu(dong);
        }

        // Admin không mua hàng (API đặt hàng chỉ nhận vai trò KhachHang) nên cũng không được bỏ sách vào giỏ:
        // trước đây admin bấm "Thêm vào giỏ" vẫn thêm được vào một giỏ không hiện ở đâu cả
        private void ChanQuanTri()
        {
            if (User.IsInRole(VaiTro.Admin))
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongCoQuyen,
                    "Tài khoản quản trị không dùng giỏ hàng. Muốn thử đặt sách, hãy đăng nhập bằng tài khoản khách.");
            }
        }

        private GioHang TinhVaLuu(List<GioHangDong> dong)
        {
            var gioHang = _gioHangBll.TinhLai(dong);   // có thể bỏ sách đã ngừng bán / giảm số lượng theo tồn kho
            GioHangSession.Luu(HttpContext, dong);
            return gioHang;
        }
    }
}
