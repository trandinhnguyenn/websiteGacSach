using BanSach.DAL;
using BanSach.DTO;

namespace BanSach.BLL
{
    public class TheLoaiBLL
    {
        private readonly TheLoaiDAL _theLoaiDal;
        private readonly SachDAL _sachDal;
        private readonly NhatKyBLL _nhatKy;

        public TheLoaiBLL(TheLoaiDAL theLoaiDal, SachDAL sachDal, NhatKyBLL nhatKy)
        {
            _theLoaiDal = theLoaiDal;
            _sachDal = sachDal;
            _nhatKy = nhatKy;
        }

        // Danh sách kèm số sách của từng thể loại (đọc chéo Sach.xml)
        public List<TheLoaiTomTat> LayDanhSach()
        {
            var soSach = _sachDal.DemTheoTungTheLoai();
            return _theLoaiDal.LayTatCa()
                .OrderBy(tl => tl.MaTheLoai)
                .Select(tl => new TheLoaiTomTat
                {
                    MaTheLoai = tl.MaTheLoai,
                    TenTheLoai = tl.TenTheLoai,
                    MoTa = tl.MoTa,
                    SoLuongSach = soSach.GetValueOrDefault(tl.MaTheLoai)
                })
                .ToList();
        }

        public TheLoai LayTheoMa(string ma)
        {
            return _theLoaiDal.LayTheoMa(ma)
                ?? throw LoiNghiepVuException.KhongTimThay($"Không tìm thấy thể loại {ma}");
        }

        public TheLoai Them(TheLoai dauVao, NguoiThucHien nguoi)
        {
            var theLoai = ChuanHoaVaKiemTra(dauVao);

            lock (XmlStore.Khoa)
            {
                KiemTraTrungTen(theLoai.TenTheLoai, null);
                theLoai.MaTheLoai = _theLoaiDal.TaoMaMoi();
                _theLoaiDal.Them(theLoai);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.Them, DoiTuongNhatKy.TheLoai, theLoai.MaTheLoai,
                    $"Thêm thể loại \"{theLoai.TenTheLoai}\"");
            }

            return theLoai;
        }

        public TheLoai CapNhat(string ma, TheLoai dauVao, NguoiThucHien nguoi)
        {
            var theLoai = ChuanHoaVaKiemTra(dauVao);
            theLoai.MaTheLoai = ma;

            lock (XmlStore.Khoa)
            {
                var cu = LayTheoMa(ma);
                KiemTraTrungTen(theLoai.TenTheLoai, ma);
                _theLoaiDal.CapNhat(theLoai);

                var thayDoi = ThayDoi.MoTa(
                    ("TenTheLoai", cu.TenTheLoai, theLoai.TenTheLoai),
                    ("MoTa", cu.MoTa, theLoai.MoTa));
                if (thayDoi.Length > 0)
                {
                    _nhatKy.Ghi(nguoi, HanhDongNhatKy.Sua, DoiTuongNhatKy.TheLoai, ma, $"Sửa thể loại {ma}: {thayDoi}");
                }
            }

            return theLoai;
        }

        // Chặn xóa khi thể loại còn sách (kiểm tra chéo Sach.xml vì XSD không keyref sang file khác được)
        public void Xoa(string ma, NguoiThucHien nguoi)
        {
            lock (XmlStore.Khoa)
            {
                var theLoai = LayTheoMa(ma);
                var soSach = _sachDal.DemTheoTheLoai(ma);
                if (soSach > 0)
                {
                    throw LoiNghiepVuException.XungDot(
                        $"Thể loại \"{theLoai.TenTheLoai}\" đang có {soSach} sách, không xóa được. Hãy chuyển các sách này sang thể loại khác trước.");
                }

                _theLoaiDal.Xoa(ma);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.Xoa, DoiTuongNhatKy.TheLoai, ma, $"Xóa thể loại \"{theLoai.TenTheLoai}\"");
            }
        }

        private static TheLoai ChuanHoaVaKiemTra(TheLoai dauVao)
        {
            var theLoai = new TheLoai
            {
                TenTheLoai = ChuanHoa.MotDong(dauVao.TenTheLoai),
                MoTa = ChuanHoa.NhieuDong(dauVao.MoTa)
            };

            var kiemTra = new BoKiemTra();
            kiemTra.BatBuoc(theLoai.TenTheLoai, "Tên thể loại", 100);
            kiemTra.ToiDa(theLoai.MoTa, "Mô tả", 500);
            kiemTra.NemNeuCoLoi();
            return theLoai;
        }

        // XSD cũng có xs:unique cho TenTheLoai, nhưng kiểm tra trước ở đây để báo lỗi dễ hiểu và không phân biệt hoa thường
        private void KiemTraTrungTen(string ten, string? maBoQua)
        {
            var trung = _theLoaiDal.LayTatCa()
                .Any(tl => tl.MaTheLoai != maBoQua && string.Equals(tl.TenTheLoai, ten, StringComparison.OrdinalIgnoreCase));
            if (trung)
            {
                throw LoiNghiepVuException.XungDot($"Thể loại \"{ten}\" đã tồn tại");
            }
        }
    }
}
