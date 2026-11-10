using Microsoft.Extensions.Caching.Memory;

namespace BanSach.Web.Services
{
    // Chống dò mật khẩu, hai bộ đếm độc lập, cùng quy tắc: sai 5 lần liên tiếp thì khóa 5 phút, làm đúng thì xóa bộ đếm.
    // - Đăng nhập: đếm theo cặp (IP + tên đăng nhập). Vì tính riêng từng tài khoản, cả lớp dùng chung một IP
    //   cũng không khóa lẫn nhau.
    // - Đổi mật khẩu: đếm theo mã tài khoản (người dùng đã đăng nhập). Chặn trường hợp ai đó ngồi vào máy đang
    //   đăng nhập sẵn rồi thử dần "mật khẩu hiện tại" để chiếm tài khoản.
    public class ChongDoMatKhau
    {
        public const int SoLanSaiToiDa = 5;
        public static readonly TimeSpan ThoiGianKhoa = TimeSpan.FromMinutes(5);

        private readonly IMemoryCache _boNho;
        private readonly object _khoa = new object();

        public ChongDoMatKhau(IMemoryCache boNho)
        {
            _boNho = boNho;
        }

        public bool DangBiKhoa(string ip, string? tenDangNhap) => DaQuaGioiHan(KhoaDangNhap(ip, tenDangNhap));
        public void GhiLanSai(string ip, string? tenDangNhap) => CongLanSai(KhoaDangNhap(ip, tenDangNhap));
        public void XoaBoDem(string ip, string? tenDangNhap) => _boNho.Remove(KhoaDangNhap(ip, tenDangNhap));

        public bool DoiMatKhauBiKhoa(string maTaiKhoan) => DaQuaGioiHan(KhoaDoiMatKhau(maTaiKhoan));
        public void GhiDoiMatKhauSai(string maTaiKhoan) => CongLanSai(KhoaDoiMatKhau(maTaiKhoan));
        public void XoaBoDemDoiMatKhau(string maTaiKhoan) => _boNho.Remove(KhoaDoiMatKhau(maTaiKhoan));

        private bool DaQuaGioiHan(string khoa)
        {
            return _boNho.TryGetValue(khoa, out int soLanSai) && soLanSai >= SoLanSaiToiDa;
        }

        // Mỗi lần sai cộng 1 và đặt lại hạn 5 phút tính từ lần sai gần nhất
        private void CongLanSai(string khoa)
        {
            lock (_khoa)
            {
                var soLanSai = _boNho.TryGetValue(khoa, out int cu) ? cu + 1 : 1;
                _boNho.Set(khoa, soLanSai, ThoiGianKhoa);
            }
        }

        private static string KhoaDangNhap(string ip, string? tenDangNhap)
        {
            return $"dang-nhap-sai:{ip}:{(tenDangNhap ?? string.Empty).Trim().ToLowerInvariant()}";
        }

        private static string KhoaDoiMatKhau(string maTaiKhoan)
        {
            return $"doi-mat-khau-sai:{maTaiKhoan}";
        }
    }
}
