namespace BanSach.DTO
{
    // Ánh xạ phần tử con <ChiTiet MaSach="..."> lồng trong <DonHang>
    public class ChiTietDonHang
    {
        public string MaSach { get; set; } = string.Empty;  // attribute (khóa ngoại -> Sach.xml)
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }                 // giá lúc đặt, không đổi khi sách đổi giá

        public decimal ThanhTien => SoLuong * DonGia;       // tính ra, không lưu trong XML
    }
}
