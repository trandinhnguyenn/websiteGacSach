<?xml version="1.0" encoding="utf-8"?>
<!--
  Báo cáo doanh thu: biến đổi DonHang.xml thành trang HTML.
  Đầu vào chính: DonHang.xml. Tham số $sach: toàn bộ Sach.xml để lấy tên sách.
  Chỉ đơn "HoanThanh" mới tính vào doanh thu.
  Kỹ thuật XSLT 1.0:
    - Hai nhóm Muenchian: đơn hoàn thành theo tháng (substring(NgayDat, 1, 7)) và dòng chi tiết theo mã sách
    - xsl:sort theo một biểu thức tính được (tổng số cuốn đã bán của mỗi sách)
    - Template đệ quy chia đôi để cộng "SoLuong × DonGia"
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes" doctype-system="about:legacy-compat" />
  <xsl:decimal-format name="vn" decimal-separator="," grouping-separator="." />

  <xsl:param name="sach" />
  <xsl:param name="tenWeb" select="'Nhà sách'" />
  <xsl:param name="khauHieu" select="''" />
  <!-- Đường dẫn CSS kèm ?v=<mã băm> (C# tính), sửa file CSS là trình duyệt tải bản mới -->
  <xsl:param name="duongCss" select="'/css/bao-cao.css'" />
  <xsl:param name="ngayLap" select="''" />
  <xsl:param name="soSachBanChay" select="10" />

  <xsl:key name="don-xong-theo-thang" match="DonHang[TrangThai = 'HoanThanh']" use="substring(NgayDat, 1, 7)" />
  <xsl:key name="dong-ban-theo-sach" match="DonHang[TrangThai = 'HoanThanh']/ChiTiet" use="@MaSach" />
  <xsl:key name="don-theo-trang-thai" match="DonHang" use="TrangThai" />

  <xsl:variable name="donXong" select="/DanhSachDonHang/DonHang[TrangThai = 'HoanThanh']" />

  <xsl:template match="/">
    <xsl:variable name="doanhThu">
      <xsl:call-template name="tong-tien">
        <xsl:with-param name="dong" select="$donXong/ChiTiet" />
      </xsl:call-template>
    </xsl:variable>

    <html lang="vi">
      <head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Báo cáo doanh thu - <xsl:value-of select="$tenWeb" /></title>
        <link rel="stylesheet" href="{$duongCss}" />
      </head>
      <body>
        <div class="thanh-cong-cu khong-in">
          <a href="/Admin/DuLieu">← Quay lại trang Dữ liệu XML</a>
          <button type="button" class="nut-in" onclick="window.print()">In báo cáo</button>
        </div>

        <main class="to-giay">
          <header class="dau-bao-cao">
            <div>
              <p class="ten-hieu"><xsl:value-of select="$tenWeb" /></p>
              <p class="khau-hieu"><xsl:value-of select="$khauHieu" /></p>
            </div>
            <div class="dau-bao-cao__phai">
              <h1>Báo cáo doanh thu</h1>
              <p class="phu">Lập lúc <xsl:value-of select="$ngayLap" /> · Nguồn: DonHang.xml, Sach.xml</p>
            </div>
          </header>

          <section class="so-lieu">
            <div><strong><xsl:value-of select="format-number($doanhThu, '#.##0', 'vn')" /> ₫</strong><span>Doanh thu (đơn hoàn thành)</span></div>
            <div><strong><xsl:value-of select="count($donXong)" /></strong><span>Đơn hoàn thành</span></div>
            <div><strong><xsl:value-of select="format-number(sum($donXong/ChiTiet/SoLuong), '#.##0', 'vn')" /></strong><span>Cuốn đã bán</span></div>
            <div>
              <strong>
                <xsl:choose>
                  <xsl:when test="count($donXong) &gt; 0"><xsl:value-of select="format-number($doanhThu div count($donXong), '#.##0', 'vn')" /> ₫</xsl:when>
                  <xsl:otherwise>0 ₫</xsl:otherwise>
                </xsl:choose>
              </strong>
              <span>Trung bình mỗi đơn</span>
            </div>
          </section>

          <h2>Theo tháng <span class="phu">chỉ tính đơn hoàn thành, tháng gần nhất trước</span></h2>
          <xsl:choose>
            <xsl:when test="$donXong">
              <div class="bang-cuon">
                <table>
                  <thead>
                    <tr>
                      <th>Tháng</th>
                      <th class="so">Số đơn</th>
                      <th class="so">Số cuốn</th>
                      <th class="so">Doanh thu</th>
                    </tr>
                  </thead>
                  <tbody>
                    <xsl:for-each select="$donXong[generate-id() = generate-id(key('don-xong-theo-thang', substring(NgayDat, 1, 7))[1])]">
                      <xsl:sort select="substring(NgayDat, 1, 7)" order="descending" />
                      <xsl:variable name="donTrongThang" select="key('don-xong-theo-thang', substring(NgayDat, 1, 7))" />
                      <tr>
                        <!-- 2026-10 -> 10/2026 -->
                        <td><xsl:value-of select="concat(substring(NgayDat, 6, 2), '/', substring(NgayDat, 1, 4))" /></td>
                        <td class="so"><xsl:value-of select="count($donTrongThang)" /></td>
                        <td class="so"><xsl:value-of select="sum($donTrongThang/ChiTiet/SoLuong)" /></td>
                        <td class="so">
                          <xsl:variable name="tienThang">
                            <xsl:call-template name="tong-tien">
                              <xsl:with-param name="dong" select="$donTrongThang/ChiTiet" />
                            </xsl:call-template>
                          </xsl:variable>
                          <xsl:value-of select="format-number($tienThang, '#.##0', 'vn')" /> ₫
                        </td>
                      </tr>
                    </xsl:for-each>
                  </tbody>
                  <tfoot>
                    <tr>
                      <td>Cộng</td>
                      <td class="so"><xsl:value-of select="count($donXong)" /></td>
                      <td class="so"><xsl:value-of select="sum($donXong/ChiTiet/SoLuong)" /></td>
                      <td class="so"><xsl:value-of select="format-number($doanhThu, '#.##0', 'vn')" /> ₫</td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </xsl:when>
            <xsl:otherwise>
              <p class="ghi-chu">Chưa có đơn nào hoàn thành.</p>
            </xsl:otherwise>
          </xsl:choose>

          <h2>Sách bán chạy <span class="phu">top <xsl:value-of select="$soSachBanChay" />, theo số cuốn đã bán</span></h2>
          <xsl:if test="$donXong">
            <div class="bang-cuon">
              <table>
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Tên sách</th>
                    <th class="so">Số cuốn</th>
                    <th class="so">Doanh thu</th>
                  </tr>
                </thead>
                <tbody>
                  <xsl:for-each select="$donXong/ChiTiet[generate-id() = generate-id(key('dong-ban-theo-sach', @MaSach)[1])]">
                    <xsl:sort select="sum(key('dong-ban-theo-sach', @MaSach)/SoLuong)" data-type="number" order="descending" />
                    <xsl:sort select="@MaSach" />
                    <xsl:if test="position() &lt;= $soSachBanChay">
                      <xsl:variable name="maSach" select="@MaSach" />
                      <xsl:variable name="tenSach" select="$sach/DanhSachSach/Sach[@MaSach = $maSach]/TenSach" />
                      <tr>
                        <td class="ma"><xsl:value-of select="position()" /></td>
                        <td class="ten-sach">
                          <xsl:choose>
                            <xsl:when test="$tenSach"><xsl:value-of select="$tenSach" /></xsl:when>
                            <xsl:otherwise>(sách đã xóa)</xsl:otherwise>
                          </xsl:choose>
                          <xsl:text> </xsl:text>
                          <span class="ma"><xsl:value-of select="$maSach" /></span>
                        </td>
                        <td class="so"><xsl:value-of select="sum(key('dong-ban-theo-sach', $maSach)/SoLuong)" /></td>
                        <td class="so">
                          <xsl:variable name="tienSach">
                            <xsl:call-template name="tong-tien">
                              <xsl:with-param name="dong" select="key('dong-ban-theo-sach', $maSach)" />
                            </xsl:call-template>
                          </xsl:variable>
                          <xsl:value-of select="format-number($tienSach, '#.##0', 'vn')" /> ₫
                        </td>
                      </tr>
                    </xsl:if>
                  </xsl:for-each>
                </tbody>
              </table>
            </div>
          </xsl:if>

          <h2>Theo trạng thái <span class="phu">tất cả đơn</span></h2>
          <div class="bang-cuon">
            <table>
              <thead>
                <tr>
                  <th>Trạng thái</th>
                  <th class="so">Số đơn</th>
                  <th class="so">Giá trị đơn</th>
                </tr>
              </thead>
              <tbody>
                <xsl:call-template name="dong-trang-thai"><xsl:with-param name="ma" select="'ChoXacNhan'" /><xsl:with-param name="ten" select="'Chờ xác nhận'" /></xsl:call-template>
                <xsl:call-template name="dong-trang-thai"><xsl:with-param name="ma" select="'DaXacNhan'" /><xsl:with-param name="ten" select="'Đã xác nhận'" /></xsl:call-template>
                <xsl:call-template name="dong-trang-thai"><xsl:with-param name="ma" select="'DangGiao'" /><xsl:with-param name="ten" select="'Đang giao'" /></xsl:call-template>
                <xsl:call-template name="dong-trang-thai"><xsl:with-param name="ma" select="'HoanThanh'" /><xsl:with-param name="ten" select="'Hoàn thành'" /></xsl:call-template>
                <xsl:call-template name="dong-trang-thai"><xsl:with-param name="ma" select="'DaHuy'" /><xsl:with-param name="ten" select="'Đã hủy'" /></xsl:call-template>
              </tbody>
              <tfoot>
                <tr>
                  <td>Tổng số đơn</td>
                  <td class="so"><xsl:value-of select="count(/DanhSachDonHang/DonHang)" /></td>
                  <td></td>
                </tr>
              </tfoot>
            </table>
          </div>

          <footer class="chan-bao-cao">
            <span>Sinh bằng XSLT (BaoCaoDoanhThu.xslt) từ dữ liệu XML của <xsl:value-of select="$tenWeb" /></span>
            <span>Giá tính theo đơn giá lúc khách đặt, lưu trong từng đơn</span>
          </footer>
        </main>
      </body>
    </html>
  </xsl:template>

  <xsl:template name="dong-trang-thai">
    <xsl:param name="ma" />
    <xsl:param name="ten" />
    <xsl:variable name="don" select="key('don-theo-trang-thai', $ma)" />
    <xsl:variable name="tien">
      <xsl:call-template name="tong-tien">
        <xsl:with-param name="dong" select="$don/ChiTiet" />
      </xsl:call-template>
    </xsl:variable>
    <tr>
      <td><span class="nhan"><xsl:value-of select="$ten" /></span></td>
      <td class="so"><xsl:value-of select="count($don)" /></td>
      <td class="so"><xsl:value-of select="format-number($tien, '#.##0', 'vn')" /> ₫</td>
    </tr>
  </xsl:template>

  <!-- Tổng (SoLuong × DonGia) của một tập dòng ChiTiet, đệ quy chia đôi -->
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
