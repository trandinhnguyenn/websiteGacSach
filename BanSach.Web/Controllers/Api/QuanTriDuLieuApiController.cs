using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using BanSach.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    // Xuất / nhập nguyên file XML trong App_Data. Chỉ admin; danh sách file được phép nằm cứng trong DuLieuBLL.
    [Route("api/quantri/dulieu")]
    [Authorize(Roles = VaiTro.Admin)]
    public class QuanTriDuLieuApiController : ApiCoBanController
    {
        private const long KichThuocToiDa = 2 * 1024 * 1024;   // 2MB là quá đủ cho file dữ liệu của đồ án

        private readonly DuLieuBLL _duLieuBll;
        private readonly IWebHostEnvironment _env;

        public QuanTriDuLieuApiController(DuLieuBLL duLieuBll, IWebHostEnvironment env)
        {
            _duLieuBll = duLieuBll;
            _env = env;
        }

        // GET /api/quantri/dulieu - các file XML, số bản ghi, dung lượng, sửa lần cuối
        [HttpGet]
        public ActionResult<List<TepDuLieu>> LayDanhSach()
        {
            return _duLieuBll.LayDanhSachTep();
        }

        // GET /api/quantri/dulieu/tai/Sach.xml (hoặc Sach.xsd) - tải nguyên file về máy
        [HttpGet("tai/{tenFile}")]
        public IActionResult Tai(string tenFile)
        {
            var (noiDung, tenThat) = _duLieuBll.TaiFile(tenFile);
            return File(noiDung, "application/xml", tenThat);
        }

        // POST /api/quantri/dulieu/nhap/Sach.xml?cheDo=gop&chiKiemTra=true  (multipart, trường "file")
        // chiKiemTra=true: chỉ báo trước sẽ thêm / sửa / xóa bao nhiêu, chưa ghi gì
        [HttpPost("nhap/{tenFile}")]
        [RequestSizeLimit(KichThuocToiDa + 64 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = KichThuocToiDa + 64 * 1024)]
        public ActionResult<KetQuaNhapXml> Nhap(string tenFile, IFormFile? file, [FromQuery] string? cheDo, [FromQuery] bool chiKiemTra = true)
        {
            if (file == null || file.Length == 0)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Chưa chọn file XML");
            }
            if (file.Length > KichThuocToiDa)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "File quá lớn (tối đa 2MB)");
            }
            if (!string.Equals(Path.GetExtension(file.FileName), ".xml", StringComparison.OrdinalIgnoreCase))
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Chỉ nhận file .xml");
            }

            var thuMucAnh = Path.Combine(_env.WebRootPath, "images", "sach");
            using var luong = file.OpenReadStream();
            return _duLieuBll.NhapFile(tenFile, luong, cheDo, chiKiemTra, NguoiHienTai,
                anh => System.IO.File.Exists(Path.Combine(thuMucAnh, Path.GetFileName(anh))));
        }
    }
}
