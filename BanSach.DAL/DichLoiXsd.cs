using System.Text.RegularExpressions;

namespace BanSach.DAL
{
    // Thông báo kiểm tra XSD của .NET chỉ có tiếng Anh. Dịch các mẫu câu hay gặp sang tiếng Việt cho người dùng đọc được;
    // câu nào không khớp mẫu thì giữ nguyên bản gốc (thà tiếng Anh còn hơn dịch sai).
    public static class DichLoiXsd
    {
        private const RegexOptions TuyChon = RegexOptions.CultureInvariant;

        // The 'GiaBan' element is invalid - The value 'abc' is invalid according to its datatype 'GiaType' - <lý do>
        private static readonly Regex SaiGiaTri = new Regex(
            @"^The '(?<ten>[^']+)' (?<loai>element|attribute) is invalid - The value '(?<giaTri>.*?)' is invalid according to its datatype '(?<kieu>[^']+)' - (?<lyDo>.+)$",
            TuyChon | RegexOptions.Singleline);

        private static readonly Regex RangBuocHong = new Regex(@"^The (?<rb>\w+) constraint failed\.?$", TuyChon);
        private static readonly Regex KhongPhaiKieu = new Regex(@"^The string '.*' is not a valid (?<kieu>\w+) value\.?$", TuyChon | RegexOptions.Singleline);
        private static readonly Regex DoDai = new Regex(@"^The actual length is (?<huong>less|greater) than the (?<rb>\w+) value\.?$", TuyChon);

        private static readonly Regex ConKhongHopLe = new Regex(
            @"^The element '(?<cha>[^']+)' has invalid child element '(?<con>[^']+)'\.(?: List of possible elements expected: (?<can>.+?)\.)?$", TuyChon);
        private static readonly Regex ThieuCon = new Regex(
            @"^The element '(?<cha>[^']+)' has incomplete content\.(?: List of possible elements expected: (?<can>.+?)\.)?$", TuyChon);
        private static readonly Regex ThieuThuocTinh = new Regex(@"^The required attribute '(?<ten>[^']+)' is missing\.?$", TuyChon);
        private static readonly Regex KhongKhaiBao = new Regex(@"^The '(?<ten>[^']+)' (?<loai>element|attribute) is not declared\.?$", TuyChon);
        private static readonly Regex KhongDuocCoChu = new Regex(@"^The element '(?<ten>[^']+)' cannot contain text\..*$", TuyChon | RegexOptions.Singleline);
        private static readonly Regex TrungKhoa = new Regex(
            @"^There is a duplicate key sequence '(?<giaTri>.*?)' for the '(?<rb>[^']+)' key or unique identity constraint\.?$", TuyChon);
        private static readonly Regex KhoaRong = new Regex(
            @"^The identity constraint '(?<rb>[^']+)' validation has failed\. Either a key is missing or the existing key has an empty node\.?$", TuyChon);
        private static readonly Regex KhoaNgoai = new Regex(
            @"^The key sequence '(?<giaTri>.*?)' in '(?<rb>[^']+)' Keyref fails to refer to some key\.?$", TuyChon);
        private static readonly Regex TenTrongNhay = new Regex(@"'([^']+)'", TuyChon);

        public static string Dich(string loi)
        {
            Match m;
            if ((m = SaiGiaTri.Match(loi)).Success)
            {
                var chuThe = m.Groups["loai"].Value == "element" ? $"Thẻ <{m.Groups["ten"].Value}>" : $"Thuộc tính {m.Groups["ten"].Value}";
                return $"{chuThe}: giá trị \"{m.Groups["giaTri"].Value}\" {DichLyDo(m.Groups["lyDo"].Value, m.Groups["kieu"].Value)}";
            }
            if ((m = ConKhongHopLe.Match(loi)).Success)
            {
                var can = m.Groups["can"].Success ? $" Ở vị trí này chỉ được có: {DanhSachThe(m.Groups["can"].Value)}." : string.Empty;
                return $"Thẻ <{m.Groups["cha"].Value}> chứa thẻ con <{m.Groups["con"].Value}> sai chỗ hoặc không được phép.{can}";
            }
            if ((m = ThieuCon.Match(loi)).Success)
            {
                var can = m.Groups["can"].Success ? $" (cần {DanhSachThe(m.Groups["can"].Value)})" : string.Empty;
                return $"Thẻ <{m.Groups["cha"].Value}> còn thiếu thẻ con bắt buộc{can}.";
            }
            if ((m = ThieuThuocTinh.Match(loi)).Success)
            {
                return $"Thiếu thuộc tính bắt buộc {m.Groups["ten"].Value}.";
            }
            if ((m = KhongKhaiBao.Match(loi)).Success)
            {
                return m.Groups["loai"].Value == "element"
                    ? $"Thẻ <{m.Groups["ten"].Value}> không có trong cấu trúc cho phép."
                    : $"Thuộc tính {m.Groups["ten"].Value} không có trong cấu trúc cho phép.";
            }
            if ((m = KhongDuocCoChu.Match(loi)).Success)
            {
                return $"Thẻ <{m.Groups["ten"].Value}> không được chứa chữ, chỉ chứa thẻ con.";
            }
            if ((m = TrungKhoa.Match(loi)).Success)
            {
                return $"Giá trị \"{m.Groups["giaTri"].Value}\" bị trùng, phải là duy nhất (ràng buộc {m.Groups["rb"].Value}).";
            }
            if ((m = KhoaRong.Match(loi)).Success)
            {
                return $"Thiếu hoặc để trống giá trị khóa (ràng buộc {m.Groups["rb"].Value}).";
            }
            if ((m = KhoaNgoai.Match(loi)).Success)
            {
                return $"Giá trị \"{m.Groups["giaTri"].Value}\" không trỏ tới bản ghi nào có thật (ràng buộc {m.Groups["rb"].Value}).";
            }
            return loi;
        }

        // Lỗi "thiếu khóa" .NET báo kèm sau lỗi giá trị của chính thuộc tính khóa đó (cùng dòng): bỏ cho đỡ rối
        public static bool LaLoiKhoaRong(string loiGoc)
        {
            return KhoaRong.IsMatch(loiGoc);
        }

        private static string DichLyDo(string lyDo, string kieu)
        {
            Match m;
            if ((m = RangBuocHong.Match(lyDo)).Success)
            {
                return m.Groups["rb"].Value switch
                {
                    "Pattern" => $"không đúng mẫu quy định ({kieu})",
                    "Enumeration" => $"không nằm trong danh sách giá trị cho phép ({kieu})",
                    "MinInclusive" or "MinExclusive" => $"nhỏ hơn giá trị tối thiểu cho phép ({kieu})",
                    "MaxInclusive" or "MaxExclusive" => $"lớn hơn giá trị tối đa cho phép ({kieu})",
                    "TotalDigits" or "FractionDigits" => $"có quá nhiều chữ số ({kieu})",
                    _ => $"vi phạm ràng buộc {m.Groups["rb"].Value} ({kieu})"
                };
            }
            if ((m = DoDai.Match(lyDo)).Success)
            {
                return m.Groups["huong"].Value == "less"
                    ? $"ngắn hơn độ dài tối thiểu ({kieu})"
                    : $"dài hơn độ dài tối đa ({kieu})";
            }
            if ((m = KhongPhaiKieu.Match(lyDo)).Success)
            {
                return $"không phải {TenKieu(m.Groups["kieu"].Value)}";
            }
            return $"không hợp lệ theo kiểu {kieu}";
        }

        private static string TenKieu(string kieuDotNet)
        {
            return kieuDotNet switch
            {
                "Int16" or "Int32" or "Int64" or "Byte" or "SByte" or "UInt16" or "UInt32" or "UInt64" or "Integer" => "số nguyên",
                "Decimal" or "Double" or "Single" => "số",
                "DateTime" => "ngày giờ hợp lệ (yyyy-MM-ddTHH:mm:ss)",
                "Date" => "ngày hợp lệ (yyyy-MM-dd)",
                "Boolean" => "true hoặc false",
                _ => $"kiểu {kieuDotNet}"
            };
        }

        // .NET viết "'HinhAnh, MoTa'" (nhiều tên trong một cặp nháy) hoặc "'A' as well as 'B'" -> "<HinhAnh> hoặc <MoTa>"
        private static string DanhSachThe(string chuoi)
        {
            var ten = TenTrongNhay.Matches(chuoi)
                .SelectMany(x => x.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(x => $"<{x}>")
                .ToList();
            return ten.Count > 0 ? string.Join(" hoặc ", ten) : chuoi;
        }
    }
}
