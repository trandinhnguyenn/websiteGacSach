namespace BanSach.DTO
{
    // Kết quả phân trang dùng chung cho mọi danh sách
    public class TrangKetQua<T>
    {
        public List<T> DuLieu { get; set; } = new List<T>();
        public int TongSo { get; set; }
        public int Trang { get; set; }
        public int KichThuoc { get; set; }
        public int TongTrang { get; set; }
    }

    // Tham số lọc / sắp xếp / phân trang danh sách sách (nhận từ query string của API)
    public class BoLocSach
    {
        public string? TuKhoa { get; set; }         // tìm trong tên sách, tác giả
        public string? MaTheLoai { get; set; }
        public decimal? GiaTu { get; set; }
        public decimal? GiaDen { get; set; }
        public string? SapXep { get; set; }         // ten | gia-tang | gia-giam | moi | ton-it | ma
        public int Trang { get; set; } = 1;
        public int KichThuoc { get; set; } = 10;
    }

    // Tham số lọc nhật ký
    public class BoLocNhatKy
    {
        public string? DoiTuong { get; set; }
        public string? HanhDong { get; set; }
        public int Trang { get; set; } = 1;
        public int KichThuoc { get; set; } = 20;
    }

    // Tham số lọc đơn hàng ở trang quản trị
    public class BoLocDonHang
    {
        public string? TrangThai { get; set; }      // rỗng = mọi trạng thái
        public string? TuKhoa { get; set; }         // mã đơn, mã / tên khách, người nhận, số điện thoại
        public int Trang { get; set; } = 1;
        public int KichThuoc { get; set; } = 15;
    }
}
