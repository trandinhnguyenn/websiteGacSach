// Quản lý đơn hàng (chỉ Admin): danh sách GET /api/quantri/donhang, chi tiết GET /api/quantri/donhang/{ma},
// chuyển trạng thái POST /api/quantri/donhang/{ma}/trangthai. Thứ tự các bước và việc trừ / hoàn kho do server quyết định.
(() => {
    'use strict';
    const { api, e, tien, ngayGio, anhBia, thongBao, hienLoi, anLoi, phanTrang, doiGoXong } = BanSach;

    const MAU_TRANG_THAI = { ChoXacNhan: 'vang', DaXacNhan: 'cham', DangGiao: 'cham', HoanThanh: 'den', DaHuy: 'do' };
    // Nhãn nút cho từng bước tiến tới (bước hủy đặt nhãn riêng vì còn tùy đơn đã trừ kho hay chưa)
    const NUT_BUOC = { DaXacNhan: 'Xác nhận đơn', DangGiao: 'Bắt đầu giao', HoanThanh: 'Đã giao xong' };
    const NUT_BUOC_NGAN = { DaXacNhan: 'Xác nhận', DangGiao: 'Giao hàng', HoanThanh: 'Hoàn thành' };

    const bang = document.getElementById('bangDonHang');
    const locTrangThai = document.getElementById('locTrangThai');
    const locTuKhoa = document.getElementById('locTuKhoa');
    const modalEl = document.getElementById('modalDon');
    const modal = new bootstrap.Modal(modalEl);
    const khungLoi = modalEl.querySelector('[data-khung-loi]');

    const thamSoDau = new URLSearchParams(location.search);
    const trangThai = {
        trangThai: thamSoDau.get('trangThai') || '',
        tuKhoa: thamSoDau.get('tuKhoa') || '',
        trang: Number(thamSoDau.get('trang')) || 1
    };
    locTuKhoa.value = trangThai.tuKhoa;

    let danhSach = [];
    let donDangMo = null;

    const nhan = (don) => `<span class="nhan nhan--${MAU_TRANG_THAI[don.trangThai] || ''}">${e(don.tenTrangThai)}</span>`;
    const buocTien = (don) => don.buocTiep.find(b => b !== 'DaHuy');
    const coTheHuy = (don) => don.buocTiep.includes('DaHuy');

    function ghiLenDiaChi() {
        const thamSo = new URLSearchParams();
        if (trangThai.trangThai) thamSo.set('trangThai', trangThai.trangThai);
        if (trangThai.tuKhoa) thamSo.set('tuKhoa', trangThai.tuKhoa);
        if (trangThai.trang > 1) thamSo.set('trang', trangThai.trang);
        const chuoi = thamSo.toString();
        history.replaceState(null, '', location.pathname + (chuoi ? '?' + chuoi : ''));
    }

    // ---------- Danh sách ----------
    function veLoc(dem) {
        const tong = dem.reduce((cong, d) => cong + d.soDon, 0);
        const muc = [{ trangThai: '', tenTrangThai: 'Tất cả', soDon: tong }, ...dem];
        locTrangThai.innerHTML = muc.map(m => `
            <button type="button" data-trang-thai="${e(m.trangThai)}" aria-pressed="${m.trangThai === trangThai.trangThai}">
                ${e(m.tenTrangThai)} <span class="dem-loc ${m.trangThai === 'ChoXacNhan' && m.soDon > 0 ? 'dem-loc--can-xu-ly' : ''}">${m.soDon}</span>
            </button>`).join('');
    }

    function dongBang(don) {
        const buoc = buocTien(don);
        const thieu = don.thieuHang.length > 0;
        const nutBuoc = buoc && !thieu
            ? `<button type="button" class="btn btn-sm btn-primary" data-buoc="${buoc}" data-ma="${e(don.maDonHang)}">${NUT_BUOC_NGAN[buoc]}</button>`
            : '';
        return `
            <tr>
                <td><span class="ma">${e(don.maDonHang)}</span></td>
                <td class="text-nowrap small">${e(ngayGio(don.ngayDat))}</td>
                <td>
                    <div class="ten-chinh">${e(don.tenKhachHang)}</div>
                    <div class="dong-phu">Nhận: ${e(don.nguoiNhan)} · ${e(don.soDienThoai)}</div>
                </td>
                <td class="text-end">${don.tongSoLuong}</td>
                <td class="text-end text-nowrap">${tien(don.tongTien)}</td>
                <td>${nhan(don)}${thieu ? ' <span class="nhan nhan--do" title="Kho không đủ để xác nhận">Thiếu hàng</span>' : ''}</td>
                <td class="cot-hanh-dong">
                    ${nutBuoc}
                    <button type="button" class="btn btn-sm btn-outline-secondary" data-xem="${e(don.maDonHang)}">Chi tiết</button>
                </td>
            </tr>`;
    }

    async function taiDanhSach() {
        ghiLenDiaChi();
        const thamSo = new URLSearchParams({
            trangThai: trangThai.trangThai,
            tuKhoa: trangThai.tuKhoa,
            trang: trangThai.trang,
            kichThuoc: 15
        });
        try {
            const ketQua = await api('/api/quantri/donhang?' + thamSo);
            danhSach = ketQua.duLieu;
            trangThai.trang = ketQua.trang;
            veLoc(ketQua.demTheoTrangThai);
            document.getElementById('tongSoDon').textContent = `${ketQua.tongSo} đơn`;
            phanTrang(document.getElementById('phanTrangDon'), ketQua, (trang) => {
                trangThai.trang = trang;
                taiDanhSach();
            });
            bang.innerHTML = danhSach.length
                ? danhSach.map(dongBang).join('')
                : '<tr><td colspan="7" class="text-muted">Không có đơn nào khớp điều kiện lọc</td></tr>';
        } catch (loi) {
            bang.innerHTML = `<tr><td colspan="7" class="text-danger">${e(loi.message)}</td></tr>`;
        }
    }

    // ---------- Chi tiết (modal) ----------
    function veModal(don) {
        donDangMo = don;
        document.getElementById('tieuDeModalDon').innerHTML = `Đơn ${e(don.maDonHang)} ${nhan(don)}`;

        const canhBaoThieu = don.thieuHang.length
            ? `<div class="alert alert-warning">
                   <strong>Kho không đủ để xác nhận đơn này.</strong> Nhập thêm hàng ở trang Sách hoặc hủy đơn.
                   <ul class="mb-0 mt-1">${don.thieuHang.map(t => `<li>${e(t)}</li>`).join('')}</ul>
               </div>`
            : '';

        document.getElementById('noiDungModalDon').innerHTML = `
            ${canhBaoThieu}
            <div class="row g-4">
                <div class="col-md-7">
                    <h3 class="tieu-de-nho">Sách trong đơn</h3>
                    ${don.chiTiet.map(ct => `
                        <div class="dong-tom-tat">
                            <img src="${anhBia(ct.hinhAnh)}" alt="">
                            <div>
                                <p class="dong-tom-tat__ten">${e(ct.tenSach)}</p>
                                <p class="dong-tom-tat__phu"><span class="ma">${e(ct.maSach)}</span> · ${ct.soLuong} × ${tien(ct.donGia)}</p>
                            </div>
                            <span>${tien(ct.thanhTien)}</span>
                        </div>`).join('')}
                    <div class="tom-tat__dong tom-tat__dong--tong"><span>Tổng cộng (${don.tongSoLuong} cuốn)</span><strong>${tien(don.tongTien)}</strong></div>
                </div>
                <div class="col-md-5">
                    <h3 class="tieu-de-nho">Khách hàng</h3>
                    <p class="mb-3">${e(don.tenKhachHang)} <span class="ma">${e(don.maKhachHang)}</span></p>
                    <h3 class="tieu-de-nho">Giao đến</h3>
                    <p class="mb-1 fw-semibold">${e(don.nguoiNhan)}</p>
                    <p class="mb-1"><a href="tel:${e(don.soDienThoai)}">${e(don.soDienThoai)}</a></p>
                    <p class="mb-3">${e(don.diaChiGiao)}</p>
                    ${don.ghiChu ? `<p class="small mb-3"><strong>Ghi chú của khách:</strong> ${e(don.ghiChu)}</p>` : ''}
                    <p class="small text-muted mb-0">Đặt lúc ${e(ngayGio(don.ngayDat))}</p>
                </div>
            </div>`;

        veChanModal(don);
    }

    function veChanModal(don) {
        const chan = document.getElementById('chanModalDon');
        const buoc = buocTien(don);
        const thieu = don.thieuHang.length > 0;

        let ghiChu = '';
        if (buoc === 'DaXacNhan') {
            ghiChu = thieu ? 'Chưa xác nhận được vì kho không đủ.' : `Xác nhận sẽ trừ ${don.tongSoLuong} cuốn khỏi kho.`;
        } else if (don.daTruKho) {
            ghiChu = `Hủy đơn sẽ cộng lại ${don.tongSoLuong} cuốn vào kho.`;
        } else if (!don.buocTiep.length) {
            ghiChu = don.trangThai === 'DaHuy' ? 'Đơn đã hủy, không đổi trạng thái được nữa.' : 'Đơn đã hoàn thành.';
        }

        chan.innerHTML = `
            <p class="chan-modal-don__ghi-chu">${e(ghiChu)}</p>
            <a class="btn btn-outline-secondary" href="/BaoCao/HoaDon/${encodeURIComponent(don.maDonHang)}" target="_blank" rel="noopener">In hóa đơn</a>
            ${coTheHuy(don) ? `<button type="button" class="btn btn-outline-danger" data-buoc-modal="DaHuy">${don.daTruKho ? 'Hủy đơn và hoàn kho' : 'Hủy đơn'}</button>` : ''}
            <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Đóng</button>
            ${buoc ? `<button type="button" class="btn btn-primary" data-buoc-modal="${buoc}" ${thieu ? 'disabled' : ''}>${NUT_BUOC[buoc]}</button>` : ''}`;
    }

    async function moChiTiet(ma) {
        anLoi(khungLoi);
        try {
            veModal(await api(`/api/quantri/donhang/${encodeURIComponent(ma)}`));
            modal.show();
        } catch (loi) {
            thongBao(loi.message, 'danger');
        }
    }

    // Gửi yêu cầu chuyển trạng thái; hủy đơn thì hỏi lại trước vì không hoàn tác được
    async function chuyenTrangThai(ma, trangThaiMoi, nut, tuModal) {
        const don = (donDangMo && donDangMo.maDonHang === ma) ? donDangMo : danhSach.find(d => d.maDonHang === ma);
        if (trangThaiMoi === 'DaHuy') {
            const hoanKho = don && don.daTruKho ? ` ${don.tongSoLuong} cuốn sẽ được cộng lại vào kho.` : '';
            if (!confirm(`Hủy đơn ${ma}?${hoanKho} Thao tác này không hoàn tác được.`)) {
                return;
            }
        }

        anLoi(khungLoi);
        nut.disabled = true;
        try {
            const capNhat = await api(`/api/quantri/donhang/${encodeURIComponent(ma)}/trangthai`, {
                method: 'POST',
                body: { trangThai: trangThaiMoi }
            });
            thongBao(`Đơn ${ma}: ${capNhat.tenTrangThai}`);
            if (tuModal) {
                veModal(capNhat);
            }
            await taiDanhSach();
        } catch (loi) {
            if (tuModal) {
                hienLoi(khungLoi, loi);     // vd 409 kho không đủ, kèm danh sách sách thiếu
            } else {
                thongBao(loi.chiTiet && loi.chiTiet.length ? `${loi.message}: ${loi.chiTiet.join('; ')}` : loi.message, 'danger');
            }
            nut.disabled = false;
            if (loi.maTrangThai === 409) {
                await taiDanhSach();        // trạng thái trên màn hình đã cũ (người khác vừa xử lý), tải lại
                if (tuModal) {
                    const moi = await api(`/api/quantri/donhang/${encodeURIComponent(ma)}`).catch(() => null);
                    if (moi) veModal(moi);
                }
            }
        }
    }

    // ---------- Sự kiện ----------
    locTrangThai.addEventListener('click', (su) => {
        const nut = su.target.closest('[data-trang-thai]');
        if (!nut) {
            return;
        }
        trangThai.trangThai = nut.dataset.trangThai;
        trangThai.trang = 1;
        locTrangThai.querySelectorAll('[aria-pressed]').forEach(b => b.setAttribute('aria-pressed', String(b === nut)));
        taiDanhSach();
    });

    locTuKhoa.addEventListener('input', doiGoXong(() => {
        trangThai.tuKhoa = locTuKhoa.value.trim();
        trangThai.trang = 1;
        taiDanhSach();
    }));

    bang.addEventListener('click', (su) => {
        const nutXem = su.target.closest('[data-xem]');
        const nutBuoc = su.target.closest('[data-buoc]');
        if (nutXem) {
            moChiTiet(nutXem.dataset.xem);
        } else if (nutBuoc) {
            chuyenTrangThai(nutBuoc.dataset.ma, nutBuoc.dataset.buoc, nutBuoc, false);
        }
    });

    document.getElementById('chanModalDon').addEventListener('click', (su) => {
        const nut = su.target.closest('[data-buoc-modal]');
        if (nut && donDangMo) {
            chuyenTrangThai(donDangMo.maDonHang, nut.dataset.buocModal, nut, true);
        }
    });

    modalEl.addEventListener('hidden.bs.modal', () => { donDangMo = null; });

    taiDanhSach();
})();
