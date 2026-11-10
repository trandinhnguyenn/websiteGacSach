using BanSach.DAL;
using BanSach.DTO;

namespace BanSach.BLL
{
    public class SachBLL
    {
        private readonly SachDAL _sachDal;
        private readonly TheLoaiDAL _theLoaiDal;
        private readonly DonHangDAL _donHangDal;
        private readonly NhatKyBLL _nhatKy;

        public SachBLL(SachDAL sachDal, TheLoaiDAL theLoaiDal, DonHangDAL donHangDal, NhatKyBLL nhatKy)
        {
            _sachDal = sachDal;
            _theLoaiDal = theLoaiDal;
            _donHangDal = donHangDal;
            _nhatKy = nhatKy;
        }

        // Lọc + sắp xếp + phân trang bằng LINQ trên danh sách đọc từ Sach.xml
        public TrangKetQua<Sach> Loc(BoLocSach boLoc)
        {
            if (boLoc.GiaTu.HasValue && boLoc.GiaDen.HasValue && boLoc.GiaTu.Value > boLoc.GiaDen.Value)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Giá từ phải nhỏ hơn hoặc bằng giá đến");
            }

            var truyVan = _sachDal.LayTatCa().AsEnumerable();

            // Tìm không dấu, không phân biệt hoa thường bằng XPath translate():
            // gõ "nguyen nhat anh" vẫn ra "Nguyễn Nhật Ánh"
            var tuKhoa = ChuanHoa.BoDau(ChuanHoa.MotDong(boLoc.TuKhoa));
            if (tuKhoa.Length > 0)
            {
                var maPhuHop = _sachDal.TimMaSachBangXPath(tuKhoa).ToHashSet();
                truyVan = truyVan.Where(s => maPhuHop.Contains(s.MaSach));
            }

            if (!string.IsNullOrWhiteSpace(boLoc.MaTheLoai))
            {
                truyVan = truyVan.Where(s => s.MaTheLoai == boLoc.MaTheLoai);
            }

            if (boLoc.GiaTu.HasValue)
            {
                truyVan = truyVan.Where(s => s.GiaBan >= boLoc.GiaTu.Value);
            }

            if (boLoc.GiaDen.HasValue)
            {
                truyVan = truyVan.Where(s => s.GiaBan <= boLoc.GiaDen.Value);
            }

            switch (boLoc.SapXep)
            {
                case "ten":
                    truyVan = truyVan.OrderBy(s => ChuanHoa.BoDau(s.TenSach));
                    break;
                case "gia-tang":
                    truyVan = truyVan.OrderBy(s => s.GiaBan).ThenBy(s => s.MaSach);
                    break;
                case "gia-giam":
                    truyVan = truyVan.OrderByDescending(s => s.GiaBan).ThenBy(s => s.MaSach);
                    break;
                case "moi":
                    truyVan = truyVan.OrderByDescending(s => s.NamXuatBan).ThenByDescending(s => s.MaSach);
                    break;
                case "ton-it":
                    truyVan = truyVan.OrderBy(s => s.SoLuongTon).ThenBy(s => s.MaSach);
                    break;
                default:
                    truyVan = truyVan.OrderBy(s => s.MaSach);
                    break;
            }

            return PhanTrang.Tao(truyVan.ToList(), boLoc.Trang, boLoc.KichThuoc, 50);
        }

        public Sach LayTheoMa(string ma)
        {
            return _sachDal.LayTheoMa(ma)
                ?? throw LoiNghiepVuException.KhongTimThay($"Không tìm thấy sách {ma}");
        }

        // Tìm kiếm nâng cao bằng XPath (DAL dựng biểu thức), trả cả biểu thức để giao diện hiện ra
        public KetQuaTimXPath TimKiemXPath(DieuKienTimSach dieuKien)
        {
            if (dieuKien.GiaTu.HasValue && dieuKien.GiaDen.HasValue && dieuKien.GiaTu.Value > dieuKien.GiaDen.Value)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Giá từ phải nhỏ hơn hoặc bằng giá đến");
            }
            if (dieuKien.NamTu.HasValue && dieuKien.NamDen.HasValue && dieuKien.NamTu.Value > dieuKien.NamDen.Value)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Năm từ phải nhỏ hơn hoặc bằng năm đến");
            }

            dieuKien.TenSach = ChuanHoa.MotDongTuyChon(dieuKien.TenSach);
            dieuKien.TacGia = ChuanHoa.MotDongTuyChon(dieuKien.TacGia);
            dieuKien.NhaXuatBan = ChuanHoa.MotDongTuyChon(dieuKien.NhaXuatBan);
            var kiemTra = new BoKiemTra();
            kiemTra.ToiDa(dieuKien.TenSach, "Tên sách", 200);
            kiemTra.ToiDa(dieuKien.TacGia, "Tác giả", 100);
            kiemTra.ToiDa(dieuKien.NhaXuatBan, "Nhà xuất bản", 100);
            kiemTra.NemNeuCoLoi();

            // Bỏ dấu + chữ thường trước khi đưa vào XPath (gõ "nha xuat ban tre" vẫn khớp "NXB Trẻ")
            dieuKien.TenSach = dieuKien.TenSach == null ? null : ChuanHoa.BoDau(dieuKien.TenSach);
            dieuKien.TacGia = dieuKien.TacGia == null ? null : ChuanHoa.BoDau(dieuKien.TacGia);
            dieuKien.NhaXuatBan = dieuKien.NhaXuatBan == null ? null : ChuanHoa.BoDau(dieuKien.NhaXuatBan);

            var (ketQua, bieuThuc) = _sachDal.TimKiemXPath(dieuKien);
            return new KetQuaTimXPath
            {
                BieuThuc = bieuThuc,
                SoKetQua = ketQua.Count,
                DuLieu = ketQua.OrderBy(s => s.MaSach).ToList()
            };
        }

        // Sách cùng thể loại (trừ chính nó), còn hàng xếp trước
        public List<Sach> LayLienQuan(string ma, int soLuong)
        {
            var sach = LayTheoMa(ma);
            return _sachDal.LayTatCa()
                .Where(s => s.MaTheLoai == sach.MaTheLoai && s.MaSach != ma)
                .OrderByDescending(s => s.SoLuongTon > 0)
                .ThenByDescending(s => s.NamXuatBan)
                .Take(Math.Clamp(soLuong, 1, 12))
                .ToList();
        }

        public Sach Them(Sach dauVao, NguoiThucHien nguoi)
        {
            var sach = ChuanHoaVaKiemTra(dauVao);

            lock (XmlStore.Khoa)
            {
                KiemTraTheLoai(sach.MaTheLoai);
                KiemTraTrung(sach, null);
                sach.MaSach = _sachDal.TaoMaMoi();
                sach.HinhAnh = null;    // ảnh chỉ gắn qua API upload riêng
                _sachDal.Them(sach);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.Them, DoiTuongNhatKy.Sach, sach.MaSach,
                    $"Thêm sách \"{sach.TenSach}\" - giá {sach.GiaBan:0}, tồn {sach.SoLuongTon}");
            }

            return sach;
        }

        public Sach CapNhat(string ma, Sach dauVao, NguoiThucHien nguoi)
        {
            var sach = ChuanHoaVaKiemTra(dauVao);
            sach.MaSach = ma;

            lock (XmlStore.Khoa)
            {
                var cu = LayTheoMa(ma);
                KiemTraTheLoai(sach.MaTheLoai);
                KiemTraTrung(sach, ma);
                sach.HinhAnh = cu.HinhAnh;  // giữ ảnh cũ, đổi ảnh đi qua API upload
                _sachDal.CapNhat(sach);

                var thayDoi = ThayDoi.MoTa(
                    ("TenSach", cu.TenSach, sach.TenSach),
                    ("MaTheLoai", cu.MaTheLoai, sach.MaTheLoai),
                    ("TacGia", cu.TacGia, sach.TacGia),
                    ("NhaXuatBan", cu.NhaXuatBan, sach.NhaXuatBan),
                    ("NamXuatBan", cu.NamXuatBan, sach.NamXuatBan),
                    ("GiaBan", cu.GiaBan, sach.GiaBan),
                    ("SoLuongTon", cu.SoLuongTon, sach.SoLuongTon),
                    ("MoTa", cu.MoTa, sach.MoTa));
                if (thayDoi.Length > 0)
                {
                    _nhatKy.Ghi(nguoi, HanhDongNhatKy.Sua, DoiTuongNhatKy.Sach, ma, $"Sửa sách {ma}: {thayDoi}");
                }
            }

            return sach;
        }

        // Gắn tên file ảnh mới, trả về tên file cũ để tầng Web xóa file cũ nếu khác tên
        public string? CapNhatHinhAnh(string ma, string tenFileAnh, NguoiThucHien nguoi)
        {
            lock (XmlStore.Khoa)
            {
                var sach = LayTheoMa(ma);
                var anhCu = sach.HinhAnh;
                sach.HinhAnh = tenFileAnh;
                _sachDal.CapNhat(sach);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.Sua, DoiTuongNhatKy.Sach, ma, $"Đổi ảnh bìa sách {ma}: {anhCu ?? "(trống)"} → {tenFileAnh}");
                return anhCu;
            }
        }

        // Chặn xóa sách đã có trong đơn hàng (kiểm tra chéo DonHang.xml bằng XPath)
        public Sach Xoa(string ma, NguoiThucHien nguoi)
        {
            lock (XmlStore.Khoa)
            {
                var sach = LayTheoMa(ma);
                var soDon = _donHangDal.DemDonChuaSach(ma);
                if (soDon > 0)
                {
                    throw LoiNghiepVuException.XungDot(
                        $"Sách \"{sach.TenSach}\" đã có trong {soDon} đơn hàng, không xóa được (cần giữ lại để đối chiếu đơn cũ).");
                }

                _sachDal.Xoa(ma);
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.Xoa, DoiTuongNhatKy.Sach, ma, $"Xóa sách \"{sach.TenSach}\"");
                return sach;
            }
        }

        private static Sach ChuanHoaVaKiemTra(Sach dauVao)
        {
            var sach = new Sach
            {
                MaTheLoai = ChuanHoa.MotDong(dauVao.MaTheLoai),
                TenSach = ChuanHoa.MotDong(dauVao.TenSach),
                TacGia = ChuanHoa.MotDong(dauVao.TacGia),
                NhaXuatBan = ChuanHoa.MotDong(dauVao.NhaXuatBan),
                NamXuatBan = dauVao.NamXuatBan,
                GiaBan = dauVao.GiaBan,
                SoLuongTon = dauVao.SoLuongTon,
                MoTa = ChuanHoa.NhieuDong(dauVao.MoTa)
            };

            var namToiDa = DateTime.Now.Year + 1;
            var kiemTra = new BoKiemTra();
            kiemTra.Them(sach.MaTheLoai.Length == 0, "Chưa chọn thể loại");
            kiemTra.BatBuoc(sach.TenSach, "Tên sách", 200);
            kiemTra.BatBuoc(sach.TacGia, "Tác giả", 100);
            kiemTra.BatBuoc(sach.NhaXuatBan, "Nhà xuất bản", 100);
            kiemTra.Them(sach.NamXuatBan < 1900 || sach.NamXuatBan > namToiDa, $"Năm xuất bản phải từ 1900 đến {namToiDa}");
            kiemTra.Them(sach.GiaBan <= 0 || sach.GiaBan > 100000000, "Giá bán phải lớn hơn 0 và không quá 100.000.000");
            kiemTra.Them(sach.GiaBan != decimal.Round(sach.GiaBan, 0), "Giá bán phải là số nguyên (VNĐ)");
            kiemTra.Them(sach.SoLuongTon < 0 || sach.SoLuongTon > 1000000, "Số lượng tồn phải từ 0 đến 1.000.000");
            kiemTra.ToiDa(sach.MoTa, "Mô tả", 2000);
            kiemTra.NemNeuCoLoi();
            return sach;
        }

        // Cùng tên + tác giả + NXB + năm xuất bản (không phân biệt hoa thường) là cùng một đầu sách:
        // nhập thêm hàng thì tăng tồn kho, không tạo mã mới. Khác năm thì coi là bản tái bản, vẫn cho thêm.
        private void KiemTraTrung(Sach sach, string? maBoQua)
        {
            var trung = _sachDal.LayTatCa().FirstOrDefault(s =>
                s.MaSach != maBoQua &&
                s.NamXuatBan == sach.NamXuatBan &&
                string.Equals(s.TenSach, sach.TenSach, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.TacGia, sach.TacGia, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.NhaXuatBan, sach.NhaXuatBan, StringComparison.OrdinalIgnoreCase));

            if (trung != null)
            {
                throw LoiNghiepVuException.XungDot(
                    $"Đã có sách {trung.MaSach} cùng tên, tác giả, NXB và năm xuất bản. Nhập thêm hàng thì sửa số lượng tồn của {trung.MaSach}.");
            }
        }

        // MaTheLoai là khóa ngoại sang file khác, XSD không keyref được nên BLL kiểm tra
        private void KiemTraTheLoai(string maTheLoai)
        {
            if (_theLoaiDal.LayTheoMa(maTheLoai) == null)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, $"Thể loại {maTheLoai} không tồn tại");
            }
        }
    }
}
