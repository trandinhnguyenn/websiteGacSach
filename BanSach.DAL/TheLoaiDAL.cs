using System.Xml.Linq;
using BanSach.DTO;

namespace BanSach.DAL
{
    // Đọc / ghi TheLoai.xml bằng LINQ to XML
    public class TheLoaiDAL
    {
        private const string TenFile = "TheLoai.xml";
        private readonly XmlStore _store;

        public TheLoaiDAL(XmlStore store)
        {
            _store = store;
        }

        public List<TheLoai> LayTatCa()
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("TheLoai")
                .Select(ChuyenSangDTO)
                .ToList();
        }

        public TheLoai? LayTheoMa(string ma)
        {
            var phanTu = TimPhanTu(_store.Doc(TenFile), ma);
            return phanTu == null ? null : ChuyenSangDTO(phanTu);
        }

        public string TaoMaMoi()
        {
            return XmlStore.TaoMaMoi(_store.Doc(TenFile), "TheLoai", "MaTheLoai", "TL");
        }

        public void Them(TheLoai theLoai)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                doc.Root!.Add(ChuyenSangXml(theLoai));
                _store.Luu(doc, TenFile);
            }
        }

        public bool CapNhat(TheLoai theLoai)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                var phanTu = TimPhanTu(doc, theLoai.MaTheLoai);
                if (phanTu == null)
                {
                    return false;
                }

                // Thay cả phần tử để các element con luôn đúng thứ tự xs:sequence
                phanTu.ReplaceWith(ChuyenSangXml(theLoai));
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
            return doc.Root!.Elements("TheLoai")
                .FirstOrDefault(e => (string?)e.Attribute("MaTheLoai") == ma);
        }

        private static TheLoai ChuyenSangDTO(XElement e)
        {
            return new TheLoai
            {
                MaTheLoai = (string)e.Attribute("MaTheLoai")!,
                TenTheLoai = (string)e.Element("TenTheLoai")!,
                MoTa = (string?)e.Element("MoTa")
            };
        }

        // Phần tử null bị XElement bỏ qua, nên MoTa trống thì không sinh <MoTa/>
        private static XElement ChuyenSangXml(TheLoai tl)
        {
            return new XElement("TheLoai",
                new XAttribute("MaTheLoai", tl.MaTheLoai),
                new XElement("TenTheLoai", tl.TenTheLoai),
                string.IsNullOrEmpty(tl.MoTa) ? null : new XElement("MoTa", tl.MoTa));
        }
    }
}
