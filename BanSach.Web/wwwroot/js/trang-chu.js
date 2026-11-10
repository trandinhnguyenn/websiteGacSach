// Trang chủ: tường sách, kệ "Mới về", danh sách thể loại mở kệ tại chỗ. Dữ liệu lấy qua /api/sach và /api/theloai.
(() => {
    'use strict';
    const { api, e, theSach, theChoTai, anhBia, doiGoXong, thongBao } = BanSach;

    const tuong = document.getElementById('tuongSach');
    const keMoiVe = document.getElementById('keMoiVe');
    const dsTheLoai = document.getElementById('dsTheLoai');

    let tatCaSach = [];

    // ---------- Tường sách ----------
    function soCot() {
        const rong = window.innerWidth;
        return rong < 576 ? 3 : rong < 992 ? 4 : 6;
    }

    function dungTuong() {
        const coAnh = tatCaSach.filter(s => s.hinhAnh);
        if (coAnh.length === 0) {
            return;
        }

        const n = soCot();
        const SO_BIA_MOI_COT = 8;
        let html = '';
        for (let c = 0; c < n; c++) {
            const cot = [];
            for (let i = 0; i < SO_BIA_MOI_COT; i++) {
                cot.push(coAnh[(c * 3 + i * n + c) % coAnh.length]);
            }
            const anh = cot.map(s => `<img src="${anhBia(s.hinhAnh)}" alt="" decoding="async">`).join('');
            // Mỗi cột trôi với tốc độ và điểm bắt đầu khác nhau; nội dung nhân đôi để vòng lặp liền mạch
            html += `<div class="tuong-cot" style="--thoi-gian:${70 + (c * 13) % 40}s;--tre:-${(c * 17) % 60}s;--thu-tu:${c}">${anh}${anh}</div>`;
        }

        tuong.style.setProperty('--so-cot', n);
        tuong.dataset.soCot = n;
        tuong.innerHTML = html;
        requestAnimationFrame(() => requestAnimationFrame(() => tuong.classList.add('da-vao')));
    }

    window.addEventListener('resize', doiGoXong(() => {
        if (Number(tuong.dataset.soCot) !== soCot()) {
            dungTuong();
        }
    }, 250));

    // ---------- Kệ "Mới về" ----------
    function ganNutCuon() {
        const nutTrai = document.querySelector('[data-cuon="-1"]');
        const nutPhai = document.querySelector('[data-cuon="1"]');

        const capNhat = () => {
            nutTrai.disabled = keMoiVe.scrollLeft <= 4;
            nutPhai.disabled = keMoiVe.scrollLeft + keMoiVe.clientWidth >= keMoiVe.scrollWidth - 4;
        };

        // Bấm liên tiếp khi kệ còn đang trượt: cộng dồn vào đích cũ thay vì tính lại từ vị trí đang dở
        let dich = null;
        const giamChuyenDong = window.matchMedia('(prefers-reduced-motion: reduce)');

        [nutTrai, nutPhai].forEach(nut => nut.addEventListener('click', () => {
            const toiDa = keMoiVe.scrollWidth - keMoiVe.clientWidth;
            const goc = dich ?? keMoiVe.scrollLeft;
            dich = Math.min(toiDa, Math.max(0, goc + Number(nut.dataset.cuon) * keMoiVe.clientWidth * 0.8));
            keMoiVe.scrollTo({ left: dich, behavior: giamChuyenDong.matches ? 'auto' : 'smooth' });
        }));
        keMoiVe.addEventListener('scroll', doiGoXong(() => {
            dich = null;    // đã dừng trượt
            capNhat();
        }, 60));
        capNhat();
    }

    // ---------- Thể loại: bấm để mở kệ ngay bên dưới ----------
    function veTheLoai(theLoai) {
        dsTheLoai.innerHTML = theLoai.map(tl => `
            <li class="the-loai">
                <button type="button" class="the-loai__nut" aria-expanded="false" aria-controls="ngan-${e(tl.maTheLoai)}"
                        data-ma="${e(tl.maTheLoai)}" ${tl.soLuongSach === 0 ? 'disabled' : ''}>
                    <span class="the-loai__ten">${e(tl.tenTheLoai)}</span>
                    <span class="the-loai__so">${tl.soLuongSach === 0 ? 'Chưa có sách' : `${tl.soLuongSach} cuốn`}</span>
                </button>
                <div class="the-loai__ngan" id="ngan-${e(tl.maTheLoai)}" role="region">
                    <div class="the-loai__ngan-trong">
                        <div class="ke-sach"></div>
                        <a class="the-loai__xem" href="/Sach?maTheLoai=${encodeURIComponent(tl.maTheLoai)}">Xem tất cả sách ${e(tl.tenTheLoai)}</a>
                    </div>
                </div>
            </li>`).join('');
    }

    dsTheLoai.addEventListener('click', async (su) => {
        const nut = su.target.closest('.the-loai__nut');
        if (!nut) {
            return;
        }

        const ngan = document.getElementById(nut.getAttribute('aria-controls'));
        const dangMo = nut.getAttribute('aria-expanded') === 'true';

        // Chỉ mở một thể loại một lúc
        dsTheLoai.querySelectorAll('.the-loai__nut[aria-expanded="true"]').forEach(khac => {
            khac.setAttribute('aria-expanded', 'false');
            document.getElementById(khac.getAttribute('aria-controls')).classList.remove('mo');
        });
        if (dangMo) {
            return;
        }

        nut.setAttribute('aria-expanded', 'true');
        ngan.classList.add('mo');

        const ke = ngan.querySelector('.ke-sach');
        if (ke.dataset.daTai) {
            return;
        }
        ke.innerHTML = theChoTai(4);
        try {
            const ketQua = await api(`/api/sach?maTheLoai=${encodeURIComponent(nut.dataset.ma)}&sapXep=moi&kichThuoc=50`);
            ke.innerHTML = ketQua.duLieu.map(theSach).join('');
            ke.dataset.daTai = '1';
        } catch (loi) {
            ke.innerHTML = `<p class="text-danger">${e(loi.message)}</p>`;
        }
    });

    // ---------- Tải dữ liệu ----------
    async function tai() {
        keMoiVe.innerHTML = theChoTai(6);
        try {
            const [ketQuaSach, theLoai] = await Promise.all([
                api('/api/sach?sapXep=moi&kichThuoc=50'),
                api('/api/theloai')
            ]);
            tatCaSach = ketQuaSach.duLieu;

            dungTuong();

            keMoiVe.innerHTML = tatCaSach.slice(0, 10).map(theSach).join('');
            ganNutCuon();

            veTheLoai(theLoai);
        } catch (loi) {
            keMoiVe.innerHTML = `<p class="text-danger">${e(loi.message)}</p>`;
            thongBao(loi.message, 'danger');
        }
    }

    tai();
})();
