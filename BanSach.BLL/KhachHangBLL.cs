using System.Text.RegularExpressions;
using BanSach.DAL;
using BanSach.DTO;

namespace BanSach.BLL
{
    public class KhachHangBLL
    {
        private static readonly Regex MauSoDienThoai = new Regex(@"^0[0-9]{9}$");
        private static readonly Regex MauEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private readonly KhachHangDAL _khachHangDal;
        private readonly TaiKhoanDAL _taiKhoanDal;
        private readonly DonHangDAL _donHangDal;
        private readonly NhatKyBLL _nhatKy;

        public KhachHangBLL(KhachHangDAL khachHangDal, TaiKhoanDAL taiKhoanDal, DonHangDAL donHangDal, NhatKyBLL nhatKy)
        {
            _khachHangDal = khachHangDal;
            _taiKhoanDal = taiKhoanDal;
            _donHangDal = donHangDal;
            _nhatKy = nhatKy;
        }

        // Danh sách kèm tên đăng nhập (TaiKhoan.xml) và số đơn (DonHang.xml)
        public List<KhachHangTomTat> LayDanhSach(string? tuKhoa)
        {
            var taiKhoanTheoKhach = _taiKhoanDal.LayTatCa()
                .Where(tk => tk.MaKhachHang != null)
                .ToDictionary(tk => tk.MaKhachHang!, tk => tk.TenDangNhap);
            var soDonTheoKhach = _donHangDal.DemTheoTungKhachHang();

            var truyVan = _khachHangDal.LayTatCa().AsEnumerable();

            var tim = ChuanHoa.BoDau(ChuanHoa.MotDong(tuKhoa));
            if (tim.Length > 0)
            {
                truyVan = truyVan.Where(kh =>
                    ChuanHoa.BoDau(kh.HoTen).Contains(tim) ||
                    kh.SoDienThoai.Contains(tim) ||
                    (kh.Email ?? string.Empty).ToLowerInvariant().Contains(tim) ||
                    kh.MaKhachHang.ToLowerInvariant() == tim);
            }

            return truyVan
                .OrderBy(kh => kh.MaKhachHang)
                .Select(kh => new KhachHangTomTat
                {
                    MaKhachHang = kh.MaKhachHang,
                    HoTen = kh.HoTen,
                    SoDienThoai = kh.SoDienThoai,
                    Email = kh.Email,
                    DiaChi = kh.DiaChi,
                    TenDangNhap = taiKhoanTheoKhach.GetValueOrDefault(kh.MaKhachHang),
                    SoDonHang = soDonTheoKhach.GetValueOrDefault(kh.MaKhachHang)
                })
                .ToList();
        }

        public KhachHang LayTheoMa(string ma)
        {
            return _khachHangDal.LayTheoMa(ma)
                ?? throw LoiNghiepVuException.KhongTimThay($"Không tìm thấy khách hàng {ma}");
        }

        // Admin thêm khách chỉ tạo hồ sơ, không tạo tài khoản đăng nhập
        public KhachHang Them(KhachHang dauVao, NguoiThucHien nguoi)
        {
            var khachHang = ChuanHoaVaKiemTra(dauVao);

            lock (XmlStore.Khoa)
            {
                KiemTraTrung(khachHang, null);
                khachHang.MaKhachHang = _khachHangDal.TaoMaMoi();
                _khachHangDal.Them(khachHang);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.Them, DoiTuongNhatKy.KhachHang, khachHang.MaKhachHang,
                    $"Thêm khách hàng \"{khachHang.HoTen}\" - {khachHang.SoDienThoai}");
            }

            return khachHang;
        }

        public KhachHang CapNhat(string ma, KhachHang dauVao, NguoiThucHien nguoi)
        {
            var khachHang = ChuanHoaVaKiemTra(dauVao);
            khachHang.MaKhachHang = ma;

            lock (XmlStore.Khoa)
            {
                var cu = LayTheoMa(ma);
                KiemTraTrung(khachHang, ma);
                _khachHangDal.CapNhat(khachHang);

                var thayDoi = ThayDoi.MoTa(
                    ("HoTen", cu.HoTen, khachHang.HoTen),
                    ("SoDienThoai", cu.SoDienThoai, khachHang.SoDienThoai),
                    ("Email", cu.Email, khachHang.Email),
                    ("DiaChi", cu.DiaChi, khachHang.DiaChi));
                if (thayDoi.Length > 0)
                {
                    _nhatKy.Ghi(nguoi, HanhDongNhatKy.Sua, DoiTuongNhatKy.KhachHang, ma, $"Sửa khách hàng {ma}: {thayDoi}");
                }
            }

            return khachHang;
        }

        // Hồ sơ của chính khách đang đăng nhập: mã khách lấy từ cookie, không nhận từ client
        public KhachHang LayHoSo(NguoiThucHien nguoi)
        {
            if (string.IsNullOrEmpty(nguoi.MaKhachHang))
            {
                throw LoiNghiepVuException.KhongTimThay("Tài khoản này không có hồ sơ khách hàng");
            }
            return LayTheoMa(nguoi.MaKhachHang);
        }

        // Khách tự sửa hồ sơ của mình (không ghi nhật ký admin)
        public KhachHang CapNhatHoSo(NguoiThucHien nguoi, KhachHang dauVao)
        {
            var khachHang = ChuanHoaVaKiemTra(dauVao);

            lock (XmlStore.Khoa)
            {
                var cu = LayHoSo(nguoi);
                khachHang.MaKhachHang = cu.MaKhachHang;
                KiemTraTrung(khachHang, cu.MaKhachHang);
                _khachHangDal.CapNhat(khachHang);
            }

            return khachHang;
        }

        // Chặn xóa khách đã có đơn (giữ lịch sử mua) hoặc đã có tài khoản đăng nhập (tránh tài khoản mồ côi)
        public void Xoa(string ma, NguoiThucHien nguoi)
        {
            lock (XmlStore.Khoa)
            {
                var khachHang = LayTheoMa(ma);

                var soDon = _donHangDal.DemTheoKhachHang(ma);
                if (soDon > 0)
                {
                    throw LoiNghiepVuException.XungDot(
                        $"Khách hàng \"{khachHang.HoTen}\" đã có {soDon} đơn hàng, không xóa được.");
                }

                var taiKhoan = _taiKhoanDal.LayTheoMaKhachHang(ma);
                if (taiKhoan != null)
                {
                    throw LoiNghiepVuException.XungDot(
                        $"Khách hàng \"{khachHang.HoTen}\" đang có tài khoản đăng nhập \"{taiKhoan.TenDangNhap}\", không xóa được.");
                }

                _khachHangDal.Xoa(ma);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.Xoa, DoiTuongNhatKy.KhachHang, ma, $"Xóa khách hàng \"{khachHang.HoTen}\"");
            }
        }

        internal static KhachHang ChuanHoaVaKiemTra(KhachHang dauVao)
        {
            var kiemTra = new BoKiemTra();
            var khachHang = ChuanHoaHoSo(dauVao, kiemTra);
            kiemTra.NemNeuCoLoi();
            return khachHang;
        }

        // Dùng chung với đăng ký (TaiKhoanBLL): gom lỗi hồ sơ và lỗi tài khoản để báo một lần
        internal static KhachHang ChuanHoaHoSo(KhachHang dauVao, BoKiemTra kiemTra)
        {
            var khachHang = new KhachHang
            {
                HoTen = ChuanHoa.MotDong(dauVao.HoTen),
                SoDienThoai = ChuanHoa.SoDienThoai(dauVao.SoDienThoai),
                Email = ChuanHoa.MotDongTuyChon(dauVao.Email),
                DiaChi = ChuanHoa.MotDong(dauVao.DiaChi)
            };

            kiemTra.BatBuoc(khachHang.HoTen, "Họ tên", 100);
            kiemTra.Them(!MauSoDienThoai.IsMatch(khachHang.SoDienThoai), "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0");
            kiemTra.Them(khachHang.Email != null && !MauEmail.IsMatch(khachHang.Email), "Email không đúng định dạng");
            kiemTra.ToiDa(khachHang.Email, "Email", 100);
            kiemTra.BatBuoc(khachHang.DiaChi, "Địa chỉ", 200);
            return khachHang;
        }

        internal void KiemTraTrung(KhachHang khachHang, string? maBoQua)
        {
            var danhSach = _khachHangDal.LayTatCa().Where(kh => kh.MaKhachHang != maBoQua).ToList();

            var trungSdt = danhSach.FirstOrDefault(kh => kh.SoDienThoai == khachHang.SoDienThoai);
            if (trungSdt != null)
            {
                throw LoiNghiepVuException.XungDot(
                    $"Số điện thoại {khachHang.SoDienThoai} đã thuộc khách hàng {trungSdt.MaKhachHang} ({trungSdt.HoTen})");
            }

            if (khachHang.Email != null)
            {
                var trungEmail = danhSach.FirstOrDefault(kh => string.Equals(kh.Email, khachHang.Email, StringComparison.OrdinalIgnoreCase));
                if (trungEmail != null)
                {
                    throw LoiNghiepVuException.XungDot(
                        $"Email {khachHang.Email} đã thuộc khách hàng {trungEmail.MaKhachHang} ({trungEmail.HoTen})");
                }
            }
        }
    }
}
