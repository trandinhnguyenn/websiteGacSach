// Trang danh sách sách: lọc / sắp xếp / phân trang qua GET /api/sach, tìm nâng cao qua GET /api/sach/timkiem-xpath.
// Điều kiện lọc ghi lên địa chỉ trang (?maTheLoai=...&tuKhoa=...) để tải lại hoặc gửi link vẫn giữ nguyên.
(() => {
    'use strict';
    const { api, e, theSach, theChoTai, phanTrang, doiGoXong } = BanSach;
    const KICH_THUOC = 12;

    const luoi = document.getElementById('luoiSach');
    const dem = document.getElementById('demSach');
    const locTheLoai = document.getElementById('locTheLoai');
    const locTuKhoa = document.getElementById('locTuKhoa');
    const locGia = document.getElementById('locGia');
    const locSapXep = document.getElementById('locSapXep');
    const khungPhanTrang = document.getElementById('phanTrangSach');

    const thamSoDau = new URLSearchParams(location.search);
    const trangThai = {
        maTheLoai: thamSoDau.get('maTheLoai') || '',
        tuKhoa: thamSoDau.get('tuKhoa') || '',
        gia: thamSoDau.get('gia') || '',
        sapXep: thamSoDau.get('sapXep') || 'moi',
        trang: Number(thamSoDau.get('trang')) || 1
    };

    locTuKhoa.value = trangThai.tuKhoa;
    locGia.value = trangThai.gia;
    locSapXep.value = trangThai.sapXep;

    function ghiLenDiaChi() {
        const thamSo = new URLSearchParams();
        Object.entries(trangThai).forEach(([k, v]) => {
            if (v && !(k === 'trang' && v === 1) && !(k === 'sapXep' && v === 'moi')) {
                thamSo.set(k, v);
            }
        });
        const chuoi = thamSo.toString();
        history.replaceState(null, '', location.pathname + (chuoi ? '?' + chuoi : ''));
    }

    function veLuoi(danhSach, thongBaoTrong) {
        luoi.innerHTML = danhSach.length
            ? danhSach.map(s => theSach(s)).join('')
            : `<div class="trang-trong"><p>${e(thongBaoTrong)}</p></div>`;
    }

    async function taiDanhSach() {
        ghiLenDiaChi();
        luoi.innerHTML = theChoTai(8);

        const [giaTu, giaDen] = trangThai.gia.split('-');
        const thamSo = new URLSearchParams({
            tuKhoa: trangThai.tuKhoa,
            maTheLoai: trangThai.maTheLoai,
            sapXep: trangThai.sapXep,
            trang: trangThai.trang,
            kichThuoc: KICH_THUOC
        });
        if (giaTu) {
            thamSo.set('giaTu', giaTu);
        }
        if (giaDen) {
            thamSo.set('giaDen', giaDen);
        }

        try {
            const ketQua = await api('/api/sach?' + thamSo);
            trangThai.trang = ketQua.trang;
            dem.textContent = `${ketQua.tongSo} cuốn`;
            veLuoi(ketQua.duLieu, 'Không có cuốn nào khớp. Thử bỏ bớt điều kiện lọc.');
            phanTrang(khungPhanTrang, ketQua, (trang) => {
                trangThai.trang = trang;
                taiDanhSach();
                window.scrollTo({ top: 0, behavior: 'smooth' });
            });
        } catch (loi) {
            luoi.innerHTML = `<div class="trang-trong"><p class="text-danger">${e(loi.message)}</p></div>`;
        }
    }

    // ---------- Thể loại ----------
    function veTheLoai(theLoai) {
        const muc = [{ maTheLoai: '', tenTheLoai: 'Tất cả' }, ...theLoai.filter(tl => tl.soLuongSach > 0)];
        locTheLoai.innerHTML = muc.map(tl => `
            <button type="button" data-ma="${e(tl.maTheLoai)}" aria-pressed="${tl.maTheLoai === trangThai.maTheLoai}">
                ${e(tl.tenTheLoai)}
            </button>`).join('');

        document.getElementById('tnTheLoai').insertAdjacentHTML('beforeend',
            theLoai.map(tl => `<option value="${e(tl.maTheLoai)}">${e(tl.tenTheLoai)}</option>`).join(''));
    }

    locTheLoai.addEventListener('click', (su) => {
        const nut = su.target.closest('[data-ma]');
        if (!nut) {
            return;
        }
        locTheLoai.querySelectorAll('[aria-pressed]').forEach(b => b.setAttribute('aria-pressed', String(b === nut)));
        trangThai.maTheLoai = nut.dataset.ma;
        trangThai.trang = 1;
        taiDanhSach();
    });

    document.getElementById('formLoc').addEventListener('submit', (su) => su.preventDefault());
    locTuKhoa.addEventListener('input', doiGoXong(() => {
        trangThai.tuKhoa = locTuKhoa.value.trim();
        trangThai.trang = 1;
        taiDanhSach();
    }));
    locGia.addEventListener('change', () => {
        trangThai.gia = locGia.value;
        trangThai.trang = 1;
        taiDanhSach();
    });
    locSapXep.addEventListener('change', () => {
        trangThai.sapXep = locSapXep.value;
        trangThai.trang = 1;
        taiDanhSach();
    });

    // ---------- Tìm nâng cao bằng XPath ----------
    const nutTimNangCao = document.getElementById('nutTimNangCao');
    const khungTimNangCao = document.getElementById('khungTimNangCao');
    const loiTimNangCao = document.getElementById('loiTimNangCao');

    nutTimNangCao.addEventListener('click', () => {
        const mo = khungTimNangCao.hidden;
        khungTimNangCao.hidden = !mo;
        nutTimNangCao.setAttribute('aria-expanded', String(mo));
        if (mo) {
            document.getElementById('tnTenSach').focus();
        }
    });

    // Bảng translate() rất dài, rút gọn chuỗi trong nháy dài hơn 24 ký tự để biểu thức dễ đọc
    function rutGonBieuThuc(bieuThuc) {
        return bieuThuc.replace(/'([^']{24,})'/g, (_, chuoi) => `'${chuoi.slice(0, 8)}…'`);
    }

    document.getElementById('formTimNangCao').addEventListener('submit', async (su) => {
        su.preventDefault();
        BanSach.anLoi(loiTimNangCao);

        const giaTriSo = id => document.getElementById(id).value;
        const thamSo = new URLSearchParams();
        const them = (khoa, giaTri) => {
            if (giaTri !== '' && giaTri !== null) {
                thamSo.set(khoa, giaTri);
            }
        };
        them('tenSach', document.getElementById('tnTenSach').value.trim());
        them('tacGia', document.getElementById('tnTacGia').value.trim());
        them('nhaXuatBan', document.getElementById('tnNhaXuatBan').value.trim());
        them('maTheLoai', document.getElementById('tnTheLoai').value);
        them('giaTu', giaTriSo('tnGiaTu'));
        them('giaDen', giaTriSo('tnGiaDen'));
        them('namTu', giaTriSo('tnNamTu'));
        them('namDen', giaTriSo('tnNamDen'));
        if (document.getElementById('tnConHang').checked) {
            thamSo.set('chiConHang', 'true');
        }

        try {
            const ketQua = await api('/api/sach/timkiem-xpath?' + thamSo);
            document.getElementById('maXPath').textContent = rutGonBieuThuc(ketQua.bieuThuc);
            document.getElementById('soKetQuaXPath').textContent = `(${ketQua.soKetQua} kết quả)`;
            document.getElementById('bieuThucXPath').hidden = false;
            dem.textContent = `${ketQua.soKetQua} cuốn khớp tìm nâng cao`;
            khungPhanTrang.innerHTML = '';
            veLuoi(ketQua.duLieu, 'Không có cuốn nào thỏa mọi điều kiện.');
        } catch (loi) {
            // Xóa kết quả và biểu thức của lần tìm trước, để không ai tưởng đó là kết quả của điều kiện đang sai
            BanSach.hienLoi(loiTimNangCao, loi);
            document.getElementById('bieuThucXPath').hidden = true;
            dem.textContent = '';
            khungPhanTrang.innerHTML = '';
            veLuoi([], 'Sửa lại điều kiện tìm nâng cao rồi bấm tìm lại.');
        }
    });

    document.getElementById('nutBoTimNangCao').addEventListener('click', () => {
        document.getElementById('formTimNangCao').reset();
        document.getElementById('bieuThucXPath').hidden = true;
        BanSach.anLoi(loiTimNangCao);
        taiDanhSach();
    });

    // ---------- Bắt đầu ----------
    api('/api/theloai')
        .then(veTheLoai)
        .catch(() => { })
        .finally(taiDanhSach);
})();
