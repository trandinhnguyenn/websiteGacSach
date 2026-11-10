using BanSach.DAL;
using BanSach.DTO;

namespace BanSach.BLL
{
    // Giỏ hàng: tầng Web giữ danh sách (mã sách, số lượng) trong session, BLL kiểm tra và tính tiền.
    // Giá luôn lấy mới từ Sach.xml, không tin giá do client gửi.
    public class GioHangBLL
    {
        public const int SoLuongToiDaMoiDong = 99;

        private readonly SachDAL _sachDal;

        public GioHangBLL(SachDAL sachDal)
        {
            _sachDal = sachDal;
        }

        // Ghép thông tin sách vào giỏ; sách đã bị xóa thì bỏ khỏi giỏ, tồn kho giảm thì giảm số lượng theo.
        // Hàm sửa trực tiếp danh sách 'dong' để tầng Web lưu lại session.
        public GioHang TinhLai(List<GioHangDong> dong)
        {
            var sachTheoMa = _sachDal.LayTatCa().ToDictionary(s => s.MaSach);
            var gioHang = new GioHang();

            foreach (var d in dong.ToList())
            {
                if (!sachTheoMa.TryGetValue(d.MaSach, out var sach))
                {
                    dong.Remove(d);
                    gioHang.ThongBao.Add($"Sách {d.MaSach} đã ngừng bán nên được bỏ khỏi giỏ.");
                    continue;
                }

                if (sach.SoLuongTon > 0 && d.SoLuong > sach.SoLuongTon)
                {
                    d.SoLuong = sach.SoLuongTon;
                    gioHang.ThongBao.Add($"\"{sach.TenSach}\" chỉ còn {sach.SoLuongTon} cuốn, số lượng trong giỏ đã giảm theo.");
                }

                gioHang.Dong.Add(new GioHangChiTiet
                {
                    MaSach = sach.MaSach,
                    TenSach = sach.TenSach,
                    TacGia = sach.TacGia,
                    HinhAnh = sach.HinhAnh,
                    DonGia = sach.GiaBan,
                    SoLuong = d.SoLuong,
                    SoLuongTon = sach.SoLuongTon,
                    HetHang = sach.SoLuongTon == 0
                });
            }

            gioHang.TongSoLuong = gioHang.Dong.Sum(x => x.SoLuong);
            gioHang.TongTien = gioHang.Dong.Where(x => !x.HetHang).Sum(x => x.ThanhTien);
            return gioHang;
        }

        public void Them(List<GioHangDong> dong, string? maSach, int soLuong)
        {
            var sach = LaySach(maSach);
            if (soLuong < 1 || soLuong > SoLuongToiDaMoiDong)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, $"Số lượng phải từ 1 đến {SoLuongToiDaMoiDong}");
            }

            var daCo = dong.FirstOrDefault(d => d.MaSach == sach.MaSach);
            var tong = (daCo?.SoLuong ?? 0) + soLuong;
            KiemTraTon(sach, tong, daCo?.SoLuong ?? 0);

            if (daCo == null)
            {
                dong.Add(new GioHangDong { MaSach = sach.MaSach, SoLuong = soLuong });
            }
            else
            {
                daCo.SoLuong = tong;
            }
        }

        // Đặt lại số lượng một dòng; 0 nghĩa là bỏ khỏi giỏ
        public void CapNhat(List<GioHangDong> dong, string? maSach, int soLuong)
        {
            var daCo = dong.FirstOrDefault(d => d.MaSach == maSach)
                ?? throw LoiNghiepVuException.KhongTimThay("Sách này không có trong giỏ");

            if (soLuong <= 0)
            {
                dong.Remove(daCo);
                return;
            }

            if (soLuong > SoLuongToiDaMoiDong)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, $"Số lượng tối đa {SoLuongToiDaMoiDong} cuốn mỗi sách");
            }

            KiemTraTon(LaySach(maSach), soLuong, 0);
            daCo.SoLuong = soLuong;
        }

        public void Xoa(List<GioHangDong> dong, string? maSach)
        {
            dong.RemoveAll(d => d.MaSach == maSach);
        }

        private Sach LaySach(string? maSach)
        {
            return _sachDal.LayTheoMa(maSach ?? string.Empty)
                ?? throw LoiNghiepVuException.KhongTimThay("Không tìm thấy sách này");
        }

        private static void KiemTraTon(Sach sach, int soLuongCan, int daCoTrongGio)
        {
            if (sach.SoLuongTon == 0)
            {
                throw LoiNghiepVuException.XungDot($"\"{sach.TenSach}\" đã hết hàng");
            }

            if (soLuongCan > sach.SoLuongTon)
            {
                var them = daCoTrongGio > 0 ? $" (giỏ đã có {daCoTrongGio})" : string.Empty;
                throw LoiNghiepVuException.XungDot($"\"{sach.TenSach}\" chỉ còn {sach.SoLuongTon} cuốn{them}");
            }
        }
    }
}
