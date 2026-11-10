namespace BanSach.DTO
{
    // Ánh xạ phần tử <KhachHang MaKhachHang="..."> trong KhachHang.xml
    public class KhachHang
    {
        public string MaKhachHang { get; set; } = string.Empty; // attribute (khóa)
        public string HoTen { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty; // duy nhất (xs:unique)
        public string? Email { get; set; }                      // không bắt buộc, có thì duy nhất
        public string DiaChi { get; set; } = string.Empty;
    }
}
