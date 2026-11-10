using System.Xml.Linq;
using BanSach.DTO;

namespace BanSach.DAL
{
    // Đọc / ghi NhatKy.xml (nhật ký thao tác của admin). Chỉ thêm, không sửa / xóa.
    public class NhatKyDAL
    {
        private const string TenFile = "NhatKy.xml";
        private readonly XmlStore _store;

        public NhatKyDAL(XmlStore store)
        {
            _store = store;
        }

        public List<NhatKy> LayTatCa()
        {
            var doc = _store.Doc(TenFile);
            return doc.Root!.Elements("NhatKy")
                .Select(ChuyenSangDTO)
                .ToList();
        }

        public string TaoMaMoi()
        {
            return XmlStore.TaoMaMoi(_store.Doc(TenFile), "NhatKy", "MaNhatKy", "NK");
        }

        public void Them(NhatKy nhatKy)
        {
            lock (XmlStore.Khoa)
            {
                var doc = _store.Doc(TenFile);
                doc.Root!.Add(ChuyenSangXml(nhatKy));
                _store.Luu(doc, TenFile);
            }
        }

        private static NhatKy ChuyenSangDTO(XElement e)
        {
            return new NhatKy
            {
                MaNhatKy = (string)e.Attribute("MaNhatKy")!,
                MaTaiKhoan = (string)e.Attribute("MaTaiKhoan")!,
                MaDoiTuong = (string?)e.Attribute("MaDoiTuong"),
                ThoiGian = (DateTime)e.Element("ThoiGian")!,
                TenDangNhap = (string)e.Element("TenDangNhap")!,
                HanhDong = (string)e.Element("HanhDong")!,
                DoiTuong = (string)e.Element("DoiTuong")!,
                NoiDung = (string)e.Element("NoiDung")!
            };
        }

        private static XElement ChuyenSangXml(NhatKy nk)
        {
            return new XElement("NhatKy",
                new XAttribute("MaNhatKy", nk.MaNhatKy),
                new XAttribute("MaTaiKhoan", nk.MaTaiKhoan),
                string.IsNullOrEmpty(nk.MaDoiTuong) ? null : new XAttribute("MaDoiTuong", nk.MaDoiTuong),
                new XElement("ThoiGian", nk.ThoiGian.ToString("yyyy-MM-dd'T'HH:mm:ss")),
                new XElement("TenDangNhap", nk.TenDangNhap),
                new XElement("HanhDong", nk.HanhDong),
                new XElement("DoiTuong", nk.DoiTuong),
                new XElement("NoiDung", nk.NoiDung));
        }
    }
}
