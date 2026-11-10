// Trang Giỏ hàng: vẽ lại mỗi khi giỏ đổi (sự kiện 'gio-hang:doi' do site.js phát ra sau mọi thao tác)
(() => {
    'use strict';
    const { e, tien, dongGio, taiGio, thongBao, coSachHetHang, soCuonDatDuoc, LOI_HET_HANG } = BanSach;

    const ds = document.getElementById('dsGio');
    const tomTat = document.getElementById('tomTatGio');

    function ve(gio) {
        const luuY = gio.thongBao.map(t => `<p class="ngan-gio__luu-y">${e(t)}</p>`).join('');
        if (gio.dong.length === 0) {
            ds.innerHTML = `${luuY}<div class="trang-trong"><p>Giỏ hàng đang trống.</p><a class="btn btn-primary" href="/Sach">Xem sách</a></div>`;
            tomTat.hidden = true;
            return;
        }
        ds.innerHTML = luuY + gio.dong.map(dongGio).join('');
        // Số cuốn chỉ đếm sách còn hàng, khớp với tổng tiền
        document.getElementById('tongCuon').textContent = soCuonDatDuoc(gio);
        document.getElementById('tongTien').textContent = tien(gio.tongTien);

        // Có sách hết hàng thì khóa nút Đặt hàng (chỉ có với tài khoản khách) và nói rõ lý do
        const nutDatHang = document.getElementById('nutDatHang');
        const ghiChu = document.getElementById('ghiChuHetHang');
        if (nutDatHang) {
            const hetHang = coSachHetHang(gio);
            nutDatHang.classList.toggle('disabled', hetHang);
            nutDatHang.setAttribute('aria-disabled', String(hetHang));
            nutDatHang.tabIndex = hetHang ? -1 : 0;
            ghiChu.textContent = LOI_HET_HANG;
            ghiChu.hidden = !hetHang;
        }
        tomTat.hidden = false;
    }

    document.addEventListener('gio-hang:doi', (su) => ve(su.detail));

    // site.js đã tải giỏ lúc mở trang; tải thêm lần nữa phòng khi trang này mở trước sự kiện đó
    taiGio().catch(loi => thongBao(loi.message, 'danger'));
})();
