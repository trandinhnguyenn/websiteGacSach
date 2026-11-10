using System.Text;
using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BanSach.Web.Controllers
{
    // Báo cáo HTML sinh bằng XSLT (BaoCaoBLL). Trả thẳng HTML do XSLT tạo, không đi qua Razor layout,
    // để trang in ra giấy gọn gàng và đúng là "XML + XSLT -> HTML".
    [Authorize]
    public class BaoCaoController : Controller
    {
        private readonly BaoCaoBLL _baoCao;
        private readonly DonHangBLL _donHangBll;
        private readonly IFileVersionProvider _phienBanFile;

        public BaoCaoController(BaoCaoBLL baoCao, DonHangBLL donHangBll, IFileVersionProvider phienBanFile)
        {
            _baoCao = baoCao;
            _donHangBll = donHangBll;
            _phienBanFile = phienBanFile;
        }

        private ThongTinCuaHang CuaHang => new ThongTinCuaHang
        {
            TenWeb = ThongTinWeb.TenWeb,
            KhauHieu = ThongTinWeb.KhauHieu,
            Email = ThongTinWeb.EmailRieng,
            SoDienThoai = ThongTinWeb.SoDienThoaiHienThi,
            // Giống asp-append-version của Razor: /css/bao-cao.css?v=<mã băm nội dung>
            DuongDanCss = _phienBanFile.AddFileVersionToPath(Request.PathBase, "/css/bao-cao.css")
        };

        // /BaoCao/TonKho
        [Authorize(Roles = VaiTro.Admin)]
        public IActionResult TonKho()
        {
            return Html(_baoCao.BaoCaoTonKho(CuaHang));
        }

        // /BaoCao/DoanhThu
        [Authorize(Roles = VaiTro.Admin)]
        public IActionResult DoanhThu()
        {
            return Html(_baoCao.BaoCaoDoanhThu(CuaHang));
        }

        // /BaoCao/HoaDon/DH001: admin in mọi đơn, khách chỉ in đơn của mình (đơn người khác trả 404, không lộ là có tồn tại)
        public IActionResult HoaDon(string id)
        {
            var nguoi = User.LayNguoiThucHien();
            DonHangXem don;
            try
            {
                don = _donHangBll.LayChiTiet(id ?? string.Empty, nguoi);
            }
            catch (LoiNghiepVuException)
            {
                return NotFound();
            }

            var trangQuayLai = nguoi.VaiTro == VaiTro.Admin
                ? "/Admin/DonHang"
                : $"/DonHang/ChiTiet/{Uri.EscapeDataString(don.MaDonHang)}";
            return Html(_baoCao.InHoaDon(don.MaDonHang, CuaHang, trangQuayLai));
        }

        private ContentResult Html(byte[] noiDung)
        {
            return Content(Encoding.UTF8.GetString(noiDung), "text/html; charset=utf-8");
        }
    }
}
