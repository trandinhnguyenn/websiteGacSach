// Quản lý khách hàng: /api/khachhang (chỉ Admin)
(() => {
    'use strict';
    const { api, e, thongBao, hienLoi, anLoi, doiGoXong } = BanSach;

    const bang = document.getElementById('bangKhachHang');
    const locTuKhoa = document.getElementById('locTuKhoa');
    const modalEl = document.getElementById('modalKhachHang');
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById('formKhachHang');
    const khungLoi = form.querySelector('[data-khung-loi]');
    const nutLuu = document.getElementById('nutLuu');

    let danhSach = [];
    let maDangSua = null;

    async function taiDanhSach() {
        try {
            danhSach = await api('/api/khachhang?tuKhoa=' + encodeURIComponent(locTuKhoa.value));
            if (danhSach.length === 0) {
                bang.innerHTML = '<tr><td colspan="8" class="text-muted">Không có khách hàng phù hợp</td></tr>';
                return;
            }
            bang.innerHTML = danhSach.map(kh => `
                <tr>
                    <td><span class="ma">${e(kh.maKhachHang)}</span></td>
                    <td class="ten-chinh">${e(kh.hoTen)}</td>
                    <td class="text-nowrap">${e(kh.soDienThoai)}</td>
                    <td class="dong-phu">${e(kh.email)}</td>
                    <td class="dong-phu">${e(kh.diaChi)}</td>
                    <td>${kh.tenDangNhap
                        ? `<span class="nhan nhan--den">${e(kh.tenDangNhap)}</span>`
                        : '<span class="nhan">Chưa có</span>'}</td>
                    <td class="text-end">${kh.soDonHang > 0
                        ? `<a href="/Admin/DonHang?tuKhoa=${encodeURIComponent(kh.maKhachHang)}" title="Xem đơn của khách này">${kh.soDonHang}</a>`
                        : '0'}</td>
                    <td class="cot-hanh-dong">
                        <button type="button" class="btn btn-sm btn-outline-secondary" data-sua="${e(kh.maKhachHang)}">Sửa</button>
                        <button type="button" class="btn btn-sm btn-outline-danger" data-xoa="${e(kh.maKhachHang)}">Xóa</button>
                    </td>
                </tr>`).join('');
        } catch (loi) {
            bang.innerHTML = `<tr><td colspan="8" class="text-danger">${e(loi.message)}</td></tr>`;
        }
    }

    function moForm(khachHang) {
        maDangSua = khachHang ? khachHang.maKhachHang : null;
        document.getElementById('tieuDeModal').textContent = khachHang ? `Sửa khách hàng ${khachHang.maKhachHang}` : 'Thêm khách hàng';
        document.getElementById('hoTen').value = khachHang ? khachHang.hoTen : '';
        document.getElementById('soDienThoai').value = khachHang ? khachHang.soDienThoai : '';
        document.getElementById('email').value = khachHang ? (khachHang.email || '') : '';
        document.getElementById('diaChi').value = khachHang ? khachHang.diaChi : '';
        anLoi(khungLoi);
        modal.show();
    }

    modalEl.addEventListener('shown.bs.modal', () => document.getElementById('hoTen').focus());

    form.addEventListener('submit', async (su) => {
        su.preventDefault();
        anLoi(khungLoi);
        nutLuu.disabled = true;

        const duLieu = {
            hoTen: document.getElementById('hoTen').value,
            soDienThoai: document.getElementById('soDienThoai').value,
            email: document.getElementById('email').value,
            diaChi: document.getElementById('diaChi').value
        };
        try {
            if (maDangSua) {
                await api(`/api/khachhang/${encodeURIComponent(maDangSua)}`, { method: 'PUT', body: duLieu });
                thongBao('Đã cập nhật khách hàng');
            } else {
                const moi = await api('/api/khachhang', { method: 'POST', body: duLieu });
                thongBao(`Đã thêm khách hàng ${moi.maKhachHang}`);
            }
            modal.hide();
            await taiDanhSach();
        } catch (loi) {
            hienLoi(khungLoi, loi);
        } finally {
            nutLuu.disabled = false;
        }
    });

    bang.addEventListener('click', async (su) => {
        const nutSua = su.target.closest('[data-sua]');
        const nutXoa = su.target.closest('[data-xoa]');

        if (nutSua) {
            moForm(danhSach.find(kh => kh.maKhachHang === nutSua.dataset.sua));
        }

        if (nutXoa) {
            const khachHang = danhSach.find(kh => kh.maKhachHang === nutXoa.dataset.xoa);
            if (!confirm(`Xóa khách hàng "${khachHang.hoTen}"?`)) {
                return;
            }
            try {
                await api(`/api/khachhang/${encodeURIComponent(khachHang.maKhachHang)}`, { method: 'DELETE' });
                thongBao('Đã xóa khách hàng');
                await taiDanhSach();
            } catch (loi) {
                thongBao(loi.message, 'danger');     // vd 409: đã có đơn / đã có tài khoản
            }
        }
    });

    document.getElementById('nutThem').addEventListener('click', () => moForm(null));
    locTuKhoa.addEventListener('input', doiGoXong(taiDanhSach));

    taiDanhSach();
})();
