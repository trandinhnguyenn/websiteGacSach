namespace BanSach.DTO
{
    // Mã trạng thái lưu trong XML là chuỗi không dấu (khớp xs:enumeration trong DonHang.xsd),
    // chỉ khi hiển thị mới đổi sang tiếng Việt có dấu
    public static class TrangThaiDonHang
    {
        public const string ChoXacNhan = "ChoXacNhan";
        public const string DaXacNhan = "DaXacNhan";
        public const string DangGiao = "DangGiao";
        public const string HoanThanh = "HoanThanh";
        public const string DaHuy = "DaHuy";

        public static readonly string[] TatCa = { ChoXacNhan, DaXacNhan, DangGiao, HoanThanh, DaHuy };

        public static string TenHienThi(string trangThai)
        {
            switch (trangThai)
            {
                case ChoXacNhan: return "Chờ xác nhận";
                case DaXacNhan: return "Đã xác nhận";
                case DangGiao: return "Đang giao";
                case HoanThanh: return "Hoàn thành";
                case DaHuy: return "Đã hủy";
                default: return trangThai;
            }
        }
    }
}
