using BanSach.DAL;
using BanSach.DTO;

namespace BanSach.BLL
{
    // Ghi và tra cứu nhật ký (NhatKy.xml): thao tác của admin, cộng thêm việc khách đặt / tự hủy đơn,
    // khách tự đăng ký và mọi lần đổi mật khẩu
    public class NhatKyBLL
    {
        private readonly NhatKyDAL _nhatKyDal;

        public NhatKyBLL(NhatKyDAL nhatKyDal)
        {
            _nhatKyDal = nhatKyDal;
        }

        public void Ghi(NguoiThucHien nguoi, string hanhDong, string doiTuong, string? maDoiTuong, string noiDung)
        {
            lock (XmlStore.Khoa)
            {
                var nhatKy = new NhatKy
                {
                    MaNhatKy = _nhatKyDal.TaoMaMoi(),
                    MaTaiKhoan = nguoi.MaTaiKhoan,
                    MaDoiTuong = maDoiTuong,
                    ThoiGian = DateTime.Now,
                    TenDangNhap = nguoi.TenDangNhap,
                    HanhDong = hanhDong,
                    DoiTuong = doiTuong,
                    NoiDung = ChuanHoa.CatNgan(ChuanHoa.NhieuDong(noiDung) ?? hanhDong, 500)
                };
                _nhatKyDal.Them(nhatKy);
            }
        }

        // Mới nhất lên đầu, lọc theo đối tượng / hành động
        public TrangKetQua<NhatKy> LayDanhSach(BoLocNhatKy boLoc)
        {
            var truyVan = _nhatKyDal.LayTatCa().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(boLoc.DoiTuong))
            {
                truyVan = truyVan.Where(nk => nk.DoiTuong == boLoc.DoiTuong);
            }

            if (!string.IsNullOrWhiteSpace(boLoc.HanhDong))
            {
                truyVan = truyVan.Where(nk => nk.HanhDong == boLoc.HanhDong);
            }

            var danhSach = truyVan
                .OrderByDescending(nk => nk.ThoiGian)
                .ThenByDescending(nk => nk.MaNhatKy)
                .ToList();

            return PhanTrang.Tao(danhSach, boLoc.Trang, boLoc.KichThuoc, 100);
        }
    }
}
