// Dữ liệu XML (chỉ Admin): danh sách file GET /api/quantri/dulieu, tải GET /api/quantri/dulieu/tai/{file},
// nhập POST /api/quantri/dulieu/nhap/{file}?cheDo=&chiKiemTra= (luôn kiểm tra trước, bấm thêm lần nữa mới ghi)
(() => {
    'use strict';
    const { api, e, ngayGio, thongBao, hienLoi, anLoi } = BanSach;

    const bang = document.getElementById('bangFile');
    const modalEl = document.getElementById('modalNhap');
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById('formNhap');
    const khungLoi = form.querySelector('[data-khung-loi]');
    const oFile = document.getElementById('fileNhap');
    const khungKetQua = document.getElementById('ketQuaKiemTra');
    const nutKiemTra = document.getElementById('nutKiemTra');
    const nutNhap = document.getElementById('nutNhap');

    let fileDangNhap = null;    // tên file trong App_Data đang mở hộp nhập, vd Sach.xml

    const dungLuong = (b) => b < 1024 ? `${b} B` : `${(b / 1024).toLocaleString('vi-VN', { maximumFractionDigits: 1 })} KB`;
    const duongTai = (ten) => `/api/quantri/dulieu/tai/${encodeURIComponent(ten)}`;

    async function taiDanhSach() {
        try {
            const ds = await api('/api/quantri/dulieu');
            bang.innerHTML = ds.map(f => `
                <tr>
                    <td><span class="ten-chinh">${e(f.tenFile)}</span></td>
                    <td class="dong-phu">${e(f.noiDung)}${f.lyDoKhongNhap ? `<br><span class="small">${e(f.lyDoKhongNhap)}</span>` : ''}</td>
                    <td class="text-end">${f.soBanGhi}</td>
                    <td class="text-end text-nowrap d-none d-md-table-cell">${dungLuong(f.kichThuoc)}</td>
                    <td class="text-nowrap small d-none d-md-table-cell">${e(ngayGio(f.suaLuc))}</td>
                    <td class="cot-hanh-dong">
                        <div class="nut-tep">
                            ${f.taiDuoc ? `<a class="btn btn-sm btn-outline-secondary nut-tep__xml" href="${duongTai(f.tenFile)}" download aria-label="Tải ${e(f.tenFile)}">Tải XML</a>` : ''}
                            <a class="btn btn-sm btn-outline-secondary nut-tep__xsd" href="${duongTai(f.tenXsd)}" download aria-label="Tải ${e(f.tenXsd)}">Tải XSD</a>
                            ${f.nhapDuoc ? `<button type="button" class="btn btn-sm btn-primary nut-tep__nhap" data-nhap="${e(f.tenFile)}" aria-label="Nhập ${e(f.tenFile)}">Nhập</button>` : ''}
                        </div>
                    </td>
                </tr>`).join('');
        } catch (loi) {
            bang.innerHTML = `<tr><td colspan="6" class="text-danger">${e(loi.message)}</td></tr>`;
        }
    }

    function moHopNhap(tenFile) {
        fileDangNhap = tenFile;
        document.getElementById('tieuDeModalNhap').textContent = `Nhập ${tenFile}`;
        form.reset();
        datLaiKetQua();
        modal.show();
    }

    // Đổi file hoặc cách nhập thì kết quả kiểm tra cũ không còn đúng: phải kiểm tra lại mới được nhập
    function datLaiKetQua() {
        anLoi(khungLoi);
        khungKetQua.classList.add('d-none');
        khungKetQua.innerHTML = '';
        nutNhap.disabled = true;
    }

    const coThayDoi = (kq) => kq.themMoi + kq.capNhat + kq.xoa > 0;

    function veKetQua(kq) {
        const dong = (nhan, so, lop = '') => `<li><span>${nhan}</span><strong class="${lop}">${so}</strong></li>`;
        const canhBao = kq.canhBao.map(c => `<p class="ket-qua-nhap__canh-bao">${e(c)}</p>`).join('');
        const tieuDe = kq.daNhap ? 'Đã nhập xong'
            : coThayDoi(kq) ? 'File hợp lệ, sẵn sàng nhập' : 'File hợp lệ, không có gì mới';
        khungKetQua.innerHTML = `
            <p class="ket-qua-nhap__tieu-de">${tieuDe}: ${e(kq.tenFile)}</p>
            <ul class="ket-qua-nhap__so">
                ${dong('Bản ghi trong file', kq.soBanGhiTrongFile)}
                ${dong('Thêm mới', kq.themMoi)}
                ${dong('Cập nhật', kq.capNhat)}
                ${dong('Giữ nguyên', kq.giuNguyen)}
                ${kq.cheDo === 'thay' ? dong('Xóa', kq.xoa, kq.xoa > 0 ? 'text-danger' : '') : ''}
            </ul>
            ${canhBao}`;
        khungKetQua.classList.remove('d-none');
    }

    async function guiFile(chiKiemTra) {
        anLoi(khungLoi);
        if (!oFile.files.length) {
            hienLoi(khungLoi, { message: 'Chưa chọn file XML', chiTiet: [] });
            return null;
        }
        const duLieu = new FormData();
        duLieu.append('file', oFile.files[0]);
        const cheDo = form.querySelector('[name="cheDo"]:checked').value;
        const thamSo = new URLSearchParams({ cheDo, chiKiemTra });
        return api(`/api/quantri/dulieu/nhap/${encodeURIComponent(fileDangNhap)}?${thamSo}`, { method: 'POST', formData: duLieu });
    }

    form.addEventListener('submit', async (su) => {
        su.preventDefault();
        datLaiKetQua();
        nutKiemTra.disabled = true;
        try {
            const kq = await guiFile(true);
            if (kq) {
                veKetQua(kq);
                nutNhap.disabled = !coThayDoi(kq);
            }
        } catch (loi) {
            hienLoi(khungLoi, loi);     // lỗi XSD (kèm số dòng), sai loại file, hỏng khóa ngoại...
        } finally {
            nutKiemTra.disabled = false;
        }
    });

    nutNhap.addEventListener('click', async () => {
        const cheDo = form.querySelector('[name="cheDo"]:checked').value;
        if (cheDo === 'thay' && !confirm(`Thay toàn bộ ${fileDangNhap} bằng file này? Bản cũ được giữ dạng ${fileDangNhap}.bak.`)) {
            return;
        }
        nutNhap.disabled = true;
        nutKiemTra.disabled = true;
        try {
            const kq = await guiFile(false);
            if (kq) {
                veKetQua(kq);
                thongBao(kq.daNhap ? `Đã nhập ${kq.tenFile}` : 'Không có gì thay đổi', kq.daNhap ? 'success' : 'info');
                await taiDanhSach();
            }
        } catch (loi) {
            hienLoi(khungLoi, loi);
            nutNhap.disabled = false;
        } finally {
            nutKiemTra.disabled = false;
        }
    });

    oFile.addEventListener('change', datLaiKetQua);
    form.querySelectorAll('[name="cheDo"]').forEach(r => r.addEventListener('change', datLaiKetQua));

    bang.addEventListener('click', (su) => {
        const nut = su.target.closest('[data-nhap]');
        if (nut) {
            moHopNhap(nut.dataset.nhap);
        }
    });

    taiDanhSach();
})();
