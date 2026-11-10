namespace BanSach.BLL
{
    // Loại lỗi nghiệp vụ. Tầng Web đổi sang mã HTTP: KhongHopLe=400, KhongCoQuyen=403, KhongTimThay=404, XungDot=409
    public enum LoaiLoi
    {
        KhongHopLe,
        KhongCoQuyen,
        KhongTimThay,
        XungDot
    }

    // BLL ném lỗi này khi vi phạm quy tắc nghiệp vụ; thông báo viết sẵn tiếng Việt để hiện thẳng cho người dùng
    public class LoiNghiepVuException : Exception
    {
        public LoaiLoi Loai { get; }
        public List<string> ChiTiet { get; }

        public LoiNghiepVuException(LoaiLoi loai, string thongBao, List<string>? chiTiet = null)
            : base(thongBao)
        {
            Loai = loai;
            ChiTiet = chiTiet ?? new List<string>();
        }

        public static LoiNghiepVuException KhongTimThay(string thongBao)
        {
            return new LoiNghiepVuException(LoaiLoi.KhongTimThay, thongBao);
        }

        public static LoiNghiepVuException XungDot(string thongBao)
        {
            return new LoiNghiepVuException(LoaiLoi.XungDot, thongBao);
        }
    }

    // Đổi mật khẩu mà nhập sai mật khẩu hiện tại (vẫn là lỗi 400). Tách lớp riêng để tầng Web đếm số lần sai
    // mà không phải so chuỗi thông báo
    public class SaiMatKhauHienTaiException : LoiNghiepVuException
    {
        public SaiMatKhauHienTaiException()
            : base(LoaiLoi.KhongHopLe, "Mật khẩu hiện tại không đúng")
        {
        }
    }
}
