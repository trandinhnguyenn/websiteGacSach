// Hàm dùng chung cho toàn site: gọi API, hiện thông báo, định dạng tiền, phân trang
const BanSach = (() => {
    'use strict';

    // Lỗi trả về từ API: { thongBao, chiTiet[] } + mã HTTP
    class LoiApi extends Error {
        constructor(thongBao, chiTiet, maTrangThai) {
            super(thongBao);
            this.chiTiet = chiTiet || [];
            this.maTrangThai = maTrangThai;
        }
    }

    function layCsrfToken() {
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.content : '';
    }

    // Gọi API: tự gắn antiforgery token cho POST/PUT/DELETE, tự đổi lỗi thành LoiApi.
    // tuyChon: { method, body (object -> JSON), formData (upload file), boQua401 }
    async function api(url, tuyChon = {}) {
        const method = tuyChon.method || 'GET';
        const headers = { 'Accept': 'application/json' };
        let noiDung;

        if (method !== 'GET') {
            headers['RequestVerificationToken'] = layCsrfToken();
        }
        if (tuyChon.formData) {
            noiDung = tuyChon.formData;     // trình duyệt tự đặt Content-Type multipart
        } else if (tuyChon.body !== undefined) {
            headers['Content-Type'] = 'application/json';
            noiDung = JSON.stringify(tuyChon.body);
        }

        let phanHoi;
        try {
            phanHoi = await fetch(url, { method, headers, body: noiDung, credentials: 'same-origin' });
        } catch {
            throw new LoiApi('Không kết nối được máy chủ', [], 0);
        }

        // Hết phiên đăng nhập: đưa về trang đăng nhập, xong quay lại trang đang đứng
        if (phanHoi.status === 401 && !tuyChon.boQua401) {
            location.href = '/TaiKhoan/DangNhap?returnUrl=' + encodeURIComponent(location.pathname + location.search);
            throw new LoiApi('Bạn cần đăng nhập', [], 401);
        }

        if (phanHoi.status === 204) {
            return null;
        }

        let duLieu = null;
        const chuoi = await phanHoi.text();
        if (chuoi) {
            try {
                duLieu = JSON.parse(chuoi);
            } catch {
                duLieu = null;
            }
        }

        if (!phanHoi.ok) {
            const thongBao = duLieu && duLieu.thongBao ? duLieu.thongBao : `Lỗi ${phanHoi.status}`;
            throw new LoiApi(thongBao, duLieu && duLieu.chiTiet ? duLieu.chiTiet : [], phanHoi.status);
        }
        return duLieu;
    }

    // Chống XSS: mọi dữ liệu đưa vào innerHTML đều phải đi qua hàm này.
    // Tên sách là "<script>...</script>" thì sẽ hiện ra đúng chữ đó, không chạy được.
    function e(giaTri) {
        return String(giaTri ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function tien(so) {
        return Number(so || 0).toLocaleString('vi-VN') + ' ₫';
    }

    function ngayGio(chuoi) {
        const d = new Date(chuoi);
        return isNaN(d) ? '' : d.toLocaleString('vi-VN', { hour12: false });
    }

    // Thông báo nhỏ góc màn hình. loai: success | danger | warning | info
    function thongBao(noiDung, loai = 'success') {
        const vung = document.getElementById('vungThongBao');
        if (!vung) {
            alert(noiDung);
            return;
        }
        const el = document.createElement('div');
        // Bootstrap 5.1: nền bg-*, chữ trắng; riêng nền vàng dùng chữ tối cho dễ đọc
        const chuToi = loai === 'warning';
        el.className = `toast align-items-center bg-${loai} ${chuToi ? 'text-dark' : 'text-white'} border-0`;
        el.setAttribute('role', 'alert');
        el.innerHTML = `<div class="d-flex"><div class="toast-body">${e(noiDung)}</div>
            <button type="button" class="btn-close ${chuToi ? '' : 'btn-close-white'} me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
        vung.appendChild(el);
        const toast = new bootstrap.Toast(el, { delay: loai === 'danger' ? 7000 : 3500 });
        el.addEventListener('hidden.bs.toast', () => el.remove());
        toast.show();
    }

    // Hiện lỗi API vào một khung alert (trong modal / form), kèm danh sách chi tiết nếu có
    function hienLoi(khung, loi) {
        const chiTiet = (loi.chiTiet || []).map(c => `<li>${e(c)}</li>`).join('');
        khung.innerHTML = `<strong>${e(loi.message)}</strong>${chiTiet ? `<ul class="mb-0 mt-1">${chiTiet}</ul>` : ''}`;
        khung.classList.remove('d-none');
    }

    function anLoi(khung) {
        khung.classList.add('d-none');
        khung.innerHTML = '';
    }

    // Vẽ thanh phân trang từ kết quả { trang, tongTrang }; bấm trang nào gọi chuyenTrang(so)
    function phanTrang(khung, ketQua, chuyenTrang) {
        if (!ketQua || ketQua.tongTrang <= 1) {
            khung.innerHTML = '';
            return;
        }
        const nut = (so, nhan, tat, dangChon) =>
            `<li class="page-item ${tat ? 'disabled' : ''} ${dangChon ? 'active' : ''}">
                <button type="button" class="page-link" data-trang="${so}">${nhan}</button></li>`;

        let html = nut(ketQua.trang - 1, '&laquo;', ketQua.trang === 1, false);
        for (let i = 1; i <= ketQua.tongTrang; i++) {
            html += nut(i, i, false, i === ketQua.trang);
        }
        html += nut(ketQua.trang + 1, '&raquo;', ketQua.trang === ketQua.tongTrang, false);
        khung.innerHTML = `<ul class="pagination pagination-sm mb-0">${html}</ul>`;

        khung.querySelectorAll('[data-trang]').forEach(b =>
            b.addEventListener('click', () => chuyenTrang(Number(b.dataset.trang))));
    }

    // Chờ người dùng ngừng gõ rồi mới gọi (tìm kiếm khi gõ)
    function doiGoXong(ham, msCho = 350) {
        let hen;
        return (...thamSo) => {
            clearTimeout(hen);
            hen = setTimeout(() => ham(...thamSo), msCho);
        };
    }

    // Đường dẫn ảnh bìa (thiếu ảnh thì dùng ảnh mặc định)
    function anhBia(tenFile, phienBan) {
        const ten = tenFile || 'no-image.png';
        return `/images/sach/${encodeURIComponent(ten)}${phienBan ? '?v=' + phienBan : ''}`;
    }

    function duongDanSach(maSach) {
        return `/Sach/ChiTiet/${encodeURIComponent(maSach)}`;
    }

    // Thẻ một cuốn sách kiểu Mendo: bìa đặt trên nền "studio" xám, bấm vào để xem chi tiết,
    // bên dưới là tên, tác giả, giá và nút Thêm vào giỏ (tuyChon.nutThem = false để ẩn nút)
    // Admin không mua hàng: thẻ sách hiện số tồn kho thay cho nút Thêm vào giỏ
    const laQuanTri = () => document.body.dataset.vaiTro === 'Admin';

    function theSach(s, tuyChon = {}) {
        const hetHang = s.soLuongTon === 0;
        let nut = '';
        if (laQuanTri()) {
            const lop = hetHang ? 'the-sach__kho--het' : (s.soLuongTon <= 5 ? 'the-sach__kho--it' : '');
            nut = `<span class="the-sach__kho ${lop}">Kho: ${s.soLuongTon}</span>`;
        } else if (tuyChon.nutThem !== false) {
            nut = `
                    <button type="button" class="nut-them" data-them="${e(s.maSach)}" ${hetHang ? 'disabled' : ''}>
                        ${hetHang ? 'Hết hàng' : 'Thêm vào giỏ'}
                    </button>`;
        }
        return `
            <article class="the-sach ${hetHang ? 'het-hang' : ''}">
                <a class="the-sach__lien-ket" href="${duongDanSach(s.maSach)}">
                    <div class="the-sach__san">
                        <div class="the-sach__bia">
                            <img src="${anhBia(s.hinhAnh)}" alt="Bìa sách ${e(s.tenSach)}" loading="lazy" decoding="async">
                        </div>
                    </div>
                    <h3 class="the-sach__ten">${e(s.tenSach)}</h3>
                </a>
                <p class="the-sach__tac-gia">${e(s.tacGia)}</p>
                <div class="the-sach__chan">
                    <span class="the-sach__gia">${tien(s.giaBan)}</span>${nut}
                </div>
            </article>`;
    }

    // Thẻ chờ tải: cùng hình dạng với thẻ sách thật nên trang không bị giật khi dữ liệu về
    function theChoTai(soLuong) {
        return Array.from({ length: soLuong }, () => `
            <div class="the-sach the-sach--cho" aria-hidden="true">
                <div class="the-sach__san"></div>
                <p class="the-sach__ten">Đang tải tên sách</p>
                <p class="the-sach__tac-gia">Tác giả</p>
            </div>`).join('');
    }

    // Ảnh bìa tải xong thì hiện dần; lỗi thì thay bằng ảnh mặc định.
    // Sự kiện load/error của ảnh không nổi bọt nên bắt ở pha capture.
    document.addEventListener('load', (su) => {
        if (su.target.tagName === 'IMG' && su.target.closest('.the-sach__bia')) {
            su.target.classList.add('da-tai');
        }
    }, true);
    document.addEventListener('error', (su) => {
        const anh = su.target;
        if (anh.tagName === 'IMG' && !anh.src.endsWith('no-image.png')) {
            anh.src = anhBia(null);
        }
    }, true);

    // =================================================================
    // Giỏ hàng: số lượng trên đầu trang + ngăn trượt bên phải
    // =================================================================
    let gioHienTai = null;

    function capNhatSoLuongGio(gio) {
        const so = document.getElementById('soLuongGio');
        if (so) {
            so.textContent = gio.tongSoLuong;
            so.classList.toggle('co-hang', gio.tongSoLuong > 0);
        }
    }

    // Sách hết hàng vẫn nằm trong giỏ (để khách thấy và tự bỏ) nhưng không tính vào số cuốn / tổng tiền,
    // và chặn nút Đặt hàng cho đến khi bỏ hết các dòng đó
    const LOI_HET_HANG = 'Bỏ các sách đã hết hàng khỏi giỏ để đặt hàng.';

    function coSachHetHang(gio) {
        return gio.dong.some(d => d.hetHang);
    }

    function soCuonDatDuoc(gio) {
        return gio.dong.filter(d => !d.hetHang).reduce((tong, d) => tong + d.soLuong, 0);
    }

    // Một dòng sách trong giỏ, dùng chung cho ngăn trượt và trang Giỏ hàng
    function dongGio(d) {
        return `
            <div class="dong-gio ${d.hetHang ? 'het-hang' : ''}" data-ma="${e(d.maSach)}">
                <a class="dong-gio__bia" href="${duongDanSach(d.maSach)}"><img src="${anhBia(d.hinhAnh)}" alt=""></a>
                <div class="dong-gio__thong-tin">
                    <a class="dong-gio__ten" href="${duongDanSach(d.maSach)}">${e(d.tenSach)}</a>
                    <p class="dong-gio__tac-gia">${e(d.tacGia)}</p>
                    ${d.hetHang
                        ? '<p class="dong-gio__het">Sách này đã hết hàng, hãy bỏ khỏi giỏ để đặt hàng.</p>'
                        : `<div class="o-so-luong" role="group" aria-label="Số lượng">
                            <button type="button" data-doi="-1" aria-label="Bớt một cuốn" ${d.soLuong <= 1 ? 'disabled' : ''}>−</button>
                            <span>${d.soLuong}</span>
                            <button type="button" data-doi="1" aria-label="Thêm một cuốn" ${d.soLuong >= d.soLuongTon ? 'disabled' : ''}>+</button>
                           </div>`}
                </div>
                <div class="dong-gio__phai">
                    <span class="dong-gio__tien">${tien(d.thanhTien)}</span>
                    <button type="button" class="nut-chu-toi" data-bo="1">Bỏ</button>
                </div>
            </div>`;
    }

    function veNganGio(gio) {
        const than = document.getElementById('noiDungNganGio');
        const chan = document.getElementById('chanNganGio');
        if (!than) {
            return;
        }

        const thongBao = gio.thongBao.map(t => `<p class="ngan-gio__luu-y">${e(t)}</p>`).join('');
        if (gio.dong.length === 0) {
            than.innerHTML = `${thongBao}<div class="ngan-gio__trong"><p>Giỏ hàng đang trống.</p><a class="btn btn-primary" href="/Sach">Xem sách</a></div>`;
            chan.innerHTML = '';
            return;
        }

        than.innerHTML = thongBao + gio.dong.map(dongGio).join('');
        const hetHang = coSachHetHang(gio);
        chan.innerHTML = `
            <div class="ngan-gio__tong"><span>Tổng cộng</span><strong>${tien(gio.tongTien)}</strong></div>
            ${hetHang
                ? `<p class="ngan-gio__luu-y">${LOI_HET_HANG}</p>
                   <button type="button" class="btn btn-primary btn-lg w-100" disabled>Đặt hàng</button>`
                : '<a class="btn btn-primary btn-lg w-100" href="/GioHang/ThanhToan">Đặt hàng</a>'}
            <a class="ngan-gio__xem" href="/GioHang">Xem giỏ hàng</a>`;
    }

    // Mọi nơi đổi giỏ đều gọi hàm này để đầu trang, ngăn trượt và trang Giỏ hàng cùng cập nhật
    function apDungGio(gio) {
        gioHienTai = gio;
        capNhatSoLuongGio(gio);
        veNganGio(gio);
        document.dispatchEvent(new CustomEvent('gio-hang:doi', { detail: gio }));
        return gio;
    }

    async function taiGio() {
        return apDungGio(await api('/api/giohang'));
    }

    async function themVaoGio(maSach, soLuong = 1) {
        const gio = apDungGio(await api('/api/giohang', { method: 'POST', body: { maSach, soLuong } }));
        moNganGio();
        return gio;
    }

    async function doiSoLuong(maSach, soLuong) {
        return apDungGio(await api('/api/giohang', { method: 'PUT', body: { maSach, soLuong } }));
    }

    async function boKhoiGio(maSach) {
        return apDungGio(await api(`/api/giohang/${encodeURIComponent(maSach)}`, { method: 'DELETE' }));
    }

    function moNganGio() {
        const ngan = document.getElementById('nganGio');
        if (ngan) {
            bootstrap.Offcanvas.getOrCreateInstance(ngan).show();
        }
    }

    // Bấm "Thêm vào giỏ" ở bất kỳ thẻ sách nào
    document.addEventListener('click', async (su) => {
        const nut = su.target.closest('[data-them]');
        if (!nut || nut.disabled) {
            return;
        }
        nut.disabled = true;
        try {
            await themVaoGio(nut.dataset.them, 1);
        } catch (loi) {
            thongBao(loi.message, 'danger');
        } finally {
            nut.disabled = false;
        }
    });

    // Nút +, −, Bỏ trong dòng giỏ hàng (ngăn trượt và trang Giỏ hàng)
    document.addEventListener('click', async (su) => {
        const nut = su.target.closest('.dong-gio [data-doi], .dong-gio [data-bo]');
        if (!nut) {
            return;
        }
        const dong = nut.closest('.dong-gio');
        const ma = dong.dataset.ma;
        const hienTai = gioHienTai ? gioHienTai.dong.find(d => d.maSach === ma) : null;
        try {
            if (nut.dataset.bo) {
                await boKhoiGio(ma);
            } else if (hienTai) {
                await doiSoLuong(ma, hienTai.soLuong + Number(nut.dataset.doi));
            }
        } catch (loi) {
            thongBao(loi.message, 'danger');
        }
    });

    // Quy tắc mật khẩu mới: bản sao 3 quy tắc đầu của ChinhSachMatKhau.cs để báo ngay khi gõ.
    // Danh sách mật khẩu phổ biến chỉ server kiểm tra; server luôn là nơi quyết định.
    function quyTacMatKhau(matKhau, tenDangNhap) {
        const ten = (tenDangNhap || '').trim().toLowerCase();
        return {
            doDai: matKhau.length >= 8 && matKhau.length <= 100,
            chuVaSo: /\p{L}/u.test(matKhau) && /\p{Nd}/u.test(matKhau),
            khongChuaTen: matKhau.length > 0 && (ten.length < 3 || !matKhau.toLowerCase().includes(ten))
        };
    }

    // Gắn danh sách quy tắc (ul chứa li[data-quy-tac]) vào ô mật khẩu: quy tắc nào đạt thì đánh dấu ngay khi gõ.
    // tuyChon.layTenDangNhap(): tên đăng nhập hiện tại; tuyChon.quyTacThem(matKhau): thêm quy tắc riêng của trang.
    // Trả về hàm kiểm tra lại, cho ra chữ của các quy tắc chưa đạt (dùng làm danh sách lỗi khi bấm gửi).
    function theoDoiMatKhau(oMatKhau, khungQuyTac, tuyChon = {}) {
        const capNhat = () => {
            const matKhau = oMatKhau.value;
            const ketQua = Object.assign(
                quyTacMatKhau(matKhau, tuyChon.layTenDangNhap ? tuyChon.layTenDangNhap() : ''),
                tuyChon.quyTacThem ? tuyChon.quyTacThem(matKhau) : {});
            const chuaDat = [];
            khungQuyTac.querySelectorAll('[data-quy-tac]').forEach((li) => {
                const dat = ketQua[li.dataset.quyTac] === true;
                li.classList.toggle('dat', dat);
                if (!dat) {
                    chuaDat.push(li.textContent.trim());
                }
            });
            return chuaDat;
        };
        oMatKhau.addEventListener('input', capNhat);
        return capNhat;
    }

    // Thanh nhắc đổi mật khẩu yếu: "Để sau" thì ẩn tới khi đóng trình duyệt (sessionStorage có thể bị chặn: bỏ qua)
    const KHOA_AN_NHAC = 'BanSach.anNhacMatKhau';
    function ganNhacMatKhau() {
        const thanh = document.getElementById('nhacMatKhau');
        if (!thanh) {
            return;
        }
        try {
            if (sessionStorage.getItem(KHOA_AN_NHAC) === '1') {
                thanh.remove();
                return;
            }
        } catch { /* không đọc được thì cứ hiện */ }
        thanh.querySelector('[data-an-nhac-mat-khau]').addEventListener('click', () => {
            try {
                sessionStorage.setItem(KHOA_AN_NHAC, '1');
            } catch { /* chỉ ẩn ở trang này */ }
            thanh.remove();
        });
    }

    // Nút đăng xuất trên thanh menu
    document.addEventListener('DOMContentLoaded', () => {
        ganNhacMatKhau();

        if (document.getElementById('soLuongGio')) {
            taiGio().catch(() => { });
        }

        const nutDangXuat = document.getElementById('nutDangXuat');
        if (nutDangXuat) {
            nutDangXuat.addEventListener('click', async () => {
                try {
                    await api('/api/auth/dangxuat', { method: 'POST' });
                } finally {
                    location.href = '/';
                }
            });
        }
    });

    return { api, e, tien, ngayGio, thongBao, hienLoi, anLoi, phanTrang, doiGoXong, anhBia, theSach, theChoTai, duongDanSach,
        coSachHetHang, soCuonDatDuoc, LOI_HET_HANG,
        taiGio, themVaoGio, dongGio, LoiApi, quyTacMatKhau, theoDoiMatKhau, laQuanTri };
})();
