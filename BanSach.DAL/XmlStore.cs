using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace BanSach.DAL
{
    // Lỗi khi dữ liệu sắp ghi không qua được XSD
    public class LoiXsdException : Exception
    {
        public List<string> ChiTiet { get; }

        public LoiXsdException(string tenFile, List<string> chiTiet)
            : base($"Dữ liệu ghi vào {tenFile} không hợp lệ theo {Path.ChangeExtension(tenFile, ".xsd")}")
        {
            ChiTiet = chiTiet;
        }
    }

    // Lớp dùng chung để đọc / kiểm tra XSD / ghi file XML trong App_Data.
    // Mọi DAL đều đi qua lớp này, không DAL nào tự mở file.
    public class XmlStore
    {
        // Khóa toàn cục: mọi thao tác đọc - sửa - ghi XML đều nằm trong khóa này,
        // tránh 2 request cùng ghi một file làm mất dữ liệu của nhau.
        // Dùng lock (Monitor) nên cùng một luồng có thể lock lồng nhau (BLL lock rồi gọi DAL lock tiếp).
        public static readonly object Khoa = new object();

        private readonly string _thuMuc;
        private readonly Dictionary<string, XmlSchemaSet> _boNhoXsd = new Dictionary<string, XmlSchemaSet>();

        public XmlStore(string thuMucDuLieu)
        {
            _thuMuc = thuMucDuLieu;
        }

        public string ThuMuc => _thuMuc;

        public string DuongDan(string tenFile) => Path.Combine(_thuMuc, tenFile);

        // Thiết lập đọc an toàn: cấm DTD (chống XXE và "billion laughs"), không tải tài nguyên bên ngoài
        public static XmlReaderSettings CaiDatDocAnToan()
        {
            return new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreWhitespace = true
            };
        }

        // Đọc file XML thành XDocument (LINQ to XML)
        public XDocument Doc(string tenFile)
        {
            lock (Khoa)
            {
                using (var reader = XmlReader.Create(DuongDan(tenFile), CaiDatDocAnToan()))
                {
                    return XDocument.Load(reader);
                }
            }
        }

        // Đọc file XML người dùng tải lên (nhập dữ liệu): cùng cài đặt an toàn, giữ số dòng để báo lỗi XSD đúng chỗ,
        // giới hạn số ký tự để file quá lớn không làm đầy bộ nhớ
        public static XDocument DocTuLuong(Stream luong)
        {
            var caiDat = CaiDatDocAnToan();
            caiDat.MaxCharactersInDocument = 10_000_000;
            using (var reader = XmlReader.Create(luong, caiDat))
            {
                return XDocument.Load(reader, LoadOptions.SetLineInfo);
            }
        }

        // Nội dung thô của một file trong App_Data (để tải về). Đọc trong khóa để không lấy phải file đang ghi dở.
        public byte[] DocByte(string tenFile)
        {
            lock (Khoa)
            {
                return File.ReadAllBytes(DuongDan(tenFile));
            }
        }

        public FileInfo ThongTinFile(string tenFile)
        {
            return new FileInfo(DuongDan(tenFile));
        }

        // Kiểm tra theo XSD và trả về danh sách lỗi (kèm số dòng nếu tài liệu được đọc có SetLineInfo), không ném lỗi
        public List<string> LayLoiXsd(XDocument doc, string tenFile)
        {
            var danhSach = new List<(int Dong, string Goc)>();
            doc.Validate(LayXsd(tenFile), (sender, e) =>
            {
                if (e.Severity != XmlSeverityType.Error)
                {
                    return;
                }
                danhSach.Add((e.Exception?.LineNumber ?? 0, e.Message));
            });

            // Thông báo gốc của .NET là tiếng Anh: dịch sang tiếng Việt. Lỗi "khóa rỗng" đi kèm một lỗi khác trên cùng dòng
            // (thường là chính giá trị khóa bị sai kiểu) chỉ là hệ quả, bỏ đi cho danh sách gọn.
            // Lỗi trùng khóa .NET báo ở cuối phạm vi kiểm tra: xếp lại theo số dòng (OrderBy giữ thứ tự cho lỗi cùng dòng)
            var loi = new List<string>();
            foreach (var (dong, goc) in danhSach.OrderBy(x => x.Dong > 0 ? x.Dong : int.MaxValue))
            {
                if (DichLoiXsd.LaLoiKhoaRong(goc) && dong > 0
                    && danhSach.Any(x => x.Dong == dong && !DichLoiXsd.LaLoiKhoaRong(x.Goc)))
                {
                    continue;
                }
                var thongBao = DichLoiXsd.Dich(goc);
                loi.Add(dong > 0 ? $"Dòng {dong}: {thongBao}" : thongBao);
            }
            return loi;
        }

        // Kiểm tra XDocument theo file XSD cùng tên (Sach.xml -> Sach.xsd), có lỗi thì ném LoiXsdException
        public void KiemTraXsd(XDocument doc, string tenFile)
        {
            var loi = LayLoiXsd(doc, tenFile);
            if (loi.Count > 0)
            {
                throw new LoiXsdException(tenFile, loi);
            }
        }

        // Ghi an toàn: kiểm tra XSD -> ghi ra file tạm -> File.Replace thay file thật, file cũ giữ lại dạng .bak.
        // Nếu lỗi giữa chừng thì file thật vẫn nguyên vẹn.
        public void Luu(XDocument doc, string tenFile)
        {
            lock (Khoa)
            {
                KiemTraXsd(doc, tenFile);

                var duongDan = DuongDan(tenFile);
                var fileTam = duongDan + ".tmp";
                var caiDatGhi = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = new UTF8Encoding(false)
                };

                using (var writer = XmlWriter.Create(fileTam, caiDatGhi))
                {
                    doc.Save(writer);
                }

                if (File.Exists(duongDan))
                {
                    File.Replace(fileTam, duongDan, duongDan + ".bak");
                }
                else
                {
                    File.Move(fileTam, duongDan);
                }
            }
        }

        // Sinh mã mới: lấy số lớn nhất hiện có + 1 (TL005 -> TL006). Gọi bên trong lock để không trùng mã.
        public static string TaoMaMoi(XDocument doc, string tenPhanTu, string tenThuocTinh, string tienTo)
        {
            var soLonNhat = doc.Root!.Elements(tenPhanTu)
                .Select(e => (string?)e.Attribute(tenThuocTinh))
                .Where(ma => ma != null && ma.StartsWith(tienTo))
                .Select(ma => int.TryParse(ma!.Substring(tienTo.Length), out var so) ? so : 0)
                .DefaultIfEmpty(0)
                .Max();

            return tienTo + (soLonNhat + 1).ToString("000");
        }

        // Nạp và biên dịch XSD một lần rồi giữ lại để dùng cho các lần kiểm tra sau
        private XmlSchemaSet LayXsd(string tenFile)
        {
            var tenXsd = Path.ChangeExtension(tenFile, ".xsd");

            lock (_boNhoXsd)
            {
                if (!_boNhoXsd.TryGetValue(tenXsd, out var tapXsd))
                {
                    tapXsd = new XmlSchemaSet { XmlResolver = null };
                    using (var reader = XmlReader.Create(DuongDan(tenXsd), CaiDatDocAnToan()))
                    {
                        tapXsd.Add(null, reader);
                    }
                    tapXsd.Compile();
                    _boNhoXsd[tenXsd] = tapXsd;
                }
                return tapXsd;
            }
        }
    }
}
