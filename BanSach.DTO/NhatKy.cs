namespace BanSach.DTO
{
    // Ánh xạ phần tử <NhatKy MaNhatKy="..." MaTaiKhoan="..." MaDoiTuong="..."> trong NhatKy.xml
    public class NhatKy
    {
        public string MaNhatKy { get; set; } = string.Empty;    // attribute (khóa)
        public string MaTaiKhoan { get; set; } = string.Empty;  // attribute (khóa ngoại -> TaiKhoan.xml)
        public string? MaDoiTuong { get; set; }                 // attribute, mã bản ghi bị tác động
        public DateTime ThoiGian { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string HanhDong { get; set; } = string.Empty;    // Them | Sua | Xoa | DoiTrangThai | HuyDon | NhapXml | DangKy | DoiMatKhau
        public string DoiTuong { get; set; } = string.Empty;    // TheLoai | Sach | KhachHang | DonHang | DuLieu | TaiKhoan
        public string NoiDung { get; set; } = string.Empty;
    }

    // Giá trị hợp lệ của HanhDong / DoiTuong (khớp xs:enumeration trong NhatKy.xsd)
    public static class HanhDongNhatKy
    {
        public const string Them = "Them";
        public const string Sua = "Sua";
        public const string Xoa = "Xoa";
        public const string DoiTrangThai = "DoiTrangThai";
        public const string HuyDon = "HuyDon";
        public const string NhapXml = "NhapXml";
        public const string DangKy = "DangKy";
        public const string DoiMatKhau = "DoiMatKhau";
    }

    public static class DoiTuongNhatKy
    {
        public const string TheLoai = "TheLoai";
        public const string Sach = "Sach";
        public const string KhachHang = "KhachHang";
        public const string DonHang = "DonHang";
        public const string DuLieu = "DuLieu";
        public const string TaiKhoan = "TaiKhoan";
    }
}
