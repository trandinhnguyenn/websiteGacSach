using BanSach.BLL;

namespace BanSach.Web.Services
{
    // Lưu / xóa ảnh bìa trong wwwroot/images/sach. Thuộc tầng Web vì làm việc với thư mục web, không phải XML.
    public class AnhBiaService
    {
        public const long KichThuocToiDa = 2 * 1024 * 1024; // 2MB
        public const string AnhMacDinh = "no-image.png";

        private static readonly string[] DuoiChoPhep = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly string _thuMucAnh;

        public AnhBiaService(IWebHostEnvironment env)
        {
            _thuMucAnh = Path.Combine(env.WebRootPath, "images", "sach");
        }

        // Kiểm tra rồi lưu ảnh với tên theo mã sách (S001.png). Trả về tên file đã lưu.
        public async Task<string> LuuAsync(string maSach, IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Chưa chọn file ảnh");
            }

            if (file.Length > KichThuocToiDa)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Ảnh bìa tối đa 2MB");
            }

            var duoi = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!DuoiChoPhep.Contains(duoi))
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Chỉ nhận ảnh JPG, PNG hoặc WEBP");
            }

            byte[] duLieu;
            using (var boNho = new MemoryStream())
            {
                await file.CopyToAsync(boNho);
                duLieu = boNho.ToArray();
            }

            // Không tin đuôi file: đọc mấy byte đầu (magic bytes) để biết đúng là ảnh gì.
            // Đổi tên virus.exe thành virus.png sẽ bị chặn ở đây.
            var loaiThat = NhanDienAnh(duLieu);
            var khopDuoi = loaiThat switch
            {
                "png" => duoi == ".png",
                "jpg" => duoi == ".jpg" || duoi == ".jpeg",
                "webp" => duoi == ".webp",
                _ => false
            };
            if (!khopDuoi)
            {
                throw new LoiNghiepVuException(LoaiLoi.KhongHopLe, "Nội dung file không phải ảnh hợp lệ hoặc không khớp đuôi file");
            }

            // Tên file do server đặt theo mã sách, không dùng tên người dùng gửi lên (tránh ../../ hay ký tự lạ)
            var tenFile = $"{maSach}.{loaiThat}";
            await File.WriteAllBytesAsync(Path.Combine(_thuMucAnh, tenFile), duLieu);
            return tenFile;
        }

        public void Xoa(string? tenFile)
        {
            if (string.IsNullOrEmpty(tenFile) || tenFile == AnhMacDinh)
            {
                return;
            }

            var duongDan = Path.Combine(_thuMucAnh, Path.GetFileName(tenFile));
            if (File.Exists(duongDan))
            {
                File.Delete(duongDan);
            }
        }

        private static string? NhanDienAnh(byte[] b)
        {
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            {
                return "png";
            }

            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            {
                return "jpg";
            }

            // RIFF....WEBP
            if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
            {
                return "webp";
            }

            return null;
        }
    }
}
