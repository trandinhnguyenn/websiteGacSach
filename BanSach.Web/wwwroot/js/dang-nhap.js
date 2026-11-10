// Trang đăng nhập: gửi POST /api/auth/dangnhap, thành công thì chuyển trang
(() => {
    'use strict';
    const { api, hienLoi, anLoi } = BanSach;
    const form = document.getElementById('formDangNhap');
    const khungLoi = document.getElementById('loiDangNhap');
    const nut = document.getElementById('nutDangNhap');

    // Góc tường sách bên trái: 9 bìa mới nhất (lỗi thì bỏ qua, không ảnh hưởng đăng nhập)
    const tuong = document.getElementById('tuongDangNhap');
    BanSach.api('/api/sach?sapXep=moi&kichThuoc=9')
        .then(ketQua => {
            tuong.innerHTML = ketQua.duLieu
                .map(s => `<img src="${BanSach.anhBia(s.hinhAnh)}" alt="" decoding="async">`)
                .join('');
        })
        .catch(() => { });

    form.addEventListener('submit', async (su) => {
        su.preventDefault();
        anLoi(khungLoi);

        const tenDangNhap = document.getElementById('tenDangNhap').value.trim();
        const matKhau = document.getElementById('matKhau').value;
        if (!tenDangNhap || !matKhau) {
            hienLoi(khungLoi, { message: 'Nhập đủ tên đăng nhập và mật khẩu', chiTiet: [] });
            return;
        }

        nut.disabled = true;
        try {
            const ketQua = await api('/api/auth/dangnhap', {
                method: 'POST',
                boQua401: true,     // sai mật khẩu trả 401: hiện lỗi tại chỗ, không chuyển trang
                body: {
                    tenDangNhap,
                    matKhau,
                    ghiNho: document.getElementById('ghiNho').checked,
                    quayLai: form.dataset.quayLai || null
                }
            });
            // Tải trang mới để layout nhận cookie đăng nhập và token CSRF mới gắn với tài khoản này
            location.href = ketQua.chuyenHuong;
        } catch (loi) {
            hienLoi(khungLoi, loi);
            document.getElementById('matKhau').value = '';
            nut.disabled = false;
        }
    });
})();
