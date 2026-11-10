using System.Collections.Concurrent;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using BanSach.DAL;

namespace BanSach.BLL
{
    // Thông tin cửa hàng in lên báo cáo (tầng Web truyền xuống, BLL không biết tên web)
    public class ThongTinCuaHang
    {
        public string TenWeb { get; set; } = string.Empty;
        public string KhauHieu { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        // Đường dẫn bao-cao.css kèm số phiên bản (?v=...) do tầng Web tính
        public string DuongDanCss { get; set; } = "/css/bao-cao.css";
    }

    // Sinh báo cáo HTML bằng XSLT: file XML trong App_Data + file .xslt trong thư mục Xslt -> trang HTML.
    // Dữ liệu từ file khác (tên sách, tên khách) truyền vào XSLT dưới dạng tham số node-set (XPathNavigator).
    public class BaoCaoBLL
    {
        public const string TonKho = "BaoCaoTonKho.xslt";
        public const string DoanhThu = "BaoCaoDoanhThu.xslt";
        public const string HoaDon = "HoaDon.xslt";

        private readonly XmlStore _store;
        private readonly string _thuMucXslt;
        // Biên dịch mỗi file XSLT một lần; XslCompiledTransform dùng chung giữa các request được (Transform an toàn đa luồng)
        private readonly ConcurrentDictionary<string, XslCompiledTransform> _daBienDich = new ConcurrentDictionary<string, XslCompiledTransform>();

        public BaoCaoBLL(XmlStore store, string thuMucXslt)
        {
            _store = store;
            _thuMucXslt = thuMucXslt;
        }

        public byte[] BaoCaoTonKho(ThongTinCuaHang cuaHang)
        {
            var thamSo = ThamSoChung(cuaHang);
            thamSo.AddParam("theLoai", string.Empty, _store.Doc("TheLoai.xml").CreateNavigator());
            thamSo.AddParam("nguongSapHet", string.Empty, 5.0);
            return BienDoi(TonKho, "Sach.xml", thamSo);
        }

        public byte[] BaoCaoDoanhThu(ThongTinCuaHang cuaHang)
        {
            var thamSo = ThamSoChung(cuaHang);
            thamSo.AddParam("sach", string.Empty, _store.Doc("Sach.xml").CreateNavigator());
            return BienDoi(DoanhThu, "DonHang.xml", thamSo);
        }

        // Quyền xem đơn (khách chỉ xem đơn của mình) kiểm tra ở DonHangBLL trước khi gọi hàm này
        public byte[] InHoaDon(string maDonHang, ThongTinCuaHang cuaHang, string trangQuayLai)
        {
            var thamSo = ThamSoChung(cuaHang);
            thamSo.AddParam("maDonHang", string.Empty, maDonHang);
            thamSo.AddParam("sach", string.Empty, _store.Doc("Sach.xml").CreateNavigator());
            thamSo.AddParam("khachHang", string.Empty, _store.Doc("KhachHang.xml").CreateNavigator());
            thamSo.AddParam("email", string.Empty, cuaHang.Email);
            thamSo.AddParam("soDienThoai", string.Empty, cuaHang.SoDienThoai);
            thamSo.AddParam("ngayIn", string.Empty, DateTime.Now.ToString("HH:mm dd/MM/yyyy"));
            thamSo.AddParam("trangQuayLai", string.Empty, trangQuayLai);
            return BienDoi(HoaDon, "DonHang.xml", thamSo);
        }

        private static XsltArgumentList ThamSoChung(ThongTinCuaHang cuaHang)
        {
            var thamSo = new XsltArgumentList();
            thamSo.AddParam("tenWeb", string.Empty, cuaHang.TenWeb);
            thamSo.AddParam("khauHieu", string.Empty, cuaHang.KhauHieu);
            thamSo.AddParam("duongCss", string.Empty, cuaHang.DuongDanCss);
            thamSo.AddParam("ngayLap", string.Empty, DateTime.Now.ToString("HH:mm dd/MM/yyyy"));
            return thamSo;
        }

        private byte[] BienDoi(string tenXslt, string tenFileXml, XsltArgumentList thamSo)
        {
            var xslt = _daBienDich.GetOrAdd(tenXslt, BienDich);
            var dauVao = _store.Doc(tenFileXml);

            // Ghi ra Stream (StringWriter sẽ thành utf-16) bằng chính <xsl:output> của file .xslt,
            // chỉ đổi sang UTF-8 không BOM: BOM lọt vào chuỗi trả về sẽ thành ký tự lạ trước <!DOCTYPE>.
            // XSLT method="html" tự chèn <META http-equiv="Content-Type"> vào <head>.
            var caiDat = xslt.OutputSettings!.Clone();
            caiDat.Encoding = new UTF8Encoding(false);
            using var ketQua = new MemoryStream();
            using (var viet = XmlWriter.Create(ketQua, caiDat))
            {
                xslt.Transform(dauVao.CreateNavigator(), thamSo, viet);
            }
            return ketQua.ToArray();
        }

        // XsltSettings.Default: tắt document() và <msxsl:script> (không cho XSLT đọc file tùy ý hay chạy code C#).
        // Đọc file .xslt với DtdProcessing.Prohibit; KHÔNG bỏ khoảng trắng vì <xsl:text> </xsl:text> cần giữ dấu cách.
        private XslCompiledTransform BienDich(string tenXslt)
        {
            var caiDat = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            var xslt = new XslCompiledTransform();
            using (var reader = XmlReader.Create(Path.Combine(_thuMucXslt, tenXslt), caiDat))
            {
                xslt.Load(reader, XsltSettings.Default, null);
            }
            return xslt;
        }
    }
}
