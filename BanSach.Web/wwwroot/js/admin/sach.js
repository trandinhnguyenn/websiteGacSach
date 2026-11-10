// Quản lý sách: lọc / phân trang qua GET /api/sach, thêm / sửa / xóa / đổi ảnh qua các API tương ứng
(() => {
    'use strict';
    const { api, e, tien, thongBao, hienLoi, anLoi, phanTrang, doiGoXong, anhBia } = BanSach;
    const DUNG_LUONG_ANH_TOI_DA = 2 * 1024 * 1024;

    const bang = document.getElementById('bangSach');
    const khungPhanTrang = document.getElementById('phanTrangSach');
    const tongSo = document.getElementById('tongSoSach');
    const locTuKhoa = document.getElementById('locTuKhoa');
    const locTheLoai = document.getElementById('locTheLoai');
    const locSapXep = document.getElementById('locSapXep');

    const modalEl = document.getElementById('modalSach');
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById('formSach');
    const khungLoi = form.querySelector('[data-khung-loi]');
    const nutLuu = document.getElementById('nutLuu');
    const fileAnh = document.getElementById('fileAnh');
    const xemTruocAnh = document.getElementById('xemTruocAnh');

    let tenTheLoai = {};        // MaTheLoai -> TenTheLoai
    let trangHienTai = 1;
    let danhSachTrang = [];
    let maDangSua = null;
    let phienBanAnh = '';       // thêm ?v=... sau khi đổi ảnh để trình duyệt không dùng ảnh cũ trong cache

    async function taiTheLoai() {
        const ds = await api('/api/theloai');
        tenTheLoai = Object.fromEntries(ds.map(tl => [tl.maTheLoai, tl.tenTheLoai]));
        const tuyChon = ds.map(tl => `<option value="${e(tl.maTheLoai)}">${e(tl.tenTheLoai)}</option>`).join('');
        locTheLoai.insertAdjacentHTML('beforeend', tuyChon);
        document.getElementById('maTheLoai').innerHTML = '<option value="">-- Chọn thể loại --</option>' + tuyChon;
    }

    async function taiDanhSach(trang = trangHienTai) {
        const thamSo = new URLSearchParams({
            tuKhoa: locTuKhoa.value,
            maTheLoai: locTheLoai.value,
            sapXep: locSapXep.value,
            trang,
            kichThuoc: 10
        });
        try {
            const ketQua = await api('/api/sach?' + thamSo);
            trangHienTai = ketQua.trang;
            danhSachTrang = ketQua.duLieu;
            tongSo.textContent = `${ketQua.tongSo} sách`;
            phanTrang(khungPhanTrang, ketQua, taiDanhSach);

            if (ketQua.duLieu.length === 0) {
                bang.innerHTML = '<tr><td colspan="7" class="text-muted">Không có sách phù hợp</td></tr>';
                return;
            }
            bang.innerHTML = ketQua.duLieu.map(s => `
                <tr>
                    <td><img src="${anhBia(s.hinhAnh, phienBanAnh)}" class="anh-bia-nho" alt="" loading="lazy"></td>
                    <td><span class="ma">${e(s.maSach)}</span></td>
                    <td>
                        <div class="ten-chinh">${e(s.tenSach)}</div>
                        <div class="dong-phu">${e(s.tacGia)}, ${e(s.nhaXuatBan)}, ${s.namXuatBan}</div>
                    </td>
                    <td class="dong-phu">${e(tenTheLoai[s.maTheLoai] || s.maTheLoai)}</td>
                    <td class="text-end text-nowrap">${tien(s.giaBan)}</td>
                    <td class="text-end">${veTonKho(s.soLuongTon)}</td>
                    <td class="cot-hanh-dong">
                        <button type="button" class="btn btn-sm btn-outline-secondary" data-sua="${e(s.maSach)}">Sửa</button>
                        <button type="button" class="btn btn-sm btn-outline-danger" data-xoa="${e(s.maSach)}">Xóa</button>
                    </td>
                </tr>`).join('');
        } catch (loi) {
            bang.innerHTML = `<tr><td colspan="7" class="text-danger">${e(loi.message)}</td></tr>`;
        }
    }

    function veTonKho(so) {
        if (so === 0) {
            return '<span class="so-ton-het">Hết</span>';
        }
        if (so <= 5) {
            return `<span class="so-ton-it">${so}</span>`;
        }
        return String(so);
    }

    function moForm(sach) {
        maDangSua = sach ? sach.maSach : null;
        document.getElementById('tieuDeModal').textContent = sach ? `Sửa sách ${sach.maSach}` : 'Thêm sách';
        document.getElementById('tenSach').value = sach ? sach.tenSach : '';
        document.getElementById('maTheLoai').value = sach ? sach.maTheLoai : (locTheLoai.value || '');
        document.getElementById('tacGia').value = sach ? sach.tacGia : '';
        document.getElementById('nhaXuatBan').value = sach ? sach.nhaXuatBan : '';
        document.getElementById('namXuatBan').value = sach ? sach.namXuatBan : new Date().getFullYear();
        document.getElementById('giaBan').value = sach ? sach.giaBan : '';
        document.getElementById('soLuongTon').value = sach ? sach.soLuongTon : 0;
        document.getElementById('moTaSach').value = sach ? (sach.moTa || '') : '';
        document.getElementById('namXuatBan').max = new Date().getFullYear() + 1;
        fileAnh.value = '';
        xemTruocAnh.src = anhBia(sach ? sach.hinhAnh : null, phienBanAnh);
        anLoi(khungLoi);
        modal.show();
    }

    // Số rỗng gửi lên là 0 để BLL báo lỗi tiếng Việt (gửi "" sẽ bị lỗi định dạng JSON)
    function laySo(id) {
        const giaTri = document.getElementById(id).value;
        return giaTri === '' ? 0 : Number(giaTri);
    }

    // Xem trước ảnh ngay khi chọn file; kiểm tra sơ bộ ở trình duyệt, server vẫn kiểm tra lại
    fileAnh.addEventListener('change', () => {
        anLoi(khungLoi);
        const file = fileAnh.files[0];
        if (!file) {
            return;
        }
        if (file.size > DUNG_LUONG_ANH_TOI_DA) {
            hienLoi(khungLoi, { message: 'Ảnh bìa tối đa 2MB', chiTiet: [] });
            fileAnh.value = '';
            return;
        }
        xemTruocAnh.src = URL.createObjectURL(file);
    });

    modalEl.addEventListener('shown.bs.modal', () => document.getElementById('tenSach').focus());

    form.addEventListener('submit', async (su) => {
        su.preventDefault();
        anLoi(khungLoi);
        nutLuu.disabled = true;

        const duLieu = {
            tenSach: document.getElementById('tenSach').value,
            maTheLoai: document.getElementById('maTheLoai').value,
            tacGia: document.getElementById('tacGia').value,
            nhaXuatBan: document.getElementById('nhaXuatBan').value,
            namXuatBan: laySo('namXuatBan'),
            giaBan: laySo('giaBan'),
            soLuongTon: laySo('soLuongTon'),
            moTa: document.getElementById('moTaSach').value
        };

        try {
            const sach = maDangSua
                ? await api(`/api/sach/${encodeURIComponent(maDangSua)}`, { method: 'PUT', body: duLieu })
                : await api('/api/sach', { method: 'POST', body: duLieu });

            // Có chọn ảnh thì upload sau khi đã có mã sách
            if (fileAnh.files[0]) {
                const goi = new FormData();
                goi.append('anh', fileAnh.files[0]);
                try {
                    await api(`/api/sach/${encodeURIComponent(sach.maSach)}/anh`, { method: 'POST', formData: goi });
                    phienBanAnh = Date.now();
                } catch (loiAnh) {
                    thongBao(`Đã lưu sách ${sach.maSach} nhưng ảnh bìa lỗi: ${loiAnh.message}`, 'warning');
                }
            }

            thongBao(maDangSua ? `Đã cập nhật sách ${sach.maSach}` : `Đã thêm sách ${sach.maSach}`);
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
            moForm(danhSachTrang.find(s => s.maSach === nutSua.dataset.sua));
        }

        if (nutXoa) {
            const sach = danhSachTrang.find(s => s.maSach === nutXoa.dataset.xoa);
            if (!confirm(`Xóa sách "${sach.tenSach}"?`)) {
                return;
            }
            try {
                await api(`/api/sach/${encodeURIComponent(sach.maSach)}`, { method: 'DELETE' });
                thongBao('Đã xóa sách');
                await taiDanhSach();
            } catch (loi) {
                thongBao(loi.message, 'danger');     // vd 409: sách đã có trong đơn hàng
            }
        }
    });

    document.getElementById('nutThem').addEventListener('click', () => moForm(null));
    locTuKhoa.addEventListener('input', doiGoXong(() => taiDanhSach(1)));
    locTheLoai.addEventListener('change', () => taiDanhSach(1));
    locSapXep.addEventListener('change', () => taiDanhSach(1));

    // /Admin/Sach?sua=S001 (nút "Sửa sách này" ở trang chi tiết sách khi đăng nhập admin): mở luôn form sửa cuốn đó
    async function moSuaTuDuongDan() {
        const ma = new URLSearchParams(location.search).get('sua');
        if (!ma) {
            return;
        }
        try {
            moForm(await api(`/api/sach/${encodeURIComponent(ma)}`));
        } catch (loi) {
            thongBao(loi.message, 'danger');
        }
    }

    taiTheLoai()
        .catch(loi => thongBao(loi.message, 'danger'))
        .finally(() => {
            taiDanhSach(1);
            moSuaTuDuongDan();     // sau khi có danh sách thể loại để ô chọn thể loại hiện đúng
        });
})();
