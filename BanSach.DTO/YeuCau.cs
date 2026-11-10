namespace BanSach.DTO
{
    // Dữ liệu client gửi lên cho các API phía khách hàng

    public class GioHangYeuCau
    {
        public string? MaSach { get; set; }
        public int SoLuong { get; set; } = 1;
    }

    public class DatHangYeuCau
    {
        public string? NguoiNhan { get; set; }
        public string? SoDienThoai { get; set; }
        public string? DiaChiGiao { get; set; }
        public string? GhiChu { get; set; }
    }

    // Admin chuyển trạng thái đơn: ChoXacNhan -> DaXacNhan -> DangGiao -> HoanThanh, hoặc sang DaHuy
    public class DoiTrangThaiYeuCau
    {
        public string? TrangThai { get; set; }
    }
}
