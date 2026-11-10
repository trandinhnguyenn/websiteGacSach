namespace BanSach.BLL
{
    // Quy tắc cho mật khẩu MỚI (đăng ký, đổi mật khẩu). Tài khoản đặt mật khẩu từ trước khi có quy tắc này
    // vẫn đăng nhập được, nhưng sẽ được nhắc đổi (AuthApiController kiểm tra lúc đăng nhập).
    // wwwroot/js/site.js (quyTacMatKhau) có bản sao 3 quy tắc đầu để báo ngay khi gõ; server vẫn là nơi quyết định.
    public static class ChinhSachMatKhau
    {
        public const int DoDaiToiThieu = 8;
        public const int DoDaiToiDa = 100;

        // Những mật khẩu bị dò đầu tiên mà vẫn lọt qua quy tắc "8 ký tự, có chữ và số"
        private static readonly HashSet<string> MatKhauPhoBien = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "12345678a", "a12345678", "123456789a", "1234567a", "a1234567", "123456aa", "aa123456",
            "abc12345", "abcd1234", "abc123456", "abc123abc", "123abc123", "1q2w3e4r", "1qaz2wsx",
            "qwerty123", "qwerty12", "qwer1234", "asdf1234", "zxcv1234", "password1", "password12",
            "password123", "passw0rd", "p@ssw0rd", "matkhau1", "matkhau12", "matkhau123",
            "admin123", "admin1234", "admin12345", "iloveyou1", "anhyeuem1", "anhyeuem123", "emyeuanh1",
            "welcome1", "letmein1", "vietnam1", "vietnam123", "hanoi123", "saigon123", "danang123"
        };

        // Trả về các quy tắc chưa đạt (rỗng là mật khẩu dùng được)
        public static List<string> KiemTra(string? matKhau, string? tenDangNhap)
        {
            var loi = new List<string>();
            if (string.IsNullOrEmpty(matKhau) || matKhau.Length < DoDaiToiThieu || matKhau.Length > DoDaiToiDa)
            {
                loi.Add($"Mật khẩu từ {DoDaiToiThieu} đến {DoDaiToiDa} ký tự");
            }
            if (string.IsNullOrEmpty(matKhau))
            {
                return loi;
            }

            if (!matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
            {
                loi.Add("Mật khẩu phải có cả chữ và số");
            }

            var ten = (tenDangNhap ?? string.Empty).Trim();
            if (ten.Length >= 3 && matKhau.Contains(ten, StringComparison.OrdinalIgnoreCase))
            {
                loi.Add("Mật khẩu không được chứa tên đăng nhập");
            }

            // Ít hơn 4 ký tự khác nhau (kiểu aaaa1111) cũng dễ đoán như mật khẩu phổ biến.
            // Chỉ xét khi đã đủ dài, để mật khẩu quá ngắn không bị báo thừa một lỗi nữa
            if (matKhau.Length >= DoDaiToiThieu && (MatKhauPhoBien.Contains(matKhau) || matKhau.Distinct().Count() < 4))
            {
                loi.Add("Mật khẩu này quá phổ biến, dễ bị đoán. Hãy chọn mật khẩu khác");
            }
            return loi;
        }

        public static bool DatYeuCau(string? matKhau, string? tenDangNhap)
        {
            return KiemTra(matKhau, tenDangNhap).Count == 0;
        }
    }
}
