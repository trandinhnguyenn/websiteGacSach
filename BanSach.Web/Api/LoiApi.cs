using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BanSach.Web.Api
{
    // Định dạng lỗi thống nhất cho mọi API: { "thongBao": "...", "chiTiet": ["...", "..."] }
    public class LoiApi
    {
        public string ThongBao { get; set; } = string.Empty;
        public List<string> ChiTiet { get; set; } = new List<string>();

        public LoiApi()
        {
        }

        public LoiApi(string thongBao, List<string>? chiTiet = null)
        {
            ThongBao = thongBao;
            ChiTiet = chiTiet ?? new List<string>();
        }

        // Dữ liệu gửi lên sai kiểu (vd GiaBan = "abc") -> 400 với thông báo tiếng Việt thay cho mặc định tiếng Anh
        public static IActionResult TuModelState(ModelStateDictionary modelState)
        {
            // Request vượt giới hạn dung lượng (vd ảnh > 2MB) cũng rơi vào đây
            var quaLon = modelState.Values
                .SelectMany(muc => muc.Errors)
                .Any(loi => loi.Exception is InvalidDataException
                    || loi.Exception is BadHttpRequestException
                    || loi.ErrorMessage.Contains("too large", StringComparison.OrdinalIgnoreCase));
            if (quaLon)
            {
                return new ObjectResult(new LoiApi("Dữ liệu gửi lên quá lớn (ảnh bìa tối đa 2MB)"))
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge
                };
            }

            var chiTiet = modelState
                .Where(muc => muc.Value != null && muc.Value.Errors.Count > 0)
                .Select(muc => muc.Key.Length == 0
                    ? "Thiếu dữ liệu gửi lên"
                    : $"{muc.Key.Replace("$.", string.Empty)}: giá trị không hợp lệ")
                .Distinct()
                .ToList();

            return new BadRequestObjectResult(new LoiApi("Dữ liệu gửi lên không đúng định dạng", chiTiet));
        }

        // Ghi lỗi thẳng ra response (dùng ở chỗ không có ActionResult như sự kiện cookie, rate limiter)
        public static Task GhiAsync(HttpResponse response, int maTrangThai, string thongBao)
        {
            response.StatusCode = maTrangThai;
            return response.WriteAsJsonAsync(new LoiApi(thongBao));
        }
    }
}
