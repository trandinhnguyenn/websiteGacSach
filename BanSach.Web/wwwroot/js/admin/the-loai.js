// Quản lý thể loại: mọi thao tác đi qua /api/theloai
(() => {
    'use strict';
    const { api, e, thongBao, hienLoi, anLoi } = BanSach;

    const bang = document.getElementById('bangTheLoai');
    const modalEl = document.getElementById('modalTheLoai');
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById('formTheLoai');
    const khungLoi = form.querySelector('[data-khung-loi]');
    const oTen = document.getElementById('tenTheLoai');
    const oMoTa = document.getElementById('moTaTheLoai');
    const nutLuu = document.getElementById('nutLuu');

    let danhSach = [];
    let maDangSua = null;   // null = đang thêm mới

    async function taiDanhSach() {
        try {
            danhSach = await api('/api/theloai');
            if (danhSach.length === 0) {
                bang.innerHTML = '<tr><td colspan="5" class="text-muted">Chưa có thể loại nào</td></tr>';
                return;
            }
            bang.innerHTML = danhSach.map(tl => `
                <tr>
                    <td><span class="ma">${e(tl.maTheLoai)}</span></td>
                    <td class="ten-chinh">${e(tl.tenTheLoai)}</td>
                    <td class="dong-phu">${e(tl.moTa)}</td>
                    <td class="text-end">${tl.soLuongSach}</td>
                    <td class="cot-hanh-dong">
                        <button type="button" class="btn btn-sm btn-outline-secondary" data-sua="${e(tl.maTheLoai)}">Sửa</button>
                        <button type="button" class="btn btn-sm btn-outline-danger" data-xoa="${e(tl.maTheLoai)}">Xóa</button>
                    </td>
                </tr>`).join('');
        } catch (loi) {
            bang.innerHTML = `<tr><td colspan="5" class="text-danger">${e(loi.message)}</td></tr>`;
        }
    }

    function moForm(theLoai) {
        maDangSua = theLoai ? theLoai.maTheLoai : null;
        document.getElementById('tieuDeModal').textContent = theLoai ? `Sửa thể loại ${theLoai.maTheLoai}` : 'Thêm thể loại';
        oTen.value = theLoai ? theLoai.tenTheLoai : '';
        oMoTa.value = theLoai ? (theLoai.moTa || '') : '';
        anLoi(khungLoi);
        modal.show();
    }

    modalEl.addEventListener('shown.bs.modal', () => oTen.focus());

    document.getElementById('nutThem').addEventListener('click', () => moForm(null));

    form.addEventListener('submit', async (su) => {
        su.preventDefault();
        anLoi(khungLoi);
        nutLuu.disabled = true;

        const duLieu = { tenTheLoai: oTen.value, moTa: oMoTa.value };
        try {
            if (maDangSua) {
                await api(`/api/theloai/${encodeURIComponent(maDangSua)}`, { method: 'PUT', body: duLieu });
                thongBao('Đã cập nhật thể loại');
            } else {
                const moi = await api('/api/theloai', { method: 'POST', body: duLieu });
                thongBao(`Đã thêm thể loại ${moi.maTheLoai}`);
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
            moForm(danhSach.find(tl => tl.maTheLoai === nutSua.dataset.sua));
        }

        if (nutXoa) {
            const theLoai = danhSach.find(tl => tl.maTheLoai === nutXoa.dataset.xoa);
            if (!confirm(`Xóa thể loại "${theLoai.tenTheLoai}"?`)) {
                return;
            }
            try {
                await api(`/api/theloai/${encodeURIComponent(theLoai.maTheLoai)}`, { method: 'DELETE' });
                thongBao('Đã xóa thể loại');
                await taiDanhSach();
            } catch (loi) {
                thongBao(loi.message, 'danger');     // vd 409: thể loại còn sách
            }
        }
    });

    taiDanhSach();
})();
