using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using BanSach.DAL;
using BanSach.DTO;

namespace BanSach.BLL
{
    // Xuất / nhập nguyên file XML trong App_Data (trang Quản trị > Dữ liệu XML).
    // Nhập đi qua 3 lớp kiểm tra: XSD của file tải lên -> XSD của dữ liệu sau khi gộp -> khóa ngoại sang các file khác.
    public class DuLieuBLL
    {
        // Thông tin từng file: phần tử gốc, phần tử bản ghi, thuộc tính khóa, có cho tải / nhập không
        private sealed record MoTaFile(string Goc, string BanGhi, string Khoa, string NoiDung, bool TaiDuoc, bool NhapDuoc, string? LyDoKhongNhap);

        private static readonly Dictionary<string, MoTaFile> DanhMuc = new Dictionary<string, MoTaFile>(StringComparer.OrdinalIgnoreCase)
        {
            ["TheLoai.xml"] = new MoTaFile("DanhSachTheLoai", "TheLoai", "MaTheLoai", "Thể loại sách", true, true, null),
            ["Sach.xml"] = new MoTaFile("DanhSachSach", "Sach", "MaSach", "Danh mục sách, giá, tồn kho", true, true, null),
            ["KhachHang.xml"] = new MoTaFile("DanhSachKhachHang", "KhachHang", "MaKhachHang", "Hồ sơ khách hàng", true, true, null),
            ["DonHang.xml"] = new MoTaFile("DanhSachDonHang", "DonHang", "MaDonHang", "Đơn hàng và chi tiết đơn", true, false,
                "Đơn hàng gắn với tồn kho nên chỉ tạo / đổi qua quy trình đặt hàng"),
            ["NhatKy.xml"] = new MoTaFile("DanhSachNhatKy", "NhatKy", "MaNhatKy", "Nhật ký thao tác", true, false,
                "Nhật ký phải giữ nguyên, không cho ghi đè"),
            ["TaiKhoan.xml"] = new MoTaFile("DanhSachTaiKhoan", "TaiKhoan", "MaTaiKhoan", "Tài khoản đăng nhập", false, false,
                "Chứa mật khẩu đã băm nên không cho tải về hay nhập"),
        };

        private const int SoLoiToiDa = 15;

        private readonly XmlStore _store;
        private readonly NhatKyBLL _nhatKy;

        public DuLieuBLL(XmlStore store, NhatKyBLL nhatKy)
        {
            _store = store;
            _nhatKy = nhatKy;
        }

        public List<TepDuLieu> LayDanhSachTep()
        {
            return DanhMuc.Select(muc =>
            {
                var thongTin = _store.ThongTinFile(muc.Key);
                var moTa = muc.Value;
                // XPath count(): đếm bản ghi mà không phải chuyển sang DTO
                var soBanGhi = Convert.ToInt32(_store.Doc(muc.Key).XPathEvaluate($"count(/{moTa.Goc}/{moTa.BanGhi})"));
                return new TepDuLieu
                {
                    TenFile = muc.Key,
                    TenXsd = Path.ChangeExtension(muc.Key, ".xsd"),
                    NoiDung = moTa.NoiDung,
                    SoBanGhi = soBanGhi,
                    KichThuoc = thongTin.Length,
                    SuaLuc = thongTin.LastWriteTime,
                    TaiDuoc = moTa.TaiDuoc,
                    NhapDuoc = moTa.NhapDuoc,
                    LyDoKhongNhap = moTa.LyDoKhongNhap
                };
            }).ToList();
        }

        // Tải về: chỉ file trong danh mục (chặn path traversal kiểu ../../appsettings.json).
        // XSD của mọi file đều tải được vì không chứa dữ liệu.
        public (byte[] NoiDung, string TenFile) TaiFile(string tenFile)
        {
            var laXsd = tenFile.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase);
            var tenXml = laXsd ? Path.ChangeExtension(tenFile, ".xml") : tenFile;

            if (!DanhMuc.TryGetValue(tenXml, out var moTa) || Path.GetFileName(tenFile) != tenFile)
            {
                throw LoiNghiepVuException.KhongTimThay($"Không có file {tenFile}");
            }
            if (!laXsd && !moTa.TaiDuoc)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongCoQuyen, $"{tenXml}: {moTa.LyDoKhongNhap}");
            }

            // Lấy đúng cách viết hoa trong danh mục (Sach.xml), không dùng chuỗi người dùng gửi lên
            var tenChuan = DanhMuc.Keys.First(k => string.Equals(k, tenXml, StringComparison.OrdinalIgnoreCase));
            var tenThat = laXsd ? Path.ChangeExtension(tenChuan, ".xsd") : tenChuan;
            return (_store.DocByte(tenThat), tenThat);
        }

        // Nhập file XML tải lên. chiKiemTra = true: chạy đủ mọi bước kiểm tra, báo trước sẽ thêm / sửa / xóa bao nhiêu, nhưng không ghi.
        // coAnhBia: tầng Web cho biết ảnh bìa có trong wwwroot hay chưa (chỉ để cảnh báo, không chặn).
        public KetQuaNhapXml NhapFile(string tenFile, Stream noiDung, string? cheDo, bool chiKiemTra,
            NguoiThucHien nguoi, Func<string, bool>? coAnhBia = null)
        {
            if (nguoi.VaiTro != VaiTro.Admin)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongCoQuyen, "Chỉ admin mới nhập được dữ liệu");
            }
            if (!DanhMuc.TryGetValue(tenFile, out var moTa))
            {
                throw LoiNghiepVuException.KhongTimThay($"Không có file {tenFile}");
            }
            tenFile = DanhMuc.Keys.First(k => string.Equals(k, tenFile, StringComparison.OrdinalIgnoreCase));
            if (!moTa.NhapDuoc)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongCoQuyen, $"Không nhập được {tenFile}: {moTa.LyDoKhongNhap}");
            }

            cheDo = string.IsNullOrWhiteSpace(cheDo) ? CheDoNhapXml.Gop : cheDo.Trim().ToLowerInvariant();
            if (cheDo != CheDoNhapXml.Gop && cheDo != CheDoNhapXml.Thay)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Chế độ nhập phải là \"gop\" hoặc \"thay\"");
            }

            // ----- Bước 1: đọc file, đúng loại dữ liệu, đúng XSD -----
            XDocument fileMoi;
            try
            {
                fileMoi = XmlStore.DocTuLuong(noiDung);
            }
            catch (XmlException loi)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "File tải lên không phải XML hợp lệ",
                    new List<string> { MoTaLoiXml(loi) });
            }

            if (fileMoi.Root?.Name.LocalName != moTa.Goc || fileMoi.Root.Name.Namespace != XNamespace.None)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe,
                    $"File này không phải dữ liệu của {tenFile}: phần tử gốc phải là <{moTa.Goc}>, file đang là <{fileMoi.Root?.Name.LocalName}>");
            }

            NemNeuCoLoi(_store.LayLoiXsd(fileMoi, tenFile), $"File tải lên không đúng cấu trúc {Path.ChangeExtension(tenFile, ".xsd")}");

            lock (XmlStore.Khoa)
            {
                // ----- Bước 2: gộp với dữ liệu hiện có, đếm thêm / sửa / giữ / xóa -----
                var hienTai = _store.Doc(tenFile);
                var banGhiMoi = fileMoi.Root.Elements(moTa.BanGhi).ToList();
                var cu = hienTai.Root!.Elements(moTa.BanGhi).ToDictionary(e => (string)e.Attribute(moTa.Khoa)!);
                var ketQua = new KetQuaNhapXml { TenFile = tenFile, CheDo = cheDo, SoBanGhiTrongFile = banGhiMoi.Count };

                foreach (var banGhi in banGhiMoi)
                {
                    var ma = (string)banGhi.Attribute(moTa.Khoa)!;
                    if (!cu.TryGetValue(ma, out var banGhiCu))
                    {
                        ketQua.ThemMoi++;
                    }
                    else if (XNode.DeepEquals(BoThongTinDong(banGhiCu), BoThongTinDong(banGhi)))
                    {
                        ketQua.GiuNguyen++;
                    }
                    else
                    {
                        ketQua.CapNhat++;
                    }
                }

                XDocument sauKhiNhap;
                if (cheDo == CheDoNhapXml.Thay)
                {
                    var maMoi = banGhiMoi.Select(e => (string)e.Attribute(moTa.Khoa)!).ToHashSet();
                    ketQua.Xoa = cu.Keys.Count(ma => !maMoi.Contains(ma));
                    // Giữ thuộc tính của phần tử gốc hiện tại (xsi:noNamespaceSchemaLocation), thay toàn bộ bản ghi
                    sauKhiNhap = new XDocument(new XElement(hienTai.Root.Name, hienTai.Root.Attributes(),
                        banGhiMoi.Select(BoThongTinDong)));
                }
                else
                {
                    sauKhiNhap = new XDocument(hienTai);
                    var theoMa = sauKhiNhap.Root!.Elements(moTa.BanGhi).ToDictionary(e => (string)e.Attribute(moTa.Khoa)!);
                    foreach (var banGhi in banGhiMoi)
                    {
                        var ma = (string)banGhi.Attribute(moTa.Khoa)!;
                        if (theoMa.TryGetValue(ma, out var dangCo))
                        {
                            dangCo.ReplaceWith(BoThongTinDong(banGhi));
                        }
                        else
                        {
                            sauKhiNhap.Root.Add(BoThongTinDong(banGhi));
                        }
                    }
                }

                // ----- Bước 3: dữ liệu sau khi gộp phải qua XSD (vd trùng số điện thoại với khách đang có) -----
                NemNeuCoLoi(_store.LayLoiXsd(sauKhiNhap, tenFile), "Dữ liệu sau khi gộp vi phạm ràng buộc trong XSD");

                // ----- Bước 4: khóa ngoại sang các file khác (XSD không kiểm tra được giữa 2 file) -----
                NemNeuCoLoi(KiemTraKhoaNgoai(tenFile, sauKhiNhap), "Dữ liệu nhập làm hỏng liên kết với file khác");

                if (tenFile == "Sach.xml" && coAnhBia != null)
                {
                    var thieuAnh = sauKhiNhap.Root!.Elements("Sach")
                        .Select(s => (string?)s.Element("HinhAnh"))
                        .Where(anh => !string.IsNullOrEmpty(anh) && !coAnhBia(anh!))
                        .Distinct()
                        .ToList();
                    if (thieuAnh.Count > 0)
                    {
                        ketQua.CanhBao.Add($"Chưa có file ảnh bìa: {string.Join(", ", thieuAnh.Take(10))}{(thieuAnh.Count > 10 ? "…" : "")}. Sách vẫn nhập được, trang web sẽ hiện ảnh trống cho tới khi tải ảnh lên.");
                    }
                }

                var coThayDoi = ketQua.ThemMoi + ketQua.CapNhat + ketQua.Xoa > 0;
                if (!coThayDoi)
                {
                    ketQua.CanhBao.Add("Dữ liệu trong file giống hệt dữ liệu đang có, không có gì để nhập.");
                }
                if (chiKiemTra || !coThayDoi)
                {
                    return ketQua;
                }

                // ----- Bước 5: ghi (XmlStore kiểm tra XSD lần cuối, giữ bản cũ dạng .bak) và ghi nhật ký -----
                _store.Luu(sauKhiNhap, tenFile);
                ketQua.DaNhap = true;
                _nhatKy.Ghi(nguoi, HanhDongNhatKy.NhapXml, DoiTuongNhatKy.DuLieu, null,
                    $"Nhập {tenFile} ({(cheDo == CheDoNhapXml.Thay ? "thay toàn bộ" : "gộp")}): {ketQua.ThemMoi} thêm mới, {ketQua.CapNhat} cập nhật, {ketQua.Xoa} xóa, {ketQua.GiuNguyen} giữ nguyên");
                return ketQua;
            }
        }

        // Khóa ngoại giữa các file, viết bằng XPath trên dữ liệu SAU khi nhập:
        //  - Sach/@MaTheLoai phải có trong TheLoai.xml
        //  - DonHang/ChiTiet/@MaSach phải có trong Sach.xml (đơn cũ vẫn cần biết sách nào)
        //  - DonHang/@MaKhachHang, TaiKhoan/@MaKhachHang phải có trong KhachHang.xml
        private List<string> KiemTraKhoaNgoai(string tenFile, XDocument sauKhiNhap)
        {
            var loi = new List<string>();

            HashSet<string> LayGiaTri(XDocument doc, string xpath) =>
                ((IEnumerable<object>)doc.XPathEvaluate(xpath)).OfType<XAttribute>().Select(a => a.Value).ToHashSet();

            void KiemTra(HashSet<string> canCo, HashSet<string> dangCo, Func<string, string> thongBao)
            {
                loi.AddRange(canCo.Where(ma => !dangCo.Contains(ma)).OrderBy(ma => ma).Select(thongBao));
            }

            switch (tenFile)
            {
                case "TheLoai.xml":
                {
                    var maTheLoai = LayGiaTri(sauKhiNhap, "/DanhSachTheLoai/TheLoai/@MaTheLoai");
                    var sach = _store.Doc("Sach.xml");
                    KiemTra(LayGiaTri(sach, "/DanhSachSach/Sach/@MaTheLoai"), maTheLoai, ma =>
                        $"Thể loại {ma} đang có {Convert.ToInt32(sach.XPathEvaluate($"count(/DanhSachSach/Sach[@MaTheLoai={XPathHelper.Literal(ma)}])"))} sách trong Sach.xml nên không bỏ đi được");
                    break;
                }
                case "Sach.xml":
                {
                    var maSach = LayGiaTri(sauKhiNhap, "/DanhSachSach/Sach/@MaSach");
                    KiemTra(LayGiaTri(sauKhiNhap, "/DanhSachSach/Sach/@MaTheLoai"),
                        LayGiaTri(_store.Doc("TheLoai.xml"), "/DanhSachTheLoai/TheLoai/@MaTheLoai"),
                        ma => $"Thể loại {ma} chưa có trong TheLoai.xml (thêm thể loại trước rồi nhập sách)");
                    KiemTra(LayGiaTri(_store.Doc("DonHang.xml"), "/DanhSachDonHang/DonHang/ChiTiet/@MaSach"), maSach,
                        ma => $"Sách {ma} đã có trong đơn hàng nên không bỏ đi được");
                    break;
                }
                case "KhachHang.xml":
                {
                    var maKhach = LayGiaTri(sauKhiNhap, "/DanhSachKhachHang/KhachHang/@MaKhachHang");
                    KiemTra(LayGiaTri(_store.Doc("DonHang.xml"), "/DanhSachDonHang/DonHang/@MaKhachHang"), maKhach,
                        ma => $"Khách {ma} đã có đơn hàng nên không bỏ đi được");
                    KiemTra(LayGiaTri(_store.Doc("TaiKhoan.xml"), "/DanhSachTaiKhoan/TaiKhoan/@MaKhachHang"), maKhach,
                        ma => $"Khách {ma} đang gắn với một tài khoản đăng nhập nên không bỏ đi được");
                    break;
                }
            }
            return loi;
        }

        // Bỏ thông tin số dòng (chỉ dùng để báo lỗi) và tạo bản sao độc lập trước khi đưa vào tài liệu khác
        private static XElement BoThongTinDong(XElement e)
        {
            return new XElement(e);
        }

        // XmlException tự nối " Line 5, position 3." vào cuối câu; bỏ đi để không lặp với "Dòng 5, cột 3:" phía trước
        private static string MoTaLoiXml(XmlException loi)
        {
            if (loi.LineNumber <= 0)
            {
                return loi.Message;
            }
            var cauLoi = loi.Message;
            var viTri = cauLoi.LastIndexOf(" Line ", StringComparison.Ordinal);
            if (viTri > 0)
            {
                cauLoi = cauLoi.Substring(0, viTri);
            }
            return $"Dòng {loi.LineNumber}, cột {loi.LinePosition}: {cauLoi}";
        }

        private static void NemNeuCoLoi(List<string> loi, string thongBao)
        {
            if (loi.Count == 0)
            {
                return;
            }
            var chiTiet = loi.Take(SoLoiToiDa).ToList();
            if (loi.Count > SoLoiToiDa)
            {
                chiTiet.Add($"… và {loi.Count - SoLoiToiDa} lỗi khác");
            }
            throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, thongBao, chiTiet);
        }
    }
}
