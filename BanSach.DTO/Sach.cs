namespace BanSach.DTO
{
    // Ánh xạ phần tử <Sach MaSach="..." MaTheLoai="..."> trong Sach.xml
    public class Sach
    {
        public string MaSach { get; set; } = string.Empty;      // attribute (khóa)
        public string MaTheLoai { get; set; } = string.Empty;   // attribute (khóa ngoại -> TheLoai.xml)
        public string TenSach { get; set; } = string.Empty;
        public string TacGia { get; set; } = string.Empty;
        public string NhaXuatBan { get; set; } = string.Empty;
        public int NamXuatBan { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }
        public string? HinhAnh { get; set; }                    // tên file ảnh trong wwwroot/images/sach
        public string? MoTa { get; set; }
    }
}
