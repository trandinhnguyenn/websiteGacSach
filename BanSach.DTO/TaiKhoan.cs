namespace BanSach.DTO
{
    // Ánh xạ phần tử <TaiKhoan MaTaiKhoan="..." VaiTro="..." MaKhachHang="..."> trong TaiKhoan.xml
    public class TaiKhoan
    {
        public string MaTaiKhoan { get; set; } = string.Empty;  // attribute (khóa)
        public string VaiTro { get; set; } = string.Empty;      // attribute: Admin | KhachHang
        public string? MaKhachHang { get; set; }                // attribute, chỉ có khi VaiTro = KhachHang
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhauHash { get; set; } = string.Empty; // PBKDF2 (base64)
        public string Salt { get; set; } = string.Empty;        // base64
    }
}
