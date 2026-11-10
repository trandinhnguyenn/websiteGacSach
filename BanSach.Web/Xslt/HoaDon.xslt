<?xml version="1.0" encoding="utf-8"?>
<!--
  Hóa đơn bán hàng của MỘT đơn: biến đổi DonHang.xml, chọn đơn theo tham số $maDonHang.
  Tham số $sach (Sach.xml) và $khachHang (KhachHang.xml) để ghép tên sách, tên khách theo khóa ngoại.
  Quyền xem đã kiểm tra ở C# (khách chỉ in được đơn của mình) trước khi gọi biến đổi.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes" doctype-system="about:legacy-compat" />
  <xsl:decimal-format name="vn" decimal-separator="," grouping-separator="." />

  <xsl:param name="maDonHang" />
  <xsl:param name="sach" />
  <xsl:param name="khachHang" />
  <xsl:param name="tenWeb" select="'Nhà sách'" />
  <xsl:param name="khauHieu" select="''" />
  <!-- Đường dẫn CSS kèm ?v=<mã băm> (C# tính), sửa file CSS là trình duyệt tải bản mới -->
  <xsl:param name="duongCss" select="'/css/bao-cao.css'" />
  <xsl:param name="email" select="''" />
  <xsl:param name="soDienThoai" select="''" />
  <xsl:param name="ngayIn" select="''" />
  <xsl:param name="trangQuayLai" select="'/'" />

  <xsl:template match="/">
    <xsl:variable name="don" select="/DanhSachDonHang/DonHang[@MaDonHang = $maDonHang]" />
    <html lang="vi">
      <head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Hóa đơn <xsl:value-of select="$maDonHang" /> - <xsl:value-of select="$tenWeb" /></title>
        <link rel="stylesheet" href="{$duongCss}" />
      </head>
      <body>
        <div class="thanh-cong-cu khong-in">
          <a href="{$trangQuayLai}">← Quay lại</a>
          <button type="button" class="nut-in" onclick="window.print()">In hóa đơn</button>
        </div>
        <main class="to-giay">
          <xsl:choose>
            <xsl:when test="$don"><xsl:apply-templates select="$don" /></xsl:when>
            <xsl:otherwise><p>Không tìm thấy đơn hàng <xsl:value-of select="$maDonHang" />.</p></xsl:otherwise>
          </xsl:choose>
        </main>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="DonHang">
    <xsl:variable name="maKhach" select="@MaKhachHang" />
    <xsl:variable name="khach" select="$khachHang/DanhSachKhachHang/KhachHang[@MaKhachHang = $maKhach]" />

    <xsl:if test="TrangThai = 'DaHuy'">
      <div class="dau-huy">ĐÃ HỦY</div>
    </xsl:if>

    <header class="dau-bao-cao">
      <div>
        <p class="ten-hieu"><xsl:value-of select="$tenWeb" /></p>
        <p class="khau-hieu"><xsl:value-of select="$khauHieu" /></p>
      </div>
      <div class="dau-bao-cao__phai">
        <h1>Hóa đơn bán hàng</h1>
        <p class="phu">
          Số <strong><xsl:value-of select="@MaDonHang" /></strong>
          · Ngày đặt <xsl:call-template name="ngay-gio"><xsl:with-param name="giaTri" select="NgayDat" /></xsl:call-template>
        </p>
        <p class="phu">Trạng thái: <xsl:call-template name="ten-trang-thai"><xsl:with-param name="ma" select="TrangThai" /></xsl:call-template></p>
      </div>
    </header>

    <section class="hai-cot">
      <div>
        <h2>Bên bán</h2>
        <p><strong><xsl:value-of select="$tenWeb" /></strong></p>
        <p>Email: <xsl:value-of select="$email" /></p>
        <p>Điện thoại: <xsl:value-of select="$soDienThoai" /></p>
      </div>
      <div>
        <h2>Bên mua</h2>
        <p>
          <strong>
            <xsl:choose>
              <xsl:when test="$khach"><xsl:value-of select="$khach/HoTen" /></xsl:when>
              <xsl:otherwise>Khách <xsl:value-of select="$maKhach" /></xsl:otherwise>
            </xsl:choose>
          </strong>
          <span class="ma"> (<xsl:value-of select="$maKhach" />)</span>
        </p>
        <p>Người nhận: <xsl:value-of select="NguoiNhan" /> · <xsl:value-of select="SoDienThoai" /></p>
        <p>Giao đến: <xsl:value-of select="DiaChiGiao" /></p>
        <xsl:if test="GhiChu">
          <p>Ghi chú: <xsl:value-of select="GhiChu" /></p>
        </xsl:if>
      </div>
    </section>

    <!-- Màn hình hẹp (điện thoại): ẩn cột STT, Số lượng, Đơn giá (class an-hep), đưa "SL × đơn giá" xuống dưới tên sách
         (class chi-hep) để bảng vừa màn hình, khỏi vuốt ngang. Khi in vẫn đủ 5 cột. -->
    <div class="bang-cuon">
      <table class="bang-hoa-don">
        <thead>
          <tr>
            <th class="an-hep">STT</th>
            <th>Tên sách</th>
            <th class="so an-hep">Số lượng</th>
            <th class="so an-hep">Đơn giá</th>
            <th class="so">Thành tiền</th>
          </tr>
        </thead>
        <tbody>
          <xsl:for-each select="ChiTiet">
            <xsl:variable name="maSach" select="@MaSach" />
            <xsl:variable name="cuon" select="$sach/DanhSachSach/Sach[@MaSach = $maSach]" />
            <tr>
              <td class="ma an-hep"><xsl:value-of select="position()" /></td>
              <td>
                <span class="ten-sach">
                  <xsl:choose>
                    <xsl:when test="$cuon"><xsl:value-of select="$cuon/TenSach" /></xsl:when>
                    <xsl:otherwise>Sách đã ngừng bán</xsl:otherwise>
                  </xsl:choose>
                </span>
                <br />
                <span class="ma"><xsl:value-of select="$maSach" /><xsl:if test="$cuon"> · <xsl:value-of select="$cuon/TacGia" /></xsl:if></span>
                <span class="chi-hep"><xsl:value-of select="SoLuong" /> × <xsl:value-of select="format-number(DonGia, '#.##0', 'vn')" /> ₫</span>
              </td>
              <td class="so an-hep"><xsl:value-of select="SoLuong" /></td>
              <td class="so an-hep"><xsl:value-of select="format-number(DonGia, '#.##0', 'vn')" /></td>
              <td class="so"><xsl:value-of select="format-number(SoLuong * DonGia, '#.##0', 'vn')" /></td>
            </tr>
          </xsl:for-each>
        </tbody>
      </table>
    </div>

    <xsl:variable name="tongTien">
      <xsl:call-template name="tong-tien">
        <xsl:with-param name="dong" select="ChiTiet" />
      </xsl:call-template>
    </xsl:variable>
    <div class="tong-hoa-don">
      <div><span>Số cuốn</span><span><xsl:value-of select="sum(ChiTiet/SoLuong)" /></span></div>
      <div><span>Phí giao hàng</span><span>0 ₫</span></div>
      <div class="tong"><span>Tổng thanh toán</span><span><xsl:value-of select="format-number($tongTien, '#.##0', 'vn')" /> ₫</span></div>
    </div>
    <p class="ghi-chu">Hình thức thanh toán: tiền mặt khi nhận hàng (COD). Giá là giá tại thời điểm đặt hàng.</p>

    <section class="ky-ten">
      <div><p><strong>Người mua hàng</strong></p><p class="phu">(Ký, ghi rõ họ tên)</p></div>
      <div><p><strong>Người bán hàng</strong></p><p class="phu">(Ký, ghi rõ họ tên)</p></div>
    </section>

    <footer class="chan-bao-cao">
      <span>Sinh bằng XSLT (HoaDon.xslt) từ DonHang.xml</span>
      <span>In lúc <xsl:value-of select="$ngayIn" /></span>
    </footer>
  </xsl:template>

  <!-- 2026-10-24T15:50:00 -> 15:50 24/10/2026 -->
  <xsl:template name="ngay-gio">
    <xsl:param name="giaTri" />
    <xsl:value-of select="concat(substring($giaTri, 12, 5), ' ', substring($giaTri, 9, 2), '/', substring($giaTri, 6, 2), '/', substring($giaTri, 1, 4))" />
  </xsl:template>

  <xsl:template name="ten-trang-thai">
    <xsl:param name="ma" />
    <xsl:choose>
      <xsl:when test="$ma = 'ChoXacNhan'">Chờ xác nhận</xsl:when>
      <xsl:when test="$ma = 'DaXacNhan'">Đã xác nhận</xsl:when>
      <xsl:when test="$ma = 'DangGiao'">Đang giao</xsl:when>
      <xsl:when test="$ma = 'HoanThanh'">Hoàn thành</xsl:when>
      <xsl:when test="$ma = 'DaHuy'">Đã hủy</xsl:when>
      <xsl:otherwise><xsl:value-of select="$ma" /></xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="tong-tien">
    <xsl:param name="dong" />
    <xsl:variable name="n" select="count($dong)" />
    <xsl:choose>
      <xsl:when test="$n = 0">0</xsl:when>
      <xsl:when test="$n = 1"><xsl:value-of select="$dong/SoLuong * $dong/DonGia" /></xsl:when>
      <xsl:otherwise>
        <xsl:variable name="nua" select="floor($n div 2)" />
        <xsl:variable name="dau">
          <xsl:call-template name="tong-tien">
            <xsl:with-param name="dong" select="$dong[position() &lt;= $nua]" />
          </xsl:call-template>
        </xsl:variable>
        <xsl:variable name="sau">
          <xsl:call-template name="tong-tien">
            <xsl:with-param name="dong" select="$dong[position() &gt; $nua]" />
          </xsl:call-template>
        </xsl:variable>
        <xsl:value-of select="$dau + $sau" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
