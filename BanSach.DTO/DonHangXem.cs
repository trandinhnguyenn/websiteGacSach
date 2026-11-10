namespace BanSach.DTO
{
    // Đơn hàng để hiển thị: ghép tên sách, ảnh bìa (Sach.xml) và tên trạng thái có dấu. Không lưu trong XML.
    public class DonHangXem
    {
        public string MaDonHang { get; set; } = string.Empty;
        public string MaKhachHang { get; set; } = string.Empty;
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string TenTrangThai { get; set; } = string.Empty;
        public string NguoiNhan { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DiaChiGiao { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public List<ChiTietDonHangXem> ChiTiet { get; set; } = new List<ChiTietDonHangXem>();
        public int TongSoLuong { get; set; }
        public decimal TongTien { get; set; }
        public bool CoTheHuy { get; set; }      // khách chỉ hủy được khi đơn còn Chờ xác nhận
    }

    public class ChiTietDonHangXem : ChiTietDonHang
    {
        public string TenSach { get; set; } = string.Empty;
        public string TacGia { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
    }
}

namespace BanSach.DTO
{
    // Đơn hàng cho trang quản trị: thêm tên khách, các bước admin được chuyển tiếp và sách đang thiếu hàng
    public class DonHangQuanTri : DonHangXem
    {
        public string TenKhachHang { get; set; } = string.Empty;   // lấy từ KhachHang.xml theo MaKhachHang
        public List<string> BuocTiep { get; set; } = new List<string>(); // mã trạng thái admin chuyển sang được
        public bool DaTruKho { get; set; }                          // đã xác nhận / đang giao: hủy thì phải hoàn kho
        public List<string> ThieuHang { get; set; } = new List<string>(); // chỉ có khi Chờ xác nhận mà kho không đủ
    }

    // Số đơn của một trạng thái (dùng cho các nút lọc và trang tổng quan)
    public class DemTrangThaiDonHang
    {
        public string TrangThai { get; set; } = string.Empty;
        public string TenTrangThai { get; set; } = string.Empty;
        public int SoDon { get; set; }
    }

    // Danh sách đơn cho admin: một trang kết quả + số đơn theo từng trạng thái + doanh thu
    public class DanhSachDonHangQuanTri : TrangKetQua<DonHangQuanTri>
    {
        public List<DemTrangThaiDonHang> DemTheoTrangThai { get; set; } = new List<DemTrangThaiDonHang>();
        public decimal DoanhThu { get; set; }       // tổng tiền các đơn Hoàn thành (toàn bộ, không theo bộ lọc)
    }
}
