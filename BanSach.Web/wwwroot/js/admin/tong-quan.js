// Trang tổng quan quản trị: đếm nhanh qua các API danh sách + 5 dòng nhật ký mới nhất
(() => {
    'use strict';
    const { api, e, ngayGio, tien } = BanSach;

    async function tai() {
        try {
            const [sach, theLoai, khachHang, nhatKy, donHang] = await Promise.all([
                api('/api/sach?kichThuoc=50&sapXep=ton-it'),
                api('/api/theloai'),
                api('/api/khachhang'),
                api('/api/nhatky?kichThuoc=5'),
                api('/api/quantri/donhang?kichThuoc=1')
            ]);

            const soDon = Object.fromEntries(donHang.demTheoTrangThai.map(d => [d.trangThai, d.soDon]));
            document.getElementById('soDonCho').textContent = soDon.ChoXacNhan || 0;
            document.getElementById('soDonDangXuLy').textContent = (soDon.DaXacNhan || 0) + (soDon.DangGiao || 0);
            document.getElementById('soDonHoanThanh').textContent = soDon.HoanThanh || 0;
            // Doanh thu viết gọn (2,36 tr) cho vừa ô, số đầy đủ để ở title
            const doanhThu = document.getElementById('doanhThu');
            doanhThu.textContent = donHang.doanhThu >= 1e6
                ? (donHang.doanhThu / 1e6).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' tr'
                : tien(donHang.doanhThu);
            doanhThu.title = tien(donHang.doanhThu);

            document.getElementById('soSach').textContent = sach.tongSo;
            document.getElementById('soTheLoai').textContent = theLoai.length;
            document.getElementById('soKhachHang').textContent = khachHang.length;
            document.getElementById('soHetHang').textContent = sach.duLieu.filter(s => s.soLuongTon === 0).length;

            const bang = document.getElementById('bangNhatKyGanDay');
            bang.innerHTML = nhatKy.duLieu.length === 0
                ? '<tr><td colspan="3" class="text-muted">Chưa có thao tác nào</td></tr>'
                : nhatKy.duLieu.map(nk => `<tr>
                    <td class="text-nowrap small">${e(ngayGio(nk.thoiGian))}</td>
                    <td>${e(nk.tenDangNhap)}</td>
                    <td class="small">${e(nk.noiDung)}</td></tr>`).join('');
        } catch (loi) {
            BanSach.thongBao(loi.message, 'danger');
        }
    }

    tai();
})();
