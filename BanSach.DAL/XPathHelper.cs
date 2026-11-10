namespace BanSach.DAL
{
    // Đưa giá trị người dùng nhập vào biểu thức XPath một cách an toàn.
    // Nếu ghép thẳng: //TaiKhoan[TenDangNhap='{nhap}'] mà người dùng nhập  x' or '1'='1
    // thì biểu thức thành //TaiKhoan[TenDangNhap='x' or '1'='1'] -> trả về mọi tài khoản (XPath injection).
    // Hàm này luôn biến giá trị thành một chuỗi literal, dấu nháy bên trong không thoát ra được.
    public static class XPathHelper
    {
        public static string Literal(string giaTri)
        {
            if (!giaTri.Contains('\''))
            {
                return "'" + giaTri + "'";
            }

            if (!giaTri.Contains('"'))
            {
                return "\"" + giaTri + "\"";
            }

            // Có cả ' và " : tách theo ' rồi nối lại bằng concat('a', "'", 'b')
            var cacPhan = giaTri.Split('\'').Select(phan => "'" + phan + "'");
            return "concat(" + string.Join(", \"'\", ", cacPhan) + ")";
        }
    }
}
