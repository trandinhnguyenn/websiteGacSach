// Trang đặt hàng: điền sẵn từ GET /api/taikhoan/hoso, gửi POST /api/donhang (giỏ lấy từ session ở server)
(() => {
    'use strict';
    const { api, e, tien, anhBia, taiGio, hienLoi, anLoi, thongBao, coSachHetHang, LOI_HET_HANG } = BanSach;

    const form = document.getElementById('formDatHang');
    const khungLoi = document.getElementById('loiDatHang');
    const nut = document.getElementById('nutDatHang');

    function veTomTat(gio) {
        if (gio.dong.length === 0) {
            document.getElementById('khungThanhToan').classList.add('d-none');
            document.getElementById('gioTrong').classList.remove('d-none');
            return;
        }
        document.getElementById('dsThanhToan').innerHTML = gio.dong.map(d => `
            <div class="dong-tom-tat ${d.hetHang ? 'het-hang' : ''}">
                <img src="${anhBia(d.hinhAnh)}" alt="">
                <div>
                    <p class="dong-tom-tat__ten">${e(d.tenSach)}</p>
                    <p class="dong-tom-tat__phu">${d.hetHang ? 'Hết hàng' : `${d.soLuong} × ${tien(d.donGia)}`}</p>
                </div>
                <span>${tien(d.thanhTien)}</span>
            </div>`).join('');
        document.getElementById('tongThanhToan').textContent = tien(gio.tongTien);

        // Có sách hết hàng thì không cho gửi đơn (server cũng chặn, đây chỉ để khách biết trước)
        const hetHang = coSachHetHang(gio);
        const ghiChu = document.getElementById('ghiChuHetHang');
        ghiChu.innerHTML = `${e(LOI_HET_HANG)} <a href="/GioHang">Sửa giỏ hàng</a>`;
        ghiChu.hidden = !hetHang;
        nut.disabled = hetHang;
    }

    document.addEventListener('gio-hang:doi', (su) => veTomTat(su.detail));

    async function batDau() {
        try {
            const [hoSo] = await Promise.all([api('/api/taikhoan/hoso'), taiGio()]);
            document.getElementById('nguoiNhan').value = hoSo.hoTen;
            document.getElementById('soDienThoai').value = hoSo.soDienThoai;
            document.getElementById('diaChiGiao').value = hoSo.diaChi;
        } catch (loi) {
            thongBao(loi.message, 'danger');
        }
    }

    form.addEventListener('submit', async (su) => {
        su.preventDefault();
        anLoi(khungLoi);
        nut.disabled = true;
        try {
            const donHang = await api('/api/donhang', {
                method: 'POST',
                body: {
                    nguoiNhan: document.getElementById('nguoiNhan').value,
                    soDienThoai: document.getElementById('soDienThoai').value,
                    diaChiGiao: document.getElementById('diaChiGiao').value,
                    ghiChu: document.getElementById('ghiChu').value
                }
            });
            location.href = `/DonHang/ChiTiet/${encodeURIComponent(donHang.maDonHang)}?moi=1`;
        } catch (loi) {
            hienLoi(khungLoi, loi);     // vd 409: sách trong giỏ không đủ hàng, kèm danh sách cuốn thiếu
            nut.disabled = false;
            taiGio().catch(() => { });
        }
    });

    batDau();
})();
