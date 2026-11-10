using System.Globalization;

namespace BanSach.BLL
{
    // Gom tất cả lỗi nhập liệu của một form rồi báo một lần, thay vì báo từng lỗi một
    internal class BoKiemTra
    {
        private readonly List<string> _loi = new List<string>();

        public void BatBuoc(string giaTri, string tenTruong, int toiDa)
        {
            if (giaTri.Length == 0)
            {
                _loi.Add($"{tenTruong} không được để trống");
            }
            else if (giaTri.Length > toiDa)
            {
                _loi.Add($"{tenTruong} tối đa {toiDa} ký tự");
            }
        }

        public void ToiDa(string? giaTri, string tenTruong, int toiDa)
        {
            if (giaTri != null && giaTri.Length > toiDa)
            {
                _loi.Add($"{tenTruong} tối đa {toiDa} ký tự");
            }
        }

        public void Them(bool dieuKienLoi, string thongBao)
        {
            if (dieuKienLoi)
            {
                _loi.Add(thongBao);
            }
        }

        public void NemNeuCoLoi()
        {
            if (_loi.Count > 0)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Dữ liệu chưa hợp lệ", _loi);
            }
        }
    }

    // Mô tả những trường đã đổi để ghi nhật ký: "GiaBan: 125000 → 130000; SoLuongTon: 25 → 30"
    internal static class ThayDoi
    {
        public static string MoTa(params (string tenTruong, object? cu, object? moi)[] cacTruong)
        {
            var danhSach = cacTruong
                .Where(t => DoiSangChuoi(t.cu) != DoiSangChuoi(t.moi))
                .Select(t => $"{t.tenTruong}: {ChuanHoa.CatNgan(DoiSangChuoi(t.cu), 40)} → {ChuanHoa.CatNgan(DoiSangChuoi(t.moi), 40)}");
            return string.Join("; ", danhSach);
        }

        private static string DoiSangChuoi(object? giaTri)
        {
            if (giaTri is decimal so)
            {
                return so.ToString("0.##", CultureInfo.InvariantCulture);
            }

            var chuoi = giaTri?.ToString();
            return string.IsNullOrEmpty(chuoi) ? "(trống)" : chuoi;
        }
    }

    // Cắt danh sách theo trang
    internal static class PhanTrang
    {
        public static DTO.TrangKetQua<T> Tao<T>(List<T> danhSach, int trang, int kichThuoc, int kichThuocToiDa)
        {
            kichThuoc = Math.Clamp(kichThuoc, 1, kichThuocToiDa);
            var tongTrang = Math.Max(1, (int)Math.Ceiling(danhSach.Count / (double)kichThuoc));
            trang = Math.Clamp(trang, 1, tongTrang);

            return new DTO.TrangKetQua<T>
            {
                DuLieu = danhSach.Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToList(),
                TongSo = danhSach.Count,
                Trang = trang,
                KichThuoc = kichThuoc,
                TongTrang = tongTrang
            };
        }
    }
}
