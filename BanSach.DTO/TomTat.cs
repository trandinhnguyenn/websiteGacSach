namespace BanSach.DTO
{
    // Các lớp "tóm tắt" dùng để trả danh sách cho trang quản trị.
    // Kế thừa DTO gốc và thêm vài số liệu tính từ file XML khác (không lưu trong XML).

    public class TheLoaiTomTat : TheLoai
    {
        public int SoLuongSach { get; set; }        // đếm trong Sach.xml theo MaTheLoai
    }

    public class KhachHangTomTat : KhachHang
    {
        public string? TenDangNhap { get; set; }    // lấy từ TaiKhoan.xml, null nếu chưa có tài khoản
        public int SoDonHang { get; set; }          // đếm trong DonHang.xml theo MaKhachHang
    }
}
