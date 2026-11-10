using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    // Controller chỉ nhận request, gọi BLL, trả kết quả. Không đụng XDocument.
    [Route("api/theloai")]
    public class TheLoaiApiController : ApiCoBanController
    {
        private readonly TheLoaiBLL _theLoaiBll;

        public TheLoaiApiController(TheLoaiBLL theLoaiBll)
        {
            _theLoaiBll = theLoaiBll;
        }

        // GET /api/theloai - ai cũng xem được
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<List<TheLoaiTomTat>> LayDanhSach()
        {
            return _theLoaiBll.LayDanhSach();
        }

        // GET /api/theloai/TL001
        [HttpGet("{ma}")]
        [AllowAnonymous]
        public ActionResult<TheLoai> LayTheoMa(string ma)
        {
            return _theLoaiBll.LayTheoMa(ma);
        }

        // POST /api/theloai - chỉ Admin
        [HttpPost]
        [Authorize(Roles = VaiTro.Admin)]
        public IActionResult Them([FromBody] TheLoai dauVao)
        {
            var theLoai = _theLoaiBll.Them(dauVao, NguoiHienTai);
            return CreatedAtAction(nameof(LayTheoMa), new { ma = theLoai.MaTheLoai }, theLoai);
        }

        // PUT /api/theloai/TL001
        [HttpPut("{ma}")]
        [Authorize(Roles = VaiTro.Admin)]
        public ActionResult<TheLoai> CapNhat(string ma, [FromBody] TheLoai dauVao)
        {
            return _theLoaiBll.CapNhat(ma, dauVao, NguoiHienTai);
        }

        // DELETE /api/theloai/TL001 - còn sách thì BLL trả 409
        [HttpDelete("{ma}")]
        [Authorize(Roles = VaiTro.Admin)]
        public IActionResult Xoa(string ma)
        {
            _theLoaiBll.Xoa(ma, NguoiHienTai);
            return NoContent();
        }
    }
}
