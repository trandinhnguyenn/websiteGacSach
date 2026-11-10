// Trang chi tiết sách: GET /api/sach/{ma}, GET /api/theloai (tên thể loại), GET /api/sach/{ma}/lienquan
(() => {
    'use strict';
    const { api, e, tien, anhBia, theSach, themVaoGio, thongBao } = BanSach;

    const trang = document.getElementById('trangChiTiet');
    const ma = trang.dataset.ma;
    // Đăng nhập bằng admin thì trang không có khung mua (xem Views/Sach/ChiTiet.cshtml): 3 biến dưới là null
    const formMua = document.getElementById('formMua');
    const oSoLuong = document.getElementById('soLuongMua');
    const nutThem = document.getElementById('nutThemGio');
    let sach = null;

    function veTon(soLuongTon) {
        const o = document.getElementById('tonSach');
        o.className = 'chi-tiet__ton';
        if (soLuongTon === 0) {
            o.textContent = 'Hết hàng';
            o.classList.add('het');
        } else if (soLuongTon <= 5) {
            o.textContent = `Chỉ còn ${soLuongTon} cuốn`;
            o.classList.add('it');
        } else {
            o.textContent = `Còn ${soLuongTon} cuốn`;
        }
    }

    // Mô tả lưu trong XML là văn bản thuần, mỗi dòng là một đoạn; escape rồi mới bọc <p>
    function veGioiThieu(moTa) {
        const doan = (moTa || '').split(/\n+/).map(x => x.trim()).filter(Boolean);
        document.getElementById('gioiThieuSach').innerHTML = doan.length
            ? doan.map(d => `<p>${e(d)}</p>`).join('')
            : '<p class="text-muted">Sách này chưa có phần giới thiệu.</p>';
    }

    async function tai() {
        try {
            const [s, theLoai] = await Promise.all([
                api(`/api/sach/${encodeURIComponent(ma)}`),
                api('/api/theloai')
            ]);
            sach = s;
            const tl = theLoai.find(x => x.maTheLoai === s.maTheLoai);
            const tenTheLoai = tl ? tl.tenTheLoai : s.maTheLoai;

            // Tên web lấy từ thẻ meta application-name trong _Layout, không viết cứng ở đây
            const tenWeb = document.querySelector('meta[name="application-name"]')?.content;
            document.title = tenWeb ? `${s.tenSach} - ${tenWeb}` : s.tenSach;
            const lienKetTheLoai = document.getElementById('duongTheLoai');
            lienKetTheLoai.textContent = tenTheLoai;
            lienKetTheLoai.href = `/Sach?maTheLoai=${encodeURIComponent(s.maTheLoai)}`;

            const anh = document.getElementById('anhBiaChiTiet');
            anh.src = anhBia(s.hinhAnh);
            anh.alt = `Bìa sách ${s.tenSach}`;
            // Gáy sách 3D lấy dải bên trái của chính ảnh bìa
            document.querySelector('.sach-3d__gay').style.backgroundImage = `url("${anhBia(s.hinhAnh)}")`;
            document.getElementById('tenSach').textContent = s.tenSach;
            document.getElementById('tacGiaSach').textContent = s.tacGia;
            document.getElementById('giaSach').textContent = tien(s.giaBan);
            veTon(s.soLuongTon);
            veGioiThieu(s.moTa);

            document.getElementById('thongSoSach').innerHTML = `
                <dt>Tác giả</dt><dd>${e(s.tacGia)}</dd>
                <dt>Thể loại</dt><dd><a href="/Sach?maTheLoai=${encodeURIComponent(s.maTheLoai)}">${e(tenTheLoai)}</a></dd>
                <dt>Nhà xuất bản</dt><dd>${e(s.nhaXuatBan)}</dd>
                <dt>Năm xuất bản</dt><dd>${s.namXuatBan}</dd>
                <dt>Mã sách</dt><dd>${e(s.maSach)}</dd>`;

            if (formMua) {
                const hetHang = s.soLuongTon === 0;
                oSoLuong.max = Math.max(1, Math.min(99, s.soLuongTon));
                oSoLuong.disabled = hetHang;
                nutThem.disabled = hetHang;
                nutThem.textContent = hetHang ? 'Hết hàng' : 'Thêm vào giỏ';
                document.querySelectorAll('[data-buoc]').forEach(b => { b.disabled = hetHang; });
            }
            document.getElementById('chiTietSach').setAttribute('aria-busy', 'false');

            taiLienQuan();
        } catch (loi) {
            if (loi.maTrangThai === 404) {
                document.getElementById('chiTietSach').classList.add('d-none');
                document.querySelector('.duong-dan').classList.add('d-none');
                document.getElementById('khongTimThay').classList.remove('d-none');
            } else {
                thongBao(loi.message, 'danger');
            }
        }
    }

    async function taiLienQuan() {
        try {
            const ds = await api(`/api/sach/${encodeURIComponent(ma)}/lienquan?soLuong=8`);
            if (ds.length > 0) {
                document.getElementById('keLienQuan').innerHTML = ds.map(s => theSach(s)).join('');
                document.getElementById('khungLienQuan').classList.remove('d-none');
            }
        } catch {
            // không có sách liên quan cũng không sao
        }
    }

    // Nút + / − cạnh ô số lượng
    document.querySelectorAll('[data-buoc]').forEach(nut => nut.addEventListener('click', () => {
        const moi = Number(oSoLuong.value || 1) + Number(nut.dataset.buoc);
        oSoLuong.value = Math.min(Number(oSoLuong.max), Math.max(1, moi));
    }));

    formMua?.addEventListener('submit', async (su) => {
        su.preventDefault();
        if (!sach) {
            return;
        }
        nutThem.disabled = true;
        try {
            await themVaoGio(sach.maSach, Number(oSoLuong.value || 1));
            oSoLuong.value = 1;
        } catch (loi) {
            thongBao(loi.message, 'danger');
        } finally {
            nutThem.disabled = sach.soLuongTon === 0;
        }
    });

    tai();
})();
