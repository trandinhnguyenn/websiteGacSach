// Hồ sơ: GET/PUT /api/taikhoan/hoso (chỉ khách), POST /api/taikhoan/doimatkhau (mọi tài khoản)
(() => {
    'use strict';
    const { api, thongBao, hienLoi, anLoi, theoDoiMatKhau } = BanSach;
    const giaTri = id => document.getElementById(id).value;

    const formHoSo = document.getElementById('formHoSo');
    if (formHoSo) {
        const khungLoi = formHoSo.querySelector('[data-khung-loi]');

        api('/api/taikhoan/hoso')
            .then(hs => {
                document.getElementById('hoTen').value = hs.hoTen;
                document.getElementById('soDienThoai').value = hs.soDienThoai;
                document.getElementById('email').value = hs.email || '';
                document.getElementById('diaChi').value = hs.diaChi;
            })
            .catch(loi => thongBao(loi.message, 'danger'));

        formHoSo.addEventListener('submit', async (su) => {
            su.preventDefault();
            anLoi(khungLoi);
            try {
                await api('/api/taikhoan/hoso', {
                    method: 'PUT',
                    body: { hoTen: giaTri('hoTen'), soDienThoai: giaTri('soDienThoai'), email: giaTri('email'), diaChi: giaTri('diaChi') }
                });
                thongBao('Đã lưu thông tin');
            } catch (loi) {
                hienLoi(khungLoi, loi);
            }
        });
    }

    const formMatKhau = document.getElementById('formDoiMatKhau');
    const loiMatKhau = formMatKhau.querySelector('[data-khung-loi]');
    const oMatKhauMoi = document.getElementById('matKhauMoi');
    const kiemTraMatKhauMoi = theoDoiMatKhau(oMatKhauMoi, document.getElementById('quyTacMatKhau'), {
        layTenDangNhap: () => formMatKhau.dataset.tenDangNhap,
        quyTacThem: (matKhau) => ({ khacMatKhauCu: matKhau.length > 0 && matKhau !== giaTri('matKhauCu') })
    });
    document.getElementById('matKhauCu').addEventListener('input', kiemTraMatKhauMoi);

    formMatKhau.addEventListener('submit', async (su) => {
        su.preventDefault();
        anLoi(loiMatKhau);
        const chuaDat = kiemTraMatKhauMoi();
        if (chuaDat.length) {
            hienLoi(loiMatKhau, { message: 'Mật khẩu mới chưa đạt yêu cầu', chiTiet: chuaDat });
            return;
        }
        if (giaTri('matKhauMoi') !== giaTri('nhapLaiMatKhauMoi')) {
            hienLoi(loiMatKhau, { message: 'Hai lần nhập mật khẩu mới không khớp', chiTiet: [] });
            return;
        }
        try {
            await api('/api/taikhoan/doimatkhau', {
                method: 'POST',
                body: { matKhauCu: giaTri('matKhauCu'), matKhauMoi: giaTri('matKhauMoi') }
            });
            formMatKhau.reset();
            kiemTraMatKhauMoi();
            // Cookie mới không còn đánh dấu mật khẩu yếu: bỏ luôn thanh nhắc, khỏi chờ tải lại trang
            document.getElementById('nhacMatKhau')?.remove();
            thongBao('Đã đổi mật khẩu. Các nơi khác đang đăng nhập tài khoản này đã bị đăng xuất.');
        } catch (loi) {
            hienLoi(loiMatKhau, loi);
        }
    });
})();
