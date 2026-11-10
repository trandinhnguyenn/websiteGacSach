namespace BanSach.DTO
{
    // Ánh xạ phần tử <TheLoai MaTheLoai="..."> trong TheLoai.xml
    public class TheLoai
    {
        public string MaTheLoai { get; set; } = string.Empty;   // attribute
        public string TenTheLoai { get; set; } = string.Empty;  // element
        public string? MoTa { get; set; }                       // element, không bắt buộc
    }
}
