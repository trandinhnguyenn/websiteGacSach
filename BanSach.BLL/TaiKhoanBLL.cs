using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BanSach.DAL;
using BanSach.DTO;

namespace BanSach.BLL
{
    public class TaiKhoanBLL
    {
        // Hash giả dùng khi tên đăng nhập không tồn tại, để thời gian xử lý giống hệt trường hợp có tài khoản
        private static readonly (string Hash, string Salt) HashGia = MatKhauHelper.TaoHash("khong-dung");

        // Khớp TenDangNhapType trong TaiKhoan.xsd
        private static readonly Regex MauTenDangNhap = new Regex(@"^[A-Za-z0-9_.]{3,30}$");

        private readonly TaiKhoanDAL _taiKhoanDal;
        private readonly KhachHangDAL _khachHangDal;
        private readonly NhatKyBLL _nhatKyBll;

        public TaiKhoanBLL(TaiKhoanDAL taiKhoanDal, KhachHangDAL khachHangDal, NhatKyBLL nhatKyBll)
        {
            _taiKhoanDal = taiKhoanDal;
            _khachHangDal = khachHangDal;
            _nhatKyBll = nhatKyBll;
        }

        // Tem bảo mật gắn vào cookie đăng nhập, rút ra từ salt + hash mật khẩu đang lưu trong TaiKhoan.xml.
        // Mỗi lần đổi mật khẩu sinh salt mới nên tem đổi theo: cookie cũ (máy khác, trình duyệt khác) mang tem cũ
        // bị từ chối ở lần gọi kế tiếp. Không cần thêm trường nào vào TaiKhoan.xml / TaiKhoan.xsd.
        public static string TaoTemBaoMat(TaiKhoan taiKhoan)
        {
            var bam = SHA256.HashData(Encoding.UTF8.GetBytes(taiKhoan.Salt + ":" + taiKhoan.MatKhauHash));
            return Convert.ToBase64String(bam, 0, 12);
        }

        // Cookie còn dùng được không: tài khoản còn, tem khớp (chưa đổi mật khẩu), vai trò và hồ sơ khách không đổi
        public bool PhienConHieuLuc(string? maTaiKhoan, string? tem, string? vaiTro, string? maKhachHang)
        {
            if (string.IsNullOrEmpty(maTaiKhoan) || string.IsNullOrEmpty(tem))
            {
                return false;
            }
            var taiKhoan = _taiKhoanDal.LayTheoMa(maTaiKhoan);
            return taiKhoan != null
                && tem == TaoTemBaoMat(taiKhoan)
                && taiKhoan.VaiTro == vaiTro
                && (taiKhoan.MaKhachHang ?? string.Empty) == (maKhachHang ?? string.Empty);
        }

        // Trả về tài khoản nếu đúng tên + mật khẩu, sai thì null (không nói rõ sai tên hay sai mật khẩu)
        public TaiKhoan? DangNhap(string? tenDangNhap, string? matKhau)
        {
            var ten = (tenDangNhap ?? string.Empty).Trim();
            if (ten.Length == 0 || ten.Length > 30 || string.IsNullOrEmpty(matKhau) || matKhau.Length > 100)
            {
                return null;
            }

            var taiKhoan = _taiKhoanDal.LayTheoTenDangNhap(ten);
            if (taiKhoan == null)
            {
                // Vẫn băm một lần để kẻ dò không phân biệt được "sai tên" và "sai mật khẩu" qua thời gian phản hồi
                MatKhauHelper.KiemTra(matKhau, HashGia.Hash, HashGia.Salt);
                return null;
            }

            return MatKhauHelper.KiemTra(matKhau, taiKhoan.MatKhauHash, taiKhoan.Salt) ? taiKhoan : null;
        }

        // Khách tự đăng ký. Nếu số điện thoại trùng một hồ sơ admin đã tạo (chưa có tài khoản) và họ tên khớp
        // thì gắn tài khoản vào hồ sơ đó; còn lại tạo hồ sơ mới. Ghi 2 file: KhachHang.xml (nếu cần) và TaiKhoan.xml.
        public (TaiKhoan TaiKhoan, bool GanHoSoCu) DangKy(DangKyYeuCau yeuCau)
        {
            var tenDangNhap = (yeuCau.TenDangNhap ?? string.Empty).Trim();

            var kiemTra = new BoKiemTra();
            var hoSo = KhachHangBLL.ChuanHoaHoSo(new KhachHang
            {
                HoTen = yeuCau.HoTen ?? string.Empty,
                SoDienThoai = yeuCau.SoDienThoai ?? string.Empty,
                Email = yeuCau.Email,
                DiaChi = yeuCau.DiaChi ?? string.Empty
            }, kiemTra);
            kiemTra.Them(!MauTenDangNhap.IsMatch(tenDangNhap),
                "Tên đăng nhập 3-30 ký tự, chỉ gồm chữ không dấu, số, dấu chấm hoặc gạch dưới");
            KiemTraMatKhauMoi(kiemTra, yeuCau.MatKhau, tenDangNhap);
            kiemTra.NemNeuCoLoi();

            lock (XmlStore.Khoa)
            {
                var trungTen = _taiKhoanDal.LayTatCa()
                    .Any(tk => string.Equals(tk.TenDangNhap, tenDangNhap, StringComparison.OrdinalIgnoreCase));
                if (trungTen)
                {
                    throw LoiNghiepVuException.XungDot($"Tên đăng nhập \"{tenDangNhap}\" đã có người dùng");
                }

                var danhSachKhach = _khachHangDal.LayTatCa();
                var hoSoCoSan = danhSachKhach.FirstOrDefault(kh => kh.SoDienThoai == hoSo.SoDienThoai);
                string maKhachHang;

                if (hoSoCoSan != null)
                {
                    if (_taiKhoanDal.LayTheoMaKhachHang(hoSoCoSan.MaKhachHang) != null)
                    {
                        throw LoiNghiepVuException.XungDot("Số điện thoại này đã đăng ký tài khoản. Hãy đăng nhập, hoặc dùng số khác.");
                    }

                    // Chỉ biết số điện thoại thì chưa đủ để nhận hồ sơ của người khác: họ tên cũng phải khớp
                    if (ChuanHoa.BoDau(hoSoCoSan.HoTen) != ChuanHoa.BoDau(hoSo.HoTen))
                    {
                        throw LoiNghiepVuException.XungDot(
                            "Số điện thoại này đã có hồ sơ ở cửa hàng với họ tên khác. Kiểm tra lại họ tên hoặc liên hệ cửa hàng.");
                    }

                    maKhachHang = hoSoCoSan.MaKhachHang;
                }
                else
                {
                    var trungEmail = hoSo.Email != null && danhSachKhach
                        .Any(kh => string.Equals(kh.Email, hoSo.Email, StringComparison.OrdinalIgnoreCase));
                    if (trungEmail)
                    {
                        throw LoiNghiepVuException.XungDot($"Email {hoSo.Email} đã được dùng cho một khách hàng khác");
                    }

                    hoSo.MaKhachHang = _khachHangDal.TaoMaMoi();
                    _khachHangDal.Them(hoSo);
                    maKhachHang = hoSo.MaKhachHang;
                }

                var (hash, salt) = MatKhauHelper.TaoHash(yeuCau.MatKhau!);
                var taiKhoan = new TaiKhoan
                {
                    MaTaiKhoan = _taiKhoanDal.TaoMaMoi(),
                    VaiTro = VaiTro.KhachHang,
                    MaKhachHang = maKhachHang,
                    TenDangNhap = tenDangNhap,
                    MatKhauHash = hash,
                    Salt = salt
                };
                _taiKhoanDal.Them(taiKhoan);

                // Người thực hiện là chính tài khoản vừa tạo (khách tự đăng ký, chưa có ai đăng nhập)
                var nguoiTao = new NguoiThucHien
                {
                    MaTaiKhoan = taiKhoan.MaTaiKhoan,
                    TenDangNhap = taiKhoan.TenDangNhap,
                    VaiTro = taiKhoan.VaiTro,
                    MaKhachHang = taiKhoan.MaKhachHang
                };
                _nhatKyBll.Ghi(nguoiTao, HanhDongNhatKy.DangKy, DoiTuongNhatKy.TaiKhoan, taiKhoan.MaTaiKhoan,
                    hoSoCoSan != null
                        ? $"Khách tự đăng ký tài khoản {tenDangNhap}, gắn vào hồ sơ có sẵn {maKhachHang} (khớp số điện thoại và họ tên)"
                        : $"Khách tự đăng ký tài khoản {tenDangNhap}, tạo hồ sơ mới {maKhachHang}");

                return (taiKhoan, hoSoCoSan != null);
            }
        }

        // Đổi xong trả về tài khoản mới để tầng Web cấp lại cookie (tem mới) cho phiên đang dùng;
        // các phiên khác của tài khoản này tự hết hiệu lực vì tem cũ không còn khớp
        public TaiKhoan DoiMatKhau(NguoiThucHien nguoi, DoiMatKhauYeuCau yeuCau)
        {
            lock (XmlStore.Khoa)
            {
                var taiKhoan = _taiKhoanDal.LayTheoMa(nguoi.MaTaiKhoan)
                    ?? throw LoiNghiepVuException.KhongTimThay("Không tìm thấy tài khoản");

                if (string.IsNullOrEmpty(yeuCau.MatKhauCu) || yeuCau.MatKhauCu.Length > 100 ||
                    !MatKhauHelper.KiemTra(yeuCau.MatKhauCu, taiKhoan.MatKhauHash, taiKhoan.Salt))
                {
                    throw new SaiMatKhauHienTaiException();
                }

                var kiemTra = new BoKiemTra();
                KiemTraMatKhauMoi(kiemTra, yeuCau.MatKhauMoi, taiKhoan.TenDangNhap);
                kiemTra.Them(yeuCau.MatKhauMoi == yeuCau.MatKhauCu, "Mật khẩu mới phải khác mật khẩu hiện tại");
                kiemTra.NemNeuCoLoi();

                var (hash, salt) = MatKhauHelper.TaoHash(yeuCau.MatKhauMoi!);
                taiKhoan.MatKhauHash = hash;
                taiKhoan.Salt = salt;
                _taiKhoanDal.CapNhat(taiKhoan);

                _nhatKyBll.Ghi(nguoi, HanhDongNhatKy.DoiMatKhau, DoiTuongNhatKy.TaiKhoan, taiKhoan.MaTaiKhoan,
                    "Đổi mật khẩu. Các phiên đăng nhập khác của tài khoản này bị đăng xuất");
                return taiKhoan;
            }
        }

        private static void KiemTraMatKhauMoi(BoKiemTra kiemTra, string? matKhau, string tenDangNhap)
        {
            foreach (var loi in ChinhSachMatKhau.KiemTra(matKhau, tenDangNhap))
            {
                kiemTra.Them(true, loi);
            }
        }
    }
}
