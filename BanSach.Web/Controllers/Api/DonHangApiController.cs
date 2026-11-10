using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using BanSach.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    // Đơn hàng phía khách. Quyền sở hữu (chỉ xem / hủy đơn của mình) kiểm tra ở BLL theo MaKhachHang trong cookie.
    [Route("api/donhang")]
    [Authorize(Roles = VaiTro.KhachHang)]
    public class DonHangApiController : ApiCoBanController
    {
        private readonly DonHangBLL _donHangBll;

        public DonHangApiController(DonHangBLL donHangBll)
        {
            _donHangBll = donHangBll;
        }

        // POST /api/donhang - đặt hàng từ giỏ trong session
        [HttpPost]
        public IActionResult DatHang([FromBody] DatHangYeuCau thongTin)
        {
            var donHang = _donHangBll.DatHang(NguoiHienTai, GioHangSession.Lay(HttpContext), thongTin);
            GioHangSession.XoaHet(HttpContext);     // đặt xong thì làm trống giỏ
            return CreatedAtAction(nameof(LayChiTiet), new { ma = donHang.MaDonHang }, donHang);
        }

        // GET /api/donhang/cua-toi
        [HttpGet("cua-toi")]
        public ActionResult<List<DonHangXem>> CuaToi()
        {
            return _donHangBll.LayDonCuaToi(NguoiHienTai);
        }

        // GET /api/donhang/DH001 - đơn của người khác trả 404
        [HttpGet("{ma}")]
        public ActionResult<DonHangXem> LayChiTiet(string ma)
        {
            return _donHangBll.LayChiTiet(ma, NguoiHienTai);
        }

        // POST /api/donhang/DH001/huy - chỉ khi Chờ xác nhận
        [HttpPost("{ma}/huy")]
        public ActionResult<DonHangXem> Huy(string ma)
        {
            return _donHangBll.HuyDonCuaToi(ma, NguoiHienTai);
        }
    }
}
