using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    // Thông tin khách hàng là dữ liệu cá nhân: toàn bộ controller chỉ Admin dùng được
    [Route("api/khachhang")]
    [Authorize(Roles = VaiTro.Admin)]
    public class KhachHangApiController : ApiCoBanController
    {
        private readonly KhachHangBLL _khachHangBll;

        public KhachHangApiController(KhachHangBLL khachHangBll)
        {
            _khachHangBll = khachHangBll;
        }

        // GET /api/khachhang?tuKhoa=
        [HttpGet]
        public ActionResult<List<KhachHangTomTat>> LayDanhSach(string? tuKhoa)
        {
            return _khachHangBll.LayDanhSach(tuKhoa);
        }

        [HttpGet("{ma}")]
        public ActionResult<KhachHang> LayTheoMa(string ma)
        {
            return _khachHangBll.LayTheoMa(ma);
        }

        [HttpPost]
        public IActionResult Them([FromBody] KhachHang dauVao)
        {
            var khachHang = _khachHangBll.Them(dauVao, NguoiHienTai);
            return CreatedAtAction(nameof(LayTheoMa), new { ma = khachHang.MaKhachHang }, khachHang);
        }

        [HttpPut("{ma}")]
        public ActionResult<KhachHang> CapNhat(string ma, [FromBody] KhachHang dauVao)
        {
            return _khachHangBll.CapNhat(ma, dauVao, NguoiHienTai);
        }

        [HttpDelete("{ma}")]
        public IActionResult Xoa(string ma)
        {
            _khachHangBll.Xoa(ma, NguoiHienTai);
            return NoContent();
        }
    }
}
