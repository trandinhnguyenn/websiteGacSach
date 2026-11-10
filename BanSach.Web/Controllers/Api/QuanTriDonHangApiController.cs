using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    // Quản lý đơn hàng phía admin. Tách khỏi /api/donhang (phía khách) để quyền của hai bên không lẫn vào nhau.
    [Route("api/quantri/donhang")]
    [Authorize(Roles = VaiTro.Admin)]
    public class QuanTriDonHangApiController : ApiCoBanController
    {
        private readonly DonHangBLL _donHangBll;

        public QuanTriDonHangApiController(DonHangBLL donHangBll)
        {
            _donHangBll = donHangBll;
        }

        // GET /api/quantri/donhang?trangThai=ChoXacNhan&tuKhoa=&trang=1&kichThuoc=15
        [HttpGet]
        public ActionResult<DanhSachDonHangQuanTri> LayDanhSach([FromQuery] BoLocDonHang boLoc)
        {
            return _donHangBll.LayDanhSachQuanTri(boLoc);
        }

        // GET /api/quantri/donhang/DH001
        [HttpGet("{ma}")]
        public ActionResult<DonHangQuanTri> LayChiTiet(string ma)
        {
            return _donHangBll.LayChiTietQuanTri(ma);
        }

        // POST /api/quantri/donhang/DH001/trangthai  { "trangThai": "DaXacNhan" }
        // 409 khi chuyển sai thứ tự hoặc kho không đủ (kèm danh sách sách thiếu)
        [HttpPost("{ma}/trangthai")]
        public ActionResult<DonHangQuanTri> DoiTrangThai(string ma, [FromBody] DoiTrangThaiYeuCau yeuCau)
        {
            return _donHangBll.DoiTrangThai(ma, yeuCau.TrangThai, NguoiHienTai);
        }
    }
}
