// Hiệu ứng chuyển trang (@view-transition trong site.css) bị trình duyệt bỏ giữa chừng - bấm chuyển trang liên tiếp,
// đổi cỡ cửa sổ, tab bị ẩn - thì các promise của nó bị reject "AbortError: Transition was skipped".
// Bắt lại cho console sạch; không ảnh hưởng gì tới trang.
// File này nạp trong <head> (không defer): sự kiện pagereveal của trang mới có thể chạy trước khi site.js ở cuối
// <body> kịp nạp, nên gắn bộ bắt lỗi ở site.js là quá muộn.
(() => {
    'use strict';
    ['pageswap', 'pagereveal'].forEach((tenSuKien) => {
        window.addEventListener(tenSuKien, (su) => {
            const vt = su.viewTransition;
            if (vt) {
                [vt.ready, vt.finished, vt.updateCallbackDone].forEach((hua) => hua && hua.catch(() => { }));
            }
        });
    });
})();
