using System.Xml.Linq;
using System.Xml.XPath;
using BanSach.DTO;

namespace BanSach.DAL
{
    // Đọc / ghi DonHang.xml. Chi tiết đơn lồng bên trong <DonHang> nên đọc / ghi cùng lúc.
    public class DonHangDAL
    {
        private const string TenFile = "DonHang.xml";
        private readonly XmlStore _store;

        public DonHangDAL(XmlStore store)
        {
            _store = store;
        }

        public List<DonHang> LayTatCa()
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("DonHang")
                .Select(ChuyenSangDTO)
                .ToList();
        }

        public DonHang? LayTheoMa(string ma)
        {
            var phanTu = TimPhanTu(_store.Doc(TenFile), ma);
            return phanTu == null ? null : ChuyenSangDTO(phanTu);
        }

        public List<DonHang> LayTheoKhachHang(string maKhachHang)
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("DonHang")
                .Where(e => (string?)e.Attribute("MaKhachHang") == maKhachHang)
                .Select(ChuyenSangDTO)
                .ToList();
        }

        // XPath: đếm số đơn có chứa một cuốn sách (dùng để chặn xóa sách đã bán)
        public int DemDonChuaSach(string maSach)
        {
            var doc = _store.Doc(TenFile);
            var bieuThuc = $"count(/DanhSachDonHang/DonHang[ChiTiet/@MaSach={XPathHelper.Literal(maSach)}])";
            return Convert.ToInt32(doc.XPathEvaluate(bieuThuc));
        }

        // XPath: đếm số đơn của một khách hàng (dùng để chặn xóa khách đã mua)
        public int DemTheoKhachHang(string maKhachHang)
        {
            var doc = _store.Doc(TenFile);
            var bieuThuc = $"count(/DanhSachDonHang/DonHang[@MaKhachHang={XPathHelper.Literal(maKhachHang)}])";
            return Convert.ToInt32(doc.XPathEvaluate(bieuThuc));
        }

        // LINQ GroupBy: số đơn của từng khách trong một lần đọc file
        public Dictionary<string, int> DemTheoTungKhachHang()
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("DonHang")
                .GroupBy(e => (string)e.Attribute("MaKhachHang")!)
                .ToDictionary(nhom => nhom.Key, nhom => nhom.Count());
        }

        public string TaoMaMoi()
        {
            return XmlStore.TaoMaMoi(_store.Doc(TenFile), "DonHang", "MaDonHang", "DH");
        }

        public void Them(DonHang donHang)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                doc.Root!.Add(ChuyenSangXml(donHang));
                _store.Luu(doc, TenFile);
            }
        }

        public bool CapNhat(DonHang donHang)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                var phanTu = TimPhanTu(doc, donHang.MaDonHang);
                if (phanTu == null)
                {
                    return false;
                }

                phanTu.ReplaceWith(ChuyenSangXml(donHang));
                _store.Luu(doc, TenFile);
                return true;
            }
        }

        private static XElement? TimPhanTu(XDocument doc, string ma)
        {
            return doc.Root!.Elements("DonHang")
                .FirstOrDefault(e => (string?)e.Attribute("MaDonHang") == ma);
        }

        private static DonHang ChuyenSangDTO(XElement e)
        {
            return new DonHang
            {
                MaDonHang = (string)e.Attribute("MaDonHang")!,
                MaKhachHang = (string)e.Attribute("MaKhachHang")!,
                NgayDat = (DateTime)e.Element("NgayDat")!,
                TrangThai = (string)e.Element("TrangThai")!,
                NguoiNhan = (string)e.Element("NguoiNhan")!,
                SoDienThoai = (string)e.Element("SoDienThoai")!,
                DiaChiGiao = (string)e.Element("DiaChiGiao")!,
                GhiChu = (string?)e.Element("GhiChu"),
                ChiTiet = e.Elements("ChiTiet")
                    .Select(ct => new ChiTietDonHang
                    {
                        MaSach = (string)ct.Attribute("MaSach")!,
                        SoLuong = (int)ct.Element("SoLuong")!,
                        DonGia = (decimal)ct.Element("DonGia")!
                    })
                    .ToList()
            };
        }

        // NgayDat ghi dạng yyyy-MM-ddTHH:mm:ss (giờ địa phương, không kèm múi giờ) cho dễ đọc và khớp xs:dateTime
        private static XElement ChuyenSangXml(DonHang dh)
        {
            return new XElement("DonHang",
                new XAttribute("MaDonHang", dh.MaDonHang),
                new XAttribute("MaKhachHang", dh.MaKhachHang),
                new XElement("NgayDat", dh.NgayDat.ToString("yyyy-MM-dd'T'HH:mm:ss")),
                new XElement("TrangThai", dh.TrangThai),
                new XElement("NguoiNhan", dh.NguoiNhan),
                new XElement("SoDienThoai", dh.SoDienThoai),
                new XElement("DiaChiGiao", dh.DiaChiGiao),
                string.IsNullOrEmpty(dh.GhiChu) ? null : new XElement("GhiChu", dh.GhiChu),
                dh.ChiTiet.Select(ct => new XElement("ChiTiet",
                    new XAttribute("MaSach", ct.MaSach),
                    new XElement("SoLuong", ct.SoLuong),
                    new XElement("DonGia", ct.DonGia))));
        }
    }
}
