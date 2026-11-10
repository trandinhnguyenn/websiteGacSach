using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace BanSach.BLL
{
    // Chuẩn hóa chuỗi người dùng nhập trước khi ghi XML
    public static class ChuanHoa
    {
        private static readonly Regex KhoangTrang = new Regex(@"\s+");

        // Bỏ ký tự XML không cho phép (vd \u0001), nếu để lại thì XmlWriter báo lỗi khi ghi file
        public static string BoKyTuCam(string chuoi)
        {
            return new string(chuoi.Where(c => XmlConvert.IsXmlChar(c) || char.IsSurrogate(c)).ToArray());
        }

        // Cho các trường kiểu xs:token: bỏ khoảng trắng đầu/cuối, gộp nhiều khoảng trắng thành 1.
        // .NET từ chối xs:token có khoảng trắng thừa nên bước này là bắt buộc.
        public static string MotDong(string? chuoi)
        {
            return KhoangTrang.Replace(BoKyTuCam(chuoi ?? string.Empty), " ").Trim();
        }

        public static string? MotDongTuyChon(string? chuoi)
        {
            var ketQua = MotDong(chuoi);
            return ketQua.Length == 0 ? null : ketQua;
        }

        // Cho các trường xs:string nhiều dòng (MoTa, GhiChu): giữ xuống dòng, chỉ cắt đầu/cuối
        public static string? NhieuDong(string? chuoi)
        {
            var ketQua = BoKyTuCam(chuoi ?? string.Empty).Replace("\r\n", "\n").Trim();
            return ketQua.Length == 0 ? null : ketQua;
        }

        // Số điện thoại: bỏ dấu cách, dấu chấm, gạch ngang (0905 123 456 -> 0905123456)
        public static string SoDienThoai(string? chuoi)
        {
            return Regex.Replace(chuoi ?? string.Empty, @"[\s.\-]", string.Empty);
        }

        // Bỏ dấu tiếng Việt và đưa về chữ thường để tìm kiếm: "Nguyễn Nhật Ánh" -> "nguyen nhat anh"
        public static string BoDau(string? chuoi)
        {
            if (string.IsNullOrEmpty(chuoi))
            {
                return string.Empty;
            }

            var tachDau = chuoi.Normalize(NormalizationForm.FormD);
            var ketQua = new StringBuilder(tachDau.Length);
            foreach (var c in tachDau)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    ketQua.Append(c);
                }
            }

            return ketQua.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .ToLowerInvariant();
        }

        public static string CatNgan(string chuoi, int toiDa)
        {
            return chuoi.Length <= toiDa ? chuoi : chuoi.Substring(0, toiDa - 3) + "...";
        }
    }
}
