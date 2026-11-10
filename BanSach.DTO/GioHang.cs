namespace BanSach.DTO
{
    // Một dòng giỏ hàng lưu trong session (chỉ mã sách + số lượng, giá luôn lấy mới từ Sach.xml)
    public class GioHangDong
    {
        public string MaSach { get; set; } = string.Empty;
        public int SoLuong { get; set; }
    }

    // Dòng giỏ hàng đã ghép thông tin sách để hiển thị
    public class GioHangChiTiet
    {
        public string MaSach { get; set; } = string.Empty;
        public string TenSach { get; set; } = string.Empty;
        public string TacGia { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public int SoLuongTon { get; set; }
        public bool HetHang { get; set; }

        public decimal ThanhTien => SoLuong * DonGia;
    }

    public class GioHang
    {
        public List<GioHangChiTiet> Dong { get; set; } = new List<GioHangChiTiet>();
        public int TongSoLuong { get; set; }
        public decimal TongTien { get; set; }
        public List<string> ThongBao { get; set; } = new List<string>();   // vd: sách đã ngừng bán, đã giảm số lượng
    }
}
