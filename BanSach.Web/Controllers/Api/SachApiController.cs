using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using BanSach.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    [Route("api/sach")]
    public class SachApiController : ApiCoBanController
    {
        private readonly SachBLL _sachBll;
        private readonly AnhBiaService _anhBia;

        public SachApiController(SachBLL sachBll, AnhBiaService anhBia)
        {
            _sachBll = sachBll;
            _anhBia = anhBia;
        }

        // GET /api/sach?tuKhoa=&maTheLoai=&giaTu=&giaDen=&sapXep=&trang=&kichThuoc=
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<TrangKetQua<Sach>> Loc([FromQuery] BoLocSach boLoc)
        {
            return _sachBll.Loc(boLoc);
        }

        // GET /api/sach/timkiem-xpath?tenSach=&tacGia=&nhaXuatBan=&maTheLoai=&giaTu=&giaDen=&namTu=&namDen=&chiConHang=
        // Trả kèm biểu thức XPath đã chạy trên Sach.xml
        [HttpGet("timkiem-xpath")]
        [AllowAnonymous]
        public ActionResult<KetQuaTimXPath> TimKiemXPath([FromQuery] DieuKienTimSach dieuKien)
        {
            return _sachBll.TimKiemXPath(dieuKien);
        }

        // GET /api/sach/S001/lienquan?soLuong=4 - sách cùng thể loại
        [HttpGet("{ma}/lienquan")]
        [AllowAnonymous]
        public ActionResult<List<Sach>> LienQuan(string ma, int soLuong = 4)
        {
            return _sachBll.LayLienQuan(ma, soLuong);
        }

        // GET /api/sach/S001
        [HttpGet("{ma}")]
        [AllowAnonymous]
        public ActionResult<Sach> LayTheoMa(string ma)
        {
            return _sachBll.LayTheoMa(ma);
        }

        [HttpPost]
        [Authorize(Roles = VaiTro.Admin)]
        public IActionResult Them([FromBody] Sach dauVao)
        {
            var sach = _sachBll.Them(dauVao, NguoiHienTai);
            return CreatedAtAction(nameof(LayTheoMa), new { ma = sach.MaSach }, sach);
        }

        [HttpPut("{ma}")]
        [Authorize(Roles = VaiTro.Admin)]
        public ActionResult<Sach> CapNhat(string ma, [FromBody] Sach dauVao)
        {
            return _sachBll.CapNhat(ma, dauVao, NguoiHienTai);
        }

        // DELETE /api/sach/S001 - đã có trong đơn thì BLL trả 409; xóa được thì xóa luôn file ảnh
        [HttpDelete("{ma}")]
        [Authorize(Roles = VaiTro.Admin)]
        public IActionResult Xoa(string ma)
        {
            var sach = _sachBll.Xoa(ma, NguoiHienTai);
            _anhBia.Xoa(sach.HinhAnh);
            return NoContent();
        }

        // POST /api/sach/S001/anh (multipart/form-data, field "anh") - giới hạn kích thước request ngay từ server
        [HttpPost("{ma}/anh")]
        [Authorize(Roles = VaiTro.Admin)]
        [RequestSizeLimit(AnhBiaService.KichThuocToiDa + 64 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = AnhBiaService.KichThuocToiDa + 64 * 1024)]
        public async Task<ActionResult<Sach>> DoiAnh(string ma, IFormFile? anh)
        {
            _sachBll.LayTheoMa(ma);     // sách không tồn tại thì dừng ở đây (404), chưa lưu file

            var tenFile = await _anhBia.LuuAsync(ma, anh);
            var anhCu = _sachBll.CapNhatHinhAnh(ma, tenFile, NguoiHienTai);
            if (anhCu != null && anhCu != tenFile)
            {
                _anhBia.Xoa(anhCu);     // S001.png -> S001.jpg thì xóa file .png cũ
            }

            return _sachBll.LayTheoMa(ma);
        }
    }
}
