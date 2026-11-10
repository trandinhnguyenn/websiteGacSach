// Đăng ký: POST /api/auth/dangky, thành công thì server đăng nhập luôn rồi chuyển trang
(() => {
    'use strict';
    const { api, anhBia, hienLoi, anLoi, theoDoiMatKhau } = BanSach;

    const form = document.getElementById('formDangKy');
    const khungLoi = document.getElementById('loiDangKy');
    const khungXong = document.getElementById('xongDangKy');
    const nut = document.getElementById('nutDangKy');
    const giaTri = id => document.getElementById(id).value;

    // Góc tường sách bên trái giống trang đăng nhập
    const tuong = document.getElementById('tuongDangNhap');
    api('/api/sach?sapXep=ten&kichThuoc=9')
        .then(kq => {
            tuong.innerHTML = kq.duLieu.map(s => `<img src="${anhBia(s.hinhAnh)}" alt="" decoding="async">`).join('');
        })
        .catch(() => { });

    // Quy tắc mật khẩu đánh dấu ngay khi gõ; đổi tên đăng nhập cũng phải xét lại quy tắc "không chứa tên"
    const kiemTraMatKhau = theoDoiMatKhau(document.getElementById('matKhau'), document.getElementById('quyTacMatKhau'), {
        layTenDangNhap: () => giaTri('tenDangNhap')
    });
    document.getElementById('tenDangNhap').addEventListener('input', kiemTraMatKhau);

    form.addEventListener('submit', async (su) => {
        su.preventDefault();
        anLoi(khungLoi);

        const chuaDat = kiemTraMatKhau();
        if (chuaDat.length) {
            hienLoi(khungLoi, { message: 'Mật khẩu chưa đạt yêu cầu', chiTiet: chuaDat });
            khungLoi.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return;
        }
        if (giaTri('matKhau') !== giaTri('nhapLaiMatKhau')) {
            hienLoi(khungLoi, { message: 'Hai lần nhập mật khẩu không khớp', chiTiet: [] });
            return;
        }

        nut.disabled = true;
        const quayLai = form.dataset.quayLai || '';
        try {
            const ketQua = await api('/api/auth/dangky' + (quayLai ? '?quayLai=' + encodeURIComponent(quayLai) : ''), {
                method: 'POST',
                body: {
                    hoTen: giaTri('hoTen'),
                    soDienThoai: giaTri('soDienThoai'),
                    email: giaTri('email'),
                    diaChi: giaTri('diaChi'),
                    tenDangNhap: giaTri('tenDangNhap'),
                    matKhau: giaTri('matKhau')
                }
            });
            khungXong.textContent = `${ketQua.thongBao} Đang chuyển trang...`;
            khungXong.classList.remove('d-none');
            setTimeout(() => { location.href = ketQua.chuyenHuong || '/'; }, 1200);
        } catch (loi) {
            hienLoi(khungLoi, loi);
            nut.disabled = false;
            khungLoi.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });
})();
