using System.Text.Json;
using BanSach.DTO;

namespace BanSach.Web.Services
{
    // Giỏ hàng lưu trong session (cookie BanSach.Phien chỉ chứa mã phiên, dữ liệu nằm ở server).
    // Chưa đăng nhập vẫn có giỏ; đăng nhập xong giỏ vẫn còn vì cùng một phiên.
    public static class GioHangSession
    {
        private const string Khoa = "GioHang";

        public static List<GioHangDong> Lay(HttpContext context)
        {
            var chuoi = context.Session.GetString(Khoa);
            if (string.IsNullOrEmpty(chuoi))
            {
                return new List<GioHangDong>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<GioHangDong>>(chuoi) ?? new List<GioHangDong>();
            }
            catch (JsonException)
            {
                return new List<GioHangDong>();
            }
        }

        public static void Luu(HttpContext context, List<GioHangDong> gio)
        {
            context.Session.SetString(Khoa, JsonSerializer.Serialize(gio));
        }

        public static void XoaHet(HttpContext context)
        {
            context.Session.Remove(Khoa);
        }
    }
}
