using System.Security.Cryptography;
using System.Text;

namespace BanSach.BLL
{
    // Băm mật khẩu bằng PBKDF2 (SHA256, 100000 vòng, salt 16 byte, hash 32 byte), lưu base64 trong TaiKhoan.xml.
    // Không bao giờ lưu mật khẩu gốc.
    public static class MatKhauHelper
    {
        private const int SoVong = 100000;
        private const int DoDaiSalt = 16;
        private const int DoDaiHash = 32;

        public static (string Hash, string Salt) TaoHash(string matKhau)
        {
            var salt = RandomNumberGenerator.GetBytes(DoDaiSalt);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(matKhau), salt, SoVong, HashAlgorithmName.SHA256, DoDaiHash);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public static bool KiemTra(string matKhau, string hashBase64, string saltBase64)
        {
            var salt = Convert.FromBase64String(saltBase64);
            var hashLuu = Convert.FromBase64String(hashBase64);
            var hashNhap = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(matKhau), salt, SoVong, HashAlgorithmName.SHA256, hashLuu.Length);

            // So sánh thời gian cố định: không lộ "đúng được mấy byte" qua thời gian phản hồi
            return CryptographicOperations.FixedTimeEquals(hashNhap, hashLuu);
        }
    }
}
