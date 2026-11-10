namespace BanSach.DTO
{
    // Khách tự đăng ký: thông tin tài khoản + hồ sơ
    public class DangKyYeuCau
    {
        public string? TenDangNhap { get; set; }
        public string? MatKhau { get; set; }
        public string? HoTen { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
    }

    public class DoiMatKhauYeuCau
    {
        public string? MatKhauCu { get; set; }
        public string? MatKhauMoi { get; set; }
    }

    // Điều kiện tìm kiếm nâng cao, BLL/DAL dựng thành một biểu thức XPath
    public class DieuKienTimSach
    {
        public string? TenSach { get; set; }
        public string? TacGia { get; set; }
        public string? NhaXuatBan { get; set; }
        public string? MaTheLoai { get; set; }
        public decimal? GiaTu { get; set; }
        public decimal? GiaDen { get; set; }
        public int? NamTu { get; set; }
        public int? NamDen { get; set; }
        public bool ChiConHang { get; set; }
    }

    // Kết quả tìm XPath: trả luôn biểu thức đã chạy để hiện cho người xem
    public class KetQuaTimXPath
    {
        public string BieuThuc { get; set; } = string.Empty;
        public int SoKetQua { get; set; }
        public List<Sach> DuLieu { get; set; } = new List<Sach>();
    }
}
