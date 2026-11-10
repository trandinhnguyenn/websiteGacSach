namespace BanSach.DTO
{
    // Người đang đăng nhập, lấy từ cookie (claims) ở tầng Web rồi truyền xuống BLL
    // để BLL ghi nhật ký và kiểm tra quyền sở hữu. Không lưu trong XML.
    public class NguoiThucHien
    {
        public string MaTaiKhoan { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public string? MaKhachHang { get; set; }
    }
}
