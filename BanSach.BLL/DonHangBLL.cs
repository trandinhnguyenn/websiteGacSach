using System.Globalization;
using System.Text.RegularExpressions;
using BanSach.DAL;
using BanSach.DTO;

namespace BanSach.BLL
{
    public class DonHangBLL
    {
        private static readonly Regex MauSoDienThoai = new Regex(@"^0[0-9]{9}$");
        private static readonly CultureInfo TiengViet = CultureInfo.GetCultureInfo("vi-VN");

        // Vòng đời một đơn. Admin chỉ được đi tới bước kế tiếp hoặc hủy; Hoàn thành và Đã hủy là trạng thái cuối.
        private static readonly Dictionary<string, string[]> BuocTiepTheo = new Dictionary<string, string[]>
        {
            [TrangThaiDonHang.ChoXacNhan] = new[] { TrangThaiDonHang.DaXacNhan, TrangThaiDonHang.DaHuy },
            [TrangThaiDonHang.DaXacNhan] = new[] { TrangThaiDonHang.DangGiao, TrangThaiDonHang.DaHuy },
            [TrangThaiDonHang.DangGiao] = new[] { TrangThaiDonHang.HoanThanh, TrangThaiDonHang.DaHuy },
        };

        private readonly DonHangDAL _donHangDal;
        private readonly SachDAL _sachDal;
        private readonly KhachHangDAL _khachHangDal;
        private readonly NhatKyBLL _nhatKy;

        public DonHangBLL(DonHangDAL donHangDal, SachDAL sachDal, KhachHangDAL khachHangDal, NhatKyBLL nhatKy)
        {
            _donHangDal = donHangDal;
            _sachDal = sachDal;
            _khachHangDal = khachHangDal;
            _nhatKy = nhatKy;
        }

        // Kho đã bị trừ từ lúc admin xác nhận đơn, nên đơn ở 2 trạng thái này mà hủy thì phải cộng sách lại vào kho
        private static bool DaTruKho(string trangThai)
        {
            return trangThai == TrangThaiDonHang.DaXacNhan || trangThai == TrangThaiDonHang.DangGiao;
        }

        private static string[] LayBuocTiep(string trangThai)
        {
            return BuocTiepTheo.TryGetValue(trangThai, out var buoc) ? buoc : Array.Empty<string>();
        }

        // =====================================================================
        // Phía khách hàng
        // =====================================================================

        // Khách đặt hàng từ giỏ. Kiểm tra tồn kho lúc đặt nhưng CHƯA trừ kho:
        // kho chỉ trừ khi admin xác nhận đơn. Giá lưu vào ChiTiet là giá tại thời điểm đặt.
        public DonHangXem DatHang(NguoiThucHien nguoi, List<GioHangDong> gioHang, DatHangYeuCau thongTin)
        {
            if (nguoi.VaiTro != VaiTro.KhachHang || string.IsNullOrEmpty(nguoi.MaKhachHang))
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongCoQuyen, "Chỉ tài khoản khách hàng mới đặt hàng được");
            }

            if (gioHang.Count == 0)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Giỏ hàng đang trống");
            }

            var nguoiNhan = ChuanHoa.MotDong(thongTin.NguoiNhan);
            var soDienThoai = ChuanHoa.SoDienThoai(thongTin.SoDienThoai);
            var diaChiGiao = ChuanHoa.MotDong(thongTin.DiaChiGiao);
            var ghiChu = ChuanHoa.NhieuDong(thongTin.GhiChu);

            var kiemTra = new BoKiemTra();
            kiemTra.BatBuoc(nguoiNhan, "Người nhận", 100);
            kiemTra.Them(!MauSoDienThoai.IsMatch(soDienThoai), "Số điện thoại người nhận phải gồm 10 chữ số, bắt đầu bằng 0");
            kiemTra.BatBuoc(diaChiGiao, "Địa chỉ giao hàng", 200);
            kiemTra.ToiDa(ghiChu, "Ghi chú", 500);
            kiemTra.NemNeuCoLoi();

            lock (XmlStore.Khoa)
            {
                var sachTheoMa = LaySachTheoMa();

                var thieuHang = new List<string>();
                foreach (var dong in gioHang)
                {
                    if (!sachTheoMa.TryGetValue(dong.MaSach, out var sach))
                    {
                        thieuHang.Add($"Sách {dong.MaSach} đã ngừng bán");
                    }
                    else if (sach.SoLuongTon < dong.SoLuong)
                    {
                        thieuHang.Add(sach.SoLuongTon == 0
                            ? $"\"{sach.TenSach}\" đã hết hàng"
                            : $"\"{sach.TenSach}\" chỉ còn {sach.SoLuongTon} cuốn");
                    }
                }

                if (thieuHang.Count > 0)
                {
                    throw new LoiNghiepVuException(LoaiLoi.XungDot, "Một số sách trong giỏ không đủ hàng", thieuHang);
                }

                var bayGio = DateTime.Now;
                var donHang = new DonHang
                {
                    MaDonHang = _donHangDal.TaoMaMoi(),
                    MaKhachHang = nguoi.MaKhachHang,
                    NgayDat = new DateTime(bayGio.Year, bayGio.Month, bayGio.Day, bayGio.Hour, bayGio.Minute, bayGio.Second),
                    TrangThai = TrangThaiDonHang.ChoXacNhan,
                    NguoiNhan = nguoiNhan,
                    SoDienThoai = soDienThoai,
                    DiaChiGiao = diaChiGiao,
                    GhiChu = ghiChu,
                    ChiTiet = gioHang.Select(dong => new ChiTietDonHang
                    {
                        MaSach = dong.MaSach,
                        SoLuong = dong.SoLuong,
                        DonGia = sachTheoMa[dong.MaSach].GiaBan
                    }).ToList()
                };

                _donHangDal.Them(donHang);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.Them, DoiTuongNhatKy.DonHang, donHang.MaDonHang,
                    $"Khách đặt đơn {donHang.MaDonHang}: {donHang.ChiTiet.Sum(ct => ct.SoLuong)} cuốn, {Tien(donHang.TongTien)}");
                return ChuyenSangXem(donHang, sachTheoMa);
            }
        }

        // Đơn của khách đang đăng nhập, mới nhất trước
        public List<DonHangXem> LayDonCuaToi(NguoiThucHien nguoi)
        {
            if (string.IsNullOrEmpty(nguoi.MaKhachHang))
            {
                return new List<DonHangXem>();
            }

            var sachTheoMa = LaySachTheoMa();
            return _donHangDal.LayTheoKhachHang(nguoi.MaKhachHang)
                .OrderByDescending(dh => dh.NgayDat)
                .Select(dh => ChuyenSangXem(dh, sachTheoMa))
                .ToList();
        }

        // Chống IDOR: khách xem đơn của người khác thì trả "không tìm thấy" (không để lộ đơn đó có tồn tại)
        public DonHangXem LayChiTiet(string ma, NguoiThucHien nguoi)
        {
            var donHang = LayDonDuocPhep(ma, nguoi);
            return ChuyenSangXem(donHang, LaySachTheoMa());
        }

        // Khách chỉ hủy được đơn của mình khi còn Chờ xác nhận. Đơn chưa trừ kho nên không cần hoàn kho.
        public DonHangXem HuyDonCuaToi(string ma, NguoiThucHien nguoi)
        {
            lock (XmlStore.Khoa)
            {
                var donHang = LayDonDuocPhep(ma, nguoi);
                if (donHang.MaKhachHang != nguoi.MaKhachHang)
                {
                    throw LoiNghiepVuException.KhongTimThay($"Không tìm thấy đơn hàng {ma}");
                }

                if (donHang.TrangThai != TrangThaiDonHang.ChoXacNhan)
                {
                    throw LoiNghiepVuException.XungDot(
                        $"Đơn {ma} đang ở trạng thái \"{TrangThaiDonHang.TenHienThi(donHang.TrangThai)}\" nên không tự hủy được. Liên hệ cửa hàng nếu cần hỗ trợ.");
                }

                donHang.TrangThai = TrangThaiDonHang.DaHuy;
                _donHangDal.CapNhat(donHang);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.HuyDon, DoiTuongNhatKy.DonHang, ma, $"Khách tự hủy đơn {ma} khi đơn còn Chờ xác nhận");
                return ChuyenSangXem(donHang, LaySachTheoMa());
            }
        }

        private DonHang LayDonDuocPhep(string ma, NguoiThucHien nguoi)
        {
            var donHang = _donHangDal.LayTheoMa(ma);
            var duocXem = donHang != null &&
                (nguoi.VaiTro == VaiTro.Admin || donHang.MaKhachHang == nguoi.MaKhachHang);

            if (!duocXem)
            {
                throw LoiNghiepVuException.KhongTimThay($"Không tìm thấy đơn hàng {ma}");
            }
            return donHang!;
        }

        // =====================================================================
        // Phía quản trị
        // =====================================================================

        // Danh sách đơn cho admin: lọc theo trạng thái + từ khóa (gõ không dấu cũng được), mới nhất trước.
        // Số đơn theo từng trạng thái tính sau khi lọc từ khóa, để các nút lọc khớp với kết quả đang tìm.
        public DanhSachDonHangQuanTri LayDanhSachQuanTri(BoLocDonHang boLoc)
        {
            var trangThai = string.IsNullOrWhiteSpace(boLoc.TrangThai) ? null : boLoc.TrangThai.Trim();
            if (trangThai != null && !TrangThaiDonHang.TatCa.Contains(trangThai))
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, $"Trạng thái \"{trangThai}\" không hợp lệ");
            }

            var sachTheoMa = LaySachTheoMa();
            var tenKhach = LayTenKhachHang();
            var tatCa = _donHangDal.LayTatCa();

            var truyVan = tatCa.AsEnumerable();
            var tuKhoa = ChuanHoa.BoDau(ChuanHoa.MotDong(boLoc.TuKhoa));
            if (tuKhoa.Length > 0)
            {
                truyVan = truyVan.Where(dh => ChuanHoa.BoDau(string.Join(" ",
                    dh.MaDonHang, dh.MaKhachHang, tenKhach.GetValueOrDefault(dh.MaKhachHang), dh.NguoiNhan, dh.SoDienThoai))
                    .Contains(tuKhoa));
            }
            var khopTuKhoa = truyVan.ToList();

            var demTheoTrangThai = TrangThaiDonHang.TatCa.Select(tt => new DemTrangThaiDonHang
            {
                TrangThai = tt,
                TenTrangThai = TrangThaiDonHang.TenHienThi(tt),
                SoDon = khopTuKhoa.Count(dh => dh.TrangThai == tt)
            }).ToList();

            var danhSach = khopTuKhoa
                .Where(dh => trangThai == null || dh.TrangThai == trangThai)
                .OrderByDescending(dh => dh.NgayDat)
                .ThenByDescending(dh => dh.MaDonHang)
                .Select(dh => ChuyenSangQuanTri(dh, sachTheoMa, tenKhach))
                .ToList();

            var trang = PhanTrang.Tao(danhSach, boLoc.Trang, boLoc.KichThuoc, 100);
            return new DanhSachDonHangQuanTri
            {
                DuLieu = trang.DuLieu,
                TongSo = trang.TongSo,
                Trang = trang.Trang,
                KichThuoc = trang.KichThuoc,
                TongTrang = trang.TongTrang,
                DemTheoTrangThai = demTheoTrangThai,
                DoanhThu = tatCa.Where(dh => dh.TrangThai == TrangThaiDonHang.HoanThanh).Sum(dh => dh.TongTien)
            };
        }

        public DonHangQuanTri LayChiTietQuanTri(string ma)
        {
            var donHang = _donHangDal.LayTheoMa(ma)
                ?? throw LoiNghiepVuException.KhongTimThay($"Không tìm thấy đơn hàng {ma}");
            return ChuyenSangQuanTri(donHang, LaySachTheoMa(), LayTenKhachHang());
        }

        // Admin chuyển trạng thái đơn:
        //  - Chờ xác nhận -> Đã xác nhận: kiểm tra đủ hàng rồi TRỪ KHO (đây là lúc sách thật sự rời kệ)
        //  - Đã xác nhận / Đang giao -> Đã hủy: HOÀN KHO số sách đã trừ
        //  - Chờ xác nhận -> Đã hủy: chưa trừ kho nên không đụng tới kho
        // Sách.xml và DonHang.xml ghi trong cùng một khóa; nếu ghi đơn lỗi thì trả kho về như cũ.
        public DonHangQuanTri DoiTrangThai(string ma, string? trangThaiMoi, NguoiThucHien nguoi)
        {
            if (nguoi.VaiTro != VaiTro.Admin)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongCoQuyen, "Chỉ admin mới đổi được trạng thái đơn");
            }

            trangThaiMoi = trangThaiMoi?.Trim() ?? string.Empty;
            if (!TrangThaiDonHang.TatCa.Contains(trangThaiMoi))
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Trạng thái mới không hợp lệ");
            }

            lock (XmlStore.Khoa)
            {
                var donHang = _donHangDal.LayTheoMa(ma)
                    ?? throw LoiNghiepVuException.KhongTimThay($"Không tìm thấy đơn hàng {ma}");
                var trangThaiCu = donHang.TrangThai;
                var tenCu = TrangThaiDonHang.TenHienThi(trangThaiCu);
                var tenMoi = TrangThaiDonHang.TenHienThi(trangThaiMoi);

                // Bấm 2 lần, hoặc 2 admin cùng xử lý một đơn: lần sau thấy trạng thái đã đổi nên bị chặn
                if (!LayBuocTiep(trangThaiCu).Contains(trangThaiMoi))
                {
                    throw LoiNghiepVuException.XungDot(trangThaiCu == trangThaiMoi
                        ? $"Đơn {ma} đã ở trạng thái \"{tenCu}\" rồi"
                        : $"Đơn {ma} đang \"{tenCu}\", không chuyển sang \"{tenMoi}\" được");
                }

                var sachTheoMa = LaySachTheoMa();
                Dictionary<string, int>? thayDoiKho = null;

                if (trangThaiMoi == TrangThaiDonHang.DaXacNhan)
                {
                    var thieuHang = TimSachThieu(donHang, sachTheoMa);
                    if (thieuHang.Count > 0)
                    {
                        throw new LoiNghiepVuException(LoaiLoi.XungDot, $"Kho không đủ hàng để xác nhận đơn {ma}", thieuHang);
                    }
                    thayDoiKho = donHang.ChiTiet.ToDictionary(ct => ct.MaSach, ct => -ct.SoLuong);
                }
                else if (trangThaiMoi == TrangThaiDonHang.DaHuy && DaTruKho(trangThaiCu))
                {
                    // Sách đã bị xóa khỏi danh mục thì không còn chỗ để cộng lại, bỏ qua dòng đó
                    thayDoiKho = donHang.ChiTiet
                        .Where(ct => sachTheoMa.ContainsKey(ct.MaSach))
                        .ToDictionary(ct => ct.MaSach, ct => ct.SoLuong);
                }

                var ketQuaKho = thayDoiKho is { Count: > 0 }
                    ? _sachDal.CongTonKho(thayDoiKho)
                    : new Dictionary<string, (int Cu, int Moi)>();

                donHang.TrangThai = trangThaiMoi;
                try
                {
                    _donHangDal.CapNhat(donHang);
                }
                catch
                {
                    if (ketQuaKho.Count > 0)
                    {
                        _sachDal.CongTonKho(thayDoiKho!.ToDictionary(kv => kv.Key, kv => -kv.Value));
                    }
                    throw;
                }

                var noiDung = $"Đơn {ma}: {tenCu} → {tenMoi}";
                if (ketQuaKho.Count > 0)
                {
                    var chiTietKho = string.Join(", ", ketQuaKho.Select(kv => $"{kv.Key} {kv.Value.Cu} → {kv.Value.Moi}"));
                    noiDung += trangThaiMoi == TrangThaiDonHang.DaXacNhan
                        ? $". Trừ kho: {chiTietKho}"
                        : $". Hoàn kho: {chiTietKho}";
                }
                _nhatKy.Ghi(nguoi,
                    trangThaiMoi == TrangThaiDonHang.DaHuy ? HanhDongNhatKy.HuyDon : HanhDongNhatKy.DoiTrangThai,
                    DoiTuongNhatKy.DonHang, ma, noiDung);

                return ChuyenSangQuanTri(donHang, LaySachTheoMa(), LayTenKhachHang());
            }
        }

        // =====================================================================
        // Dùng chung
        // =====================================================================

        private Dictionary<string, Sach> LaySachTheoMa()
        {
            return _sachDal.LayTatCa().ToDictionary(s => s.MaSach);
        }

        private Dictionary<string, string> LayTenKhachHang()
        {
            return _khachHangDal.LayTatCa().ToDictionary(kh => kh.MaKhachHang, kh => kh.HoTen);
        }

        // Các dòng trong đơn mà kho hiện tại không đáp ứng đủ (để admin biết trước khi bấm xác nhận)
        private static List<string> TimSachThieu(DonHang donHang, Dictionary<string, Sach> sachTheoMa)
        {
            var thieu = new List<string>();
            foreach (var ct in donHang.ChiTiet)
            {
                if (!sachTheoMa.TryGetValue(ct.MaSach, out var sach))
                {
                    thieu.Add($"Sách {ct.MaSach} đã bị xóa khỏi danh mục");
                }
                else if (sach.SoLuongTon < ct.SoLuong)
                {
                    thieu.Add($"\"{sach.TenSach}\": đơn cần {ct.SoLuong}, kho còn {sach.SoLuongTon}");
                }
            }
            return thieu;
        }

        private static string Tien(decimal so)
        {
            return so.ToString("#,0", TiengViet) + " ₫";
        }

        private static DonHangQuanTri ChuyenSangQuanTri(DonHang dh, Dictionary<string, Sach> sachTheoMa, Dictionary<string, string> tenKhach)
        {
            var xem = ChuyenSangXem(dh, sachTheoMa);
            return new DonHangQuanTri
            {
                MaDonHang = xem.MaDonHang,
                MaKhachHang = xem.MaKhachHang,
                NgayDat = xem.NgayDat,
                TrangThai = xem.TrangThai,
                TenTrangThai = xem.TenTrangThai,
                NguoiNhan = xem.NguoiNhan,
                SoDienThoai = xem.SoDienThoai,
                DiaChiGiao = xem.DiaChiGiao,
                GhiChu = xem.GhiChu,
                ChiTiet = xem.ChiTiet,
                TongSoLuong = xem.TongSoLuong,
                TongTien = xem.TongTien,
                CoTheHuy = xem.CoTheHuy,
                TenKhachHang = tenKhach.GetValueOrDefault(dh.MaKhachHang) ?? $"Khách {dh.MaKhachHang} (đã xóa)",
                BuocTiep = LayBuocTiep(dh.TrangThai).ToList(),
                DaTruKho = DaTruKho(dh.TrangThai),
                ThieuHang = dh.TrangThai == TrangThaiDonHang.ChoXacNhan ? TimSachThieu(dh, sachTheoMa) : new List<string>()
            };
        }

        private static DonHangXem ChuyenSangXem(DonHang dh, Dictionary<string, Sach> sachTheoMa)
        {
            return new DonHangXem
            {
                MaDonHang = dh.MaDonHang,
                MaKhachHang = dh.MaKhachHang,
                NgayDat = dh.NgayDat,
                TrangThai = dh.TrangThai,
                TenTrangThai = TrangThaiDonHang.TenHienThi(dh.TrangThai),
                NguoiNhan = dh.NguoiNhan,
                SoDienThoai = dh.SoDienThoai,
                DiaChiGiao = dh.DiaChiGiao,
                GhiChu = dh.GhiChu,
                ChiTiet = dh.ChiTiet.Select(ct =>
                {
                    sachTheoMa.TryGetValue(ct.MaSach, out var sach);
                    return new ChiTietDonHangXem
                    {
                        MaSach = ct.MaSach,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        TenSach = sach?.TenSach ?? $"Sách {ct.MaSach} (đã ngừng bán)",
                        TacGia = sach?.TacGia ?? string.Empty,
                        HinhAnh = sach?.HinhAnh
                    };
                }).ToList(),
                TongSoLuong = dh.ChiTiet.Sum(ct => ct.SoLuong),
                TongTien = dh.TongTien,
                CoTheHuy = dh.TrangThai == TrangThaiDonHang.ChoXacNhan
            };
        }
    }
}
