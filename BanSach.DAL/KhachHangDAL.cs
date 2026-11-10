using System.Xml.Linq;
using BanSach.DTO;

namespace BanSach.DAL
{
    // Đọc / ghi KhachHang.xml bằng LINQ to XML
    public class KhachHangDAL
    {
        private const string TenFile = "KhachHang.xml";
        private readonly XmlStore _store;

        public KhachHangDAL(XmlStore store)
        {
            _store = store;
        }

        public List<KhachHang> LayTatCa()
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("KhachHang")
                .Select(ChuyenSangDTO)
                .ToList();
        }

        public KhachHang? LayTheoMa(string ma)
        {
            var phanTu = TimPhanTu(_store.Doc(TenFile), ma);
            return phanTu == null ? null : ChuyenSangDTO(phanTu);
        }

        public string TaoMaMoi()
        {
            return XmlStore.TaoMaMoi(_store.Doc(TenFile), "KhachHang", "MaKhachHang", "KH");
        }

        public void Them(KhachHang khachHang)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                doc.Root!.Add(ChuyenSangXml(khachHang));
                _store.Luu(doc, TenFile);
            }
        }

        public bool CapNhat(KhachHang khachHang)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                var phanTu = TimPhanTu(doc, khachHang.MaKhachHang);
                if (phanTu == null)
                {
                    return false;
                }

                phanTu.ReplaceWith(ChuyenSangXml(khachHang));
                _store.Luu(doc, TenFile);
                return true;
            }
        }

        public bool Xoa(string ma)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                var phanTu = TimPhanTu(doc, ma);
                if (phanTu == null)
                {
                    return false;
                }

                phanTu.Remove();
                _store.Luu(doc, TenFile);
                return true;
            }
        }

        private static XElement? TimPhanTu(XDocument doc, string ma)
        {
            return doc.Root!.Elements("KhachHang")
                .FirstOrDefault(e => (string?)e.Attribute("MaKhachHang") == ma);
        }

        private static KhachHang ChuyenSangDTO(XElement e)
        {
            return new KhachHang
            {
                MaKhachHang = (string)e.Attribute("MaKhachHang")!,
                HoTen = (string)e.Element("HoTen")!,
                SoDienThoai = (string)e.Element("SoDienThoai")!,
                Email = (string?)e.Element("Email"),
                DiaChi = (string)e.Element("DiaChi")!
            };
        }

        private static XElement ChuyenSangXml(KhachHang kh)
        {
            return new XElement("KhachHang",
                new XAttribute("MaKhachHang", kh.MaKhachHang),
                new XElement("HoTen", kh.HoTen),
                new XElement("SoDienThoai", kh.SoDienThoai),
                string.IsNullOrEmpty(kh.Email) ? null : new XElement("Email", kh.Email),
                new XElement("DiaChi", kh.DiaChi));
        }
    }
}
