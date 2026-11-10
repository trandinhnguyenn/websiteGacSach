using System.Xml.Linq;
using System.Xml.XPath;
using BanSach.DTO;

namespace BanSach.DAL
{
    // Đọc / ghi Sach.xml bằng LINQ to XML, đếm tham chiếu bằng XPath
    public class SachDAL
    {
        private const string TenFile = "Sach.xml";
        private readonly XmlStore _store;

        public SachDAL(XmlStore store)
        {
            _store = store;
        }

        public List<Sach> LayTatCa()
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("Sach")
                .Select(ChuyenSangDTO)
                .ToList();
        }

        public Sach? LayTheoMa(string ma)
        {
            var phanTu = TimPhanTu(_store.Doc(TenFile), ma);
            return phanTu == null ? null : ChuyenSangDTO(phanTu);
        }

        // Tìm kiếm bằng XPath, không phân biệt dấu và hoa thường.
        // XPath 1.0 không có hàm bỏ dấu, nên dùng translate(chuỗi, 'ÀÁÂ...', 'aaa...') đổi từng chữ có dấu
        // sang chữ không dấu rồi mới contains(). Từ khóa đã được BLL bỏ dấu + chữ thường trước khi truyền vào.
        public List<string> TimMaSachBangXPath(string tuKhoaKhongDau)
        {
            var doc = _store.Doc(TenFile);
            var tuKhoa = XPathHelper.Literal(tuKhoaKhongDau);

            string BoDau(string truong) => $"translate({truong}, '{BangBoDau.Tu}', '{BangBoDau.Sang}')";

            var bieuThuc = "/DanhSachSach/Sach["
                + $"contains({BoDau("TenSach")}, {tuKhoa})"
                + $" or contains({BoDau("TacGia")}, {tuKhoa})"
                + $" or {BoDau("@MaSach")} = {tuKhoa}]";

            return doc.XPathSelectElements(bieuThuc)
                .Select(e => (string)e.Attribute("MaSach")!)
                .ToList();
        }

        // Bảng cho translate(): chữ hoa A-Z và mọi chữ Latin có dấu (gồm đủ chữ tiếng Việt) -> chữ thường không dấu
        private static readonly (string Tu, string Sang) BangBoDau = TaoBangBoDau();

        private static (string Tu, string Sang) TaoBangBoDau()
        {
            var tu = new System.Text.StringBuilder();
            var sang = new System.Text.StringBuilder();

            var cacKhoang = new (int Dau, int Cuoi)[]
            {
                ('A', 'Z'), (0x00C0, 0x00FF), (0x0102, 0x0103), (0x0110, 0x0111), (0x0128, 0x0129),
                (0x0168, 0x0169), (0x01A0, 0x01A1), (0x01AF, 0x01B0), (0x1EA0, 0x1EF9)
            };

            foreach (var (dau, cuoi) in cacKhoang)
            {
                for (var ma = dau; ma <= cuoi; ma++)
                {
                    var chu = (char)ma;
                    var goc = chu == 'Đ' || chu == 'đ'
                        ? 'd'
                        : char.ToLowerInvariant(chu.ToString().Normalize(System.Text.NormalizationForm.FormD)[0]);

                    if (goc >= 'a' && goc <= 'z' && goc != chu)
                    {
                        tu.Append(chu);
                        sang.Append(goc);
                    }
                }
            }

            return (tu.ToString(), sang.ToString());
        }

        // XPath: đếm số sách thuộc một thể loại
        public int DemTheoTheLoai(string maTheLoai)
        {
            var doc = _store.Doc(TenFile);
            var bieuThuc = $"count(/DanhSachSach/Sach[@MaTheLoai={XPathHelper.Literal(maTheLoai)}])";
            return Convert.ToInt32(doc.XPathEvaluate(bieuThuc));
        }

        // LINQ GroupBy: đếm số sách của từng thể loại trong một lần đọc file
        public Dictionary<string, int> DemTheoTungTheLoai()
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("Sach")
                .GroupBy(e => (string)e.Attribute("MaTheLoai")!)
                .ToDictionary(nhom => nhom.Key, nhom => nhom.Count());
        }

        public string TaoMaMoi()
        {
            return XmlStore.TaoMaMoi(_store.Doc(TenFile), "Sach", "MaSach", "S");
        }

        // Tìm kiếm nâng cao bằng XPath: ghép các điều kiện thành một vị từ [... and ...].
        // Mọi chuỗi người dùng nhập đều đi qua XPathHelper.Literal; số được ghi theo InvariantCulture.
        public (List<Sach> KetQua, string BieuThuc) TimKiemXPath(DieuKienTimSach dk)
        {
            var dieuKien = new List<string>();

            void ChuaChuoi(string truong, string? giaTri)
            {
                if (!string.IsNullOrWhiteSpace(giaTri))
                {
                    // giá trị đã được BLL bỏ dấu + chữ thường; trường trong XML cũng bỏ dấu bằng translate()
                    dieuKien.Add($"contains(translate({truong}, '{BangBoDau.Tu}', '{BangBoDau.Sang}'), {XPathHelper.Literal(giaTri)})");
                }
            }

            ChuaChuoi("TenSach", dk.TenSach);
            ChuaChuoi("TacGia", dk.TacGia);
            ChuaChuoi("NhaXuatBan", dk.NhaXuatBan);

            if (!string.IsNullOrWhiteSpace(dk.MaTheLoai))
            {
                dieuKien.Add($"@MaTheLoai = {XPathHelper.Literal(dk.MaTheLoai.Trim())}");
            }
            if (dk.GiaTu.HasValue)
            {
                dieuKien.Add($"number(GiaBan) >= {dk.GiaTu.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
            if (dk.GiaDen.HasValue)
            {
                dieuKien.Add($"number(GiaBan) <= {dk.GiaDen.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
            if (dk.NamTu.HasValue)
            {
                dieuKien.Add($"number(NamXuatBan) >= {dk.NamTu.Value}");
            }
            if (dk.NamDen.HasValue)
            {
                dieuKien.Add($"number(NamXuatBan) <= {dk.NamDen.Value}");
            }
            if (dk.ChiConHang)
            {
                dieuKien.Add("number(SoLuongTon) > 0");
            }

            var bieuThuc = "/DanhSachSach/Sach" + (dieuKien.Count > 0 ? "[" + string.Join(" and ", dieuKien) + "]" : string.Empty);
            var doc = _store.Doc(TenFile);
            var ketQua = doc.XPathSelectElements(bieuThuc).Select(ChuyenSangDTO).ToList();
            return (ketQua, bieuThuc);
        }

        public void Them(Sach sach)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                doc.Root!.Add(ChuyenSangXml(sach));
                _store.Luu(doc, TenFile);
            }
        }

        public bool CapNhat(Sach sach)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                var phanTu = TimPhanTu(doc, sach.MaSach);
                if (phanTu == null)
                {
                    return false;
                }

                phanTu.ReplaceWith(ChuyenSangXml(sach));
                _store.Luu(doc, TenFile);
                return true;
            }
        }

        // Cộng / trừ tồn kho nhiều sách trong một lần ghi file (số dương = nhập lại kho, số âm = xuất kho).
        // XSD chặn tồn kho âm nên nếu trừ quá số đang có thì cả lần ghi bị từ chối, file giữ nguyên.
        // Trả về tồn kho cũ / mới của từng sách để ghi nhật ký.
        public Dictionary<string, (int Cu, int Moi)> CongTonKho(IReadOnlyDictionary<string, int> thayDoi)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                var ketQua = new Dictionary<string, (int Cu, int Moi)>();

                foreach (var (maSach, soCong) in thayDoi)
                {
                    var tonKho = TimPhanTu(doc, maSach)?.Element("SoLuongTon");
                    if (tonKho == null)
                    {
                        continue;   // sách đã bị xóa: bỏ qua, BLL kiểm tra trước những trường hợp cần chặn
                    }

                    var cu = (int)tonKho;
                    tonKho.SetValue(cu + soCong);   // SetValue dùng XmlConvert, không phụ thuộc vùng miền
                    ketQua[maSach] = (cu, cu + soCong);
                }

                _store.Luu(doc, TenFile);
                return ketQua;
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
            return doc.Root!.Elements("Sach")
                .FirstOrDefault(e => (string?)e.Attribute("MaSach") == ma);
        }

        // Ép kiểu (int), (decimal) trên XElement dùng XmlConvert nên không phụ thuộc cài đặt vùng miền
        private static Sach ChuyenSangDTO(XElement e)
        {
            return new Sach
            {
                MaSach = (string)e.Attribute("MaSach")!,
                MaTheLoai = (string)e.Attribute("MaTheLoai")!,
                TenSach = (string)e.Element("TenSach")!,
                TacGia = (string)e.Element("TacGia")!,
                NhaXuatBan = (string)e.Element("NhaXuatBan")!,
                NamXuatBan = (int)e.Element("NamXuatBan")!,
                GiaBan = (decimal)e.Element("GiaBan")!,
                SoLuongTon = (int)e.Element("SoLuongTon")!,
                HinhAnh = (string?)e.Element("HinhAnh"),
                MoTa = (string?)e.Element("MoTa")
            };
        }

        // Thứ tự element phải đúng xs:sequence trong Sach.xsd
        private static XElement ChuyenSangXml(Sach s)
        {
            return new XElement("Sach",
                new XAttribute("MaSach", s.MaSach),
                new XAttribute("MaTheLoai", s.MaTheLoai),
                new XElement("TenSach", s.TenSach),
                new XElement("TacGia", s.TacGia),
                new XElement("NhaXuatBan", s.NhaXuatBan),
                new XElement("NamXuatBan", s.NamXuatBan),
                new XElement("GiaBan", s.GiaBan),
                new XElement("SoLuongTon", s.SoLuongTon),
                string.IsNullOrEmpty(s.HinhAnh) ? null : new XElement("HinhAnh", s.HinhAnh),
                string.IsNullOrEmpty(s.MoTa) ? null : new XElement("MoTa", s.MoTa));
        }
    }
}
