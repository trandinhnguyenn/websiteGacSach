using System.Xml.Linq;
using System.Xml.XPath;
using BanSach.DTO;

namespace BanSach.DAL
{
    // Đọc / ghi TaiKhoan.xml
    public class TaiKhoanDAL
    {
        private const string TenFile = "TaiKhoan.xml";
        private readonly XmlStore _store;

        public TaiKhoanDAL(XmlStore store)
        {
            _store = store;
        }

        public List<TaiKhoan> LayTatCa()
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("TaiKhoan")
                .Select(ChuyenSangDTO)
                .ToList();
        }

        public TaiKhoan? LayTheoMa(string ma)
        {
            var phanTu = _store.Doc(TenFile).Root!.Elements("TaiKhoan")
                .FirstOrDefault(e => (string?)e.Attribute("MaTaiKhoan") == ma);
            return phanTu == null ? null : ChuyenSangDTO(phanTu);
        }

        // XPath tìm theo tên đăng nhập. Tên đăng nhập do người dùng gõ nên bắt buộc đi qua XPathHelper.Literal,
        // nếu ghép chuỗi trực tiếp sẽ bị XPath injection (nhập  x' or '1'='1  là lấy được tài khoản đầu tiên).
        public TaiKhoan? LayTheoTenDangNhap(string tenDangNhap)
        {
            var doc = _store.Doc(TenFile);
            var bieuThuc = $"/DanhSachTaiKhoan/TaiKhoan[TenDangNhap={XPathHelper.Literal(tenDangNhap)}]";
            var phanTu = doc.XPathSelectElement(bieuThuc);
            return phanTu == null ? null : ChuyenSangDTO(phanTu);
        }

        public TaiKhoan? LayTheoMaKhachHang(string maKhachHang)
        {
            var phanTu = _store.Doc(TenFile).Root!.Elements("TaiKhoan")
                .FirstOrDefault(e => (string?)e.Attribute("MaKhachHang") == maKhachHang);
            return phanTu == null ? null : ChuyenSangDTO(phanTu);
        }

        public string TaoMaMoi()
        {
            return XmlStore.TaoMaMoi(_store.Doc(TenFile), "TaiKhoan", "MaTaiKhoan", "TK");
        }

        public void Them(TaiKhoan taiKhoan)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                doc.Root!.Add(ChuyenSangXml(taiKhoan));
                _store.Luu(doc, TenFile);
            }
        }

        public bool CapNhat(TaiKhoan taiKhoan)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                var phanTu = doc.Root!.Elements("TaiKhoan")
                    .FirstOrDefault(e => (string?)e.Attribute("MaTaiKhoan") == taiKhoan.MaTaiKhoan);
                if (phanTu == null)
                {
                    return false;
                }

                phanTu.ReplaceWith(ChuyenSangXml(taiKhoan));
                _store.Luu(doc, TenFile);
                return true;
            }
        }

        private static TaiKhoan ChuyenSangDTO(XElement e)
        {
            return new TaiKhoan
            {
                MaTaiKhoan = (string)e.Attribute("MaTaiKhoan")!,
                VaiTro = (string)e.Attribute("VaiTro")!,
                MaKhachHang = (string?)e.Attribute("MaKhachHang"),
                TenDangNhap = (string)e.Element("TenDangNhap")!,
                MatKhauHash = (string)e.Element("MatKhauHash")!,
                Salt = (string)e.Element("Salt")!
            };
        }

        private static XElement ChuyenSangXml(TaiKhoan tk)
        {
            return new XElement("TaiKhoan",
                new XAttribute("MaTaiKhoan", tk.MaTaiKhoan),
                new XAttribute("VaiTro", tk.VaiTro),
                string.IsNullOrEmpty(tk.MaKhachHang) ? null : new XAttribute("MaKhachHang", tk.MaKhachHang),
                new XElement("TenDangNhap", tk.TenDangNhap),
                new XElement("MatKhauHash", tk.MatKhauHash),
                new XElement("Salt", tk.Salt));
        }
    }
}
