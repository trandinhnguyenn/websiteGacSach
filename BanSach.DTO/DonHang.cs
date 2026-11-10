namespace BanSach.DTO
{
    // Ánh xạ phần tử <DonHang MaDonHang="..." MaKhachHang="..."> trong DonHang.xml
    public class DonHang
    {
        public string MaDonHang { get; set; } = string.Empty;   // attribute (khóa)
        public string MaKhachHang { get; set; } = string.Empty; // attribute (khóa ngoại -> KhachHang.xml)
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; } = TrangThaiDonHang.ChoXacNhan;
        public string NguoiNhan { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DiaChiGiao { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public List<ChiTietDonHang> ChiTiet { get; set; } = new List<ChiTietDonHang>();

        public decimal TongTien => ChiTiet.Sum(ct => ct.ThanhTien); // tính ra, không lưu trong XML
    }
}
