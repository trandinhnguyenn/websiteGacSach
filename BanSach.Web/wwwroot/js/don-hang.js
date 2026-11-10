// Đơn hàng của tôi: danh sách (GET /api/donhang/cua-toi) và chi tiết (GET /api/donhang/{ma}, POST /api/donhang/{ma}/huy)
(() => {
    'use strict';
    const { api, e, tien, ngayGio, anhBia, duongDanSach, thongBao } = BanSach;

    const BUOC = [
        ['ChoXacNhan', 'Chờ xác nhận'],
        ['DaXacNhan', 'Đã xác nhận'],
        ['DangGiao', 'Đang giao'],
        ['HoanThanh', 'Hoàn thành']
    ];

    function nhanTrangThai(don) {
        const mau = { ChoXacNhan: 'vang', DaXacNhan: 'cham', DangGiao: 'cham', HoanThanh: 'den', DaHuy: 'do' }[don.trangThai] || '';
        return `<span class="nhan nhan--${mau}">${e(don.tenTrangThai)}</span>`;
    }

    // ---------- Danh sách ----------
    const dsDon = document.getElementById('dsDon');
    if (dsDon) {
        api('/api/donhang/cua-toi')
            .then(ds => {
                document.getElementById('demDon').textContent = ds.length ? `${ds.length} đơn` : '';
                if (ds.length === 0) {
                    dsDon.innerHTML = '<div class="trang-trong"><p>Bạn chưa có đơn hàng nào.</p><a class="btn btn-primary" href="/Sach">Xem sách</a></div>';
                    return;
                }
                dsDon.innerHTML = ds.map(don => `
                    <a class="the-don" href="/DonHang/ChiTiet/${encodeURIComponent(don.maDonHang)}">
                        <div class="the-don__bia">
                            ${don.chiTiet.slice(0, 3).map(ct => `<img src="${anhBia(ct.hinhAnh)}" alt="">`).join('')}
                        </div>
                        <div class="the-don__thong-tin">
                            <p class="the-don__ma">Đơn ${e(don.maDonHang)} ${nhanTrangThai(don)}</p>
                            <p class="the-don__phu">Đặt lúc ${e(ngayGio(don.ngayDat))}, ${don.tongSoLuong} cuốn</p>
                            <p class="the-don__phu">${don.chiTiet.map(ct => e(ct.tenSach)).join(', ')}</p>
                        </div>
                        <strong class="the-don__tien">${tien(don.tongTien)}</strong>
                    </a>`).join('');
            })
            .catch(loi => {
                dsDon.innerHTML = `<p class="text-danger">${e(loi.message)}</p>`;
            });
        return;
    }

    // ---------- Chi tiết ----------
    const trang = document.getElementById('trangDon');
    const ma = trang.dataset.ma;

    if (new URLSearchParams(location.search).get('moi') === '1') {
        document.getElementById('thongBaoDatXong').classList.remove('d-none');
    }

    function veTienTrinh(don) {
        const tt = document.getElementById('tienTrinh');
        if (don.trangThai === 'DaHuy') {
            tt.innerHTML = '<li class="tien-trinh__huy">Đơn này đã hủy.</li>';
            return;
        }
        const viTri = BUOC.findIndex(([maBuoc]) => maBuoc === don.trangThai);
        tt.innerHTML = BUOC.map(([, ten], i) =>
            `<li class="${i < viTri ? 'xong' : ''} ${i === viTri ? 'hien-tai' : ''}" ${i === viTri ? 'aria-current="step"' : ''}>${ten}</li>`
        ).join('');
    }

    function ve(don) {
        document.getElementById('tieuDeDon').innerHTML = `Đơn ${e(don.maDonHang)} ${nhanTrangThai(don)}`;
        document.getElementById('ngayDatDon').textContent = `Đặt lúc ${ngayGio(don.ngayDat)}`;
        veTienTrinh(don);

        document.getElementById('dsSachDon').innerHTML = don.chiTiet.map(ct => `
            <div class="dong-tom-tat dong-tom-tat--lon">
                <a href="${duongDanSach(ct.maSach)}"><img src="${anhBia(ct.hinhAnh)}" alt=""></a>
                <div>
                    <a class="dong-tom-tat__ten" href="${duongDanSach(ct.maSach)}">${e(ct.tenSach)}</a>
                    <p class="dong-tom-tat__phu">${e(ct.tacGia)}</p>
                    <p class="dong-tom-tat__phu">${ct.soLuong} × ${tien(ct.donGia)}</p>
                </div>
                <span>${tien(ct.thanhTien)}</span>
            </div>`).join('');

        document.getElementById('nguoiNhanDon').textContent = don.nguoiNhan;
        document.getElementById('sdtDon').textContent = don.soDienThoai;
        document.getElementById('diaChiDon').textContent = don.diaChiGiao;
        document.getElementById('ghiChuDon').textContent = don.ghiChu ? `Ghi chú: ${don.ghiChu}` : '';
        document.getElementById('tongDon').textContent = tien(don.tongTien);
        document.getElementById('nutHuyDon').classList.toggle('d-none', !don.coTheHuy);
        document.getElementById('noiDungDon').classList.remove('d-none');
    }

    document.getElementById('nutHuyDon').addEventListener('click', async (su) => {
        if (!confirm(`Hủy đơn ${ma}? Thao tác này không hoàn tác được.`)) {
            return;
        }
        su.target.disabled = true;
        try {
            ve(await api(`/api/donhang/${encodeURIComponent(ma)}/huy`, { method: 'POST' }));
            document.getElementById('thongBaoDatXong').classList.add('d-none');
            thongBao(`Đã hủy đơn ${ma}`);
        } catch (loi) {
            thongBao(loi.message, 'danger');
        } finally {
            su.target.disabled = false;
        }
    });

    api(`/api/donhang/${encodeURIComponent(ma)}`)
        .then(ve)
        .catch(loi => {
            if (loi.maTrangThai === 404) {
                document.getElementById('khongThayDon').classList.remove('d-none');
            } else {
                thongBao(loi.message, 'danger');
            }
        });
})();
