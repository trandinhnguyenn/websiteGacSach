namespace BanSach.DTO
{
    // Một file dữ liệu trong App_Data, hiển thị ở trang Quản trị > Dữ liệu XML. Không lưu trong XML.
    public class TepDuLieu
    {
        public string TenFile { get; set; } = string.Empty;     // vd Sach.xml
        public string TenXsd { get; set; } = string.Empty;      // vd Sach.xsd
        public string NoiDung { get; set; } = string.Empty;     // mô tả ngắn: "Danh mục sách"
        public int SoBanGhi { get; set; }
        public long KichThuoc { get; set; }                     // byte
        public DateTime SuaLuc { get; set; }
        public bool TaiDuoc { get; set; }                       // TaiKhoan.xml chứa mật khẩu đã băm nên không cho tải
        public bool NhapDuoc { get; set; }                      // chỉ dữ liệu danh mục mới cho nhập
        public string? LyDoKhongNhap { get; set; }
    }

    // Kết quả kiểm tra / nhập một file XML tải lên
    public class KetQuaNhapXml
    {
        public string TenFile { get; set; } = string.Empty;
        public string CheDo { get; set; } = string.Empty;       // gop | thay
        public bool DaNhap { get; set; }                        // false = mới chỉ kiểm tra, hoặc không có gì thay đổi
        public int SoBanGhiTrongFile { get; set; }
        public int ThemMoi { get; set; }
        public int CapNhat { get; set; }
        public int GiuNguyen { get; set; }
        public int Xoa { get; set; }                            // chỉ có ở chế độ thay toàn bộ
        public List<string> CanhBao { get; set; } = new List<string>();
    }

    public static class CheDoNhapXml
    {
        public const string Gop = "gop";        // thêm bản ghi mới, cập nhật bản ghi trùng mã, giữ bản ghi không có trong file
        public const string Thay = "thay";      // file tải lên thay toàn bộ dữ liệu hiện có
    }
}
