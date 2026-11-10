// Xem nhật ký thao tác: GET /api/nhatky
(() => {
    'use strict';
    const { api, e, ngayGio, phanTrang } = BanSach;

    const TEN_HANH_DONG = {
        Them: ['Thêm', 'den'],
        Sua: ['Sửa', 'cham'],
        Xoa: ['Xóa', 'do'],
        DoiTrangThai: ['Đổi trạng thái', 'cham'],
        HuyDon: ['Hủy đơn', 'vang'],
        NhapXml: ['Nhập XML', 'den'],
        DangKy: ['Đăng ký', 'cham'],
        DoiMatKhau: ['Đổi mật khẩu', 'vang']
    };
    const TEN_DOI_TUONG = {
        TheLoai: 'Thể loại', Sach: 'Sách', KhachHang: 'Khách hàng', DonHang: 'Đơn hàng', DuLieu: 'Dữ liệu', TaiKhoan: 'Tài khoản'
    };

    const bang = document.getElementById('bangNhatKy');
    const locDoiTuong = document.getElementById('locDoiTuong');
    const locHanhDong = document.getElementById('locHanhDong');

    async function taiDanhSach(trang = 1) {
        const thamSo = new URLSearchParams({
            doiTuong: locDoiTuong.value,
            hanhDong: locHanhDong.value,
            trang,
            kichThuoc: 20
        });
        try {
            const ketQua = await api('/api/nhatky?' + thamSo);
            document.getElementById('tongSoNhatKy').textContent = `${ketQua.tongSo} dòng nhật ký`;
            phanTrang(document.getElementById('phanTrangNhatKy'), ketQua, taiDanhSach);

            if (ketQua.duLieu.length === 0) {
                bang.innerHTML = '<tr><td colspan="4" class="text-muted">Chưa có thao tác nào</td></tr>';
                return;
            }
            bang.innerHTML = ketQua.duLieu.map(nk => {
                const [ten, mau] = TEN_HANH_DONG[nk.hanhDong] || [nk.hanhDong, ''];
                // 4 cột: gộp người làm + thời gian, hành động + đối tượng để cột Nội dung (dài nhất) đủ chỗ
                return `<tr>
                    <td class="d-none d-md-table-cell"><span class="ma">${e(nk.maNhatKy)}</span></td>
                    <td class="text-nowrap">
                        <div class="ten-chinh">${e(nk.tenDangNhap)}</div>
                        <div class="dong-phu">${e(ngayGio(nk.thoiGian))}</div>
                    </td>
                    <td class="text-nowrap">
                        <span class="nhan nhan--${mau}">${e(ten)}</span>
                        <div class="dong-phu mt-1">${e(TEN_DOI_TUONG[nk.doiTuong] || nk.doiTuong)} ${nk.maDoiTuong ? `<span class="ma">${e(nk.maDoiTuong)}</span>` : ''}</div>
                    </td>
                    <td class="cot-noi-dung-nhat-ky">${e(nk.noiDung)}</td>
                </tr>`;
            }).join('');
        } catch (loi) {
            bang.innerHTML = `<tr><td colspan="4" class="text-danger">${e(loi.message)}</td></tr>`;
        }
    }

    locDoiTuong.addEventListener('change', () => taiDanhSach(1));
    locHanhDong.addEventListener('change', () => taiDanhSach(1));

    taiDanhSach(1);
})();
