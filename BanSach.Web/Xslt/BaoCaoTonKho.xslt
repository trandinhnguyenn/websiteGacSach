<?xml version="1.0" encoding="utf-8"?>
<!--
  Báo cáo tồn kho: biến đổi Sach.xml thành trang HTML in được.
  Đầu vào chính: Sach.xml. Tham số $theLoai: toàn bộ TheLoai.xml (C# truyền vào dạng XPathNavigator) để lấy tên thể loại.
  Kỹ thuật XSLT 1.0 dùng ở đây:
    - Nhóm theo thể loại bằng phương pháp Muenchian (xsl:key + generate-id)
    - Cộng dồn "giá bán × tồn kho" bằng template đệ quy chia đôi (XPath 1.0 sum() không nhân được từng dòng)
    - xsl:decimal-format để in số kiểu Việt Nam: 1.234.000
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes" doctype-system="about:legacy-compat" />
  <xsl:decimal-format name="vn" decimal-separator="," grouping-separator="." />

  <xsl:param name="theLoai" />
  <xsl:param name="tenWeb" select="'Nhà sách'" />
  <xsl:param name="khauHieu" select="''" />
  <!-- Đường dẫn CSS kèm ?v=<mã băm> (C# tính), sửa file CSS là trình duyệt tải bản mới -->
  <xsl:param name="duongCss" select="'/css/bao-cao.css'" />
  <xsl:param name="ngayLap" select="''" />
  <xsl:param name="nguongSapHet" select="5" />

  <!-- Gom sách theo mã thể loại -->
  <xsl:key name="sach-theo-the-loai" match="Sach" use="@MaTheLoai" />

  <!-- Biến toàn cục: tính với nút gốc của Sach.xml nên dùng được ở cả chỗ đang duyệt TheLoai.xml -->
  <xsl:variable name="tatCaSach" select="/DanhSachSach/Sach" />

  <xsl:template match="/">
    <html lang="vi">
      <head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Báo cáo tồn kho - <xsl:value-of select="$tenWeb" /></title>
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
              <h1>Báo cáo tồn kho</h1>
              <p class="phu">Lập lúc <xsl:value-of select="$ngayLap" /> · Nguồn: Sach.xml, TheLoai.xml</p>
            </div>
          </header>

          <xsl:apply-templates select="DanhSachSach" />

          <footer class="chan-bao-cao">
            <span>Sinh bằng XSLT (BaoCaoTonKho.xslt) từ dữ liệu XML của <xsl:value-of select="$tenWeb" /></span>
            <span>"Sắp hết": còn từ 1 đến <xsl:value-of select="$nguongSapHet" /> cuốn</span>
          </footer>
        </main>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="DanhSachSach">
    <xsl:variable name="giaTriKho">
      <xsl:call-template name="tong-gia-tri-ton">
        <xsl:with-param name="sach" select="Sach" />
      </xsl:call-template>
    </xsl:variable>

    <section class="so-lieu">
      <div><strong><xsl:value-of select="count(Sach)" /></strong><span>Đầu sách</span></div>
      <div><strong><xsl:value-of select="format-number(sum(Sach/SoLuongTon), '#.##0', 'vn')" /></strong><span>Cuốn đang trong kho</span></div>
      <div><strong><xsl:value-of select="format-number($giaTriKho, '#.##0', 'vn')" /> ₫</strong><span>Giá trị tồn kho (theo giá bán)</span></div>
      <div><strong class="het"><xsl:value-of select="count(Sach[SoLuongTon = 0])" /></strong><span>Đầu sách hết hàng</span></div>
      <div><strong class="sap-het"><xsl:value-of select="count(Sach[SoLuongTon &gt; 0 and SoLuongTon &lt;= $nguongSapHet])" /></strong><span>Đầu sách sắp hết</span></div>
    </section>

    <!-- Muenchian: chỉ lấy cuốn ĐẦU TIÊN của mỗi nhóm thể loại, rồi dùng key() lấy cả nhóm -->
    <xsl:for-each select="Sach[generate-id() = generate-id(key('sach-theo-the-loai', @MaTheLoai)[1])]">
      <xsl:sort select="@MaTheLoai" />
      <xsl:call-template name="nhom-the-loai">
        <xsl:with-param name="maTheLoai" select="@MaTheLoai" />
        <xsl:with-param name="sach" select="key('sach-theo-the-loai', @MaTheLoai)" />
      </xsl:call-template>
    </xsl:for-each>

    <!-- Thể loại có trong TheLoai.xml nhưng chưa có cuốn nào trong Sach.xml -->
    <xsl:variable name="theLoaiTrong" select="$theLoai/DanhSachTheLoai/TheLoai[not(@MaTheLoai = $tatCaSach/@MaTheLoai)]" />
    <xsl:if test="$theLoaiTrong">
      <p class="ghi-chu">
        <xsl:text>Thể loại chưa có sách: </xsl:text>
        <xsl:for-each select="$theLoaiTrong">
          <xsl:value-of select="TenTheLoai" />
          <xsl:if test="position() != last()">, </xsl:if>
        </xsl:for-each>
        <xsl:text>.</xsl:text>
      </p>
    </xsl:if>
  </xsl:template>

  <xsl:template name="nhom-the-loai">
    <xsl:param name="maTheLoai" />
    <xsl:param name="sach" />
    <xsl:variable name="tenTheLoai" select="$theLoai/DanhSachTheLoai/TheLoai[@MaTheLoai = $maTheLoai]/TenTheLoai" />
    <xsl:variable name="giaTriNhom">
      <xsl:call-template name="tong-gia-tri-ton">
        <xsl:with-param name="sach" select="$sach" />
      </xsl:call-template>
    </xsl:variable>

    <h2>
      <xsl:choose>
        <xsl:when test="$tenTheLoai"><xsl:value-of select="$tenTheLoai" /></xsl:when>
        <xsl:otherwise>Thể loại không xác định</xsl:otherwise>
      </xsl:choose>
      <span class="phu"><xsl:value-of select="$maTheLoai" /> · <xsl:value-of select="count($sach)" /> đầu sách</span>
    </h2>

    <div class="bang-cuon">
      <table>
        <thead>
          <tr>
            <th>Mã</th>
            <th>Tên sách</th>
            <th>Tác giả</th>
            <th class="so">Giá bán</th>
            <th class="so">Tồn kho</th>
            <th class="so">Giá trị tồn</th>
          </tr>
        </thead>
        <tbody>
          <!-- Cuốn ít hàng nhất lên đầu để thấy ngay cần nhập thêm gì -->
          <xsl:for-each select="$sach">
            <xsl:sort select="SoLuongTon" data-type="number" order="ascending" />
            <xsl:sort select="TenSach" />
            <tr>
              <td class="ma"><xsl:value-of select="@MaSach" /></td>
              <td class="ten-sach"><xsl:value-of select="TenSach" /></td>
              <td><xsl:value-of select="TacGia" /></td>
              <td class="so"><xsl:value-of select="format-number(GiaBan, '#.##0', 'vn')" /></td>
              <td class="so">
                <xsl:choose>
                  <xsl:when test="SoLuongTon = 0"><span class="het">Hết hàng</span></xsl:when>
                  <xsl:when test="SoLuongTon &lt;= $nguongSapHet"><span class="sap-het"><xsl:value-of select="SoLuongTon" /> (sắp hết)</span></xsl:when>
                  <xsl:otherwise><xsl:value-of select="SoLuongTon" /></xsl:otherwise>
                </xsl:choose>
              </td>
              <td class="so"><xsl:value-of select="format-number(GiaBan * SoLuongTon, '#.##0', 'vn')" /></td>
            </tr>
          </xsl:for-each>
        </tbody>
        <tfoot>
          <tr>
            <td colspan="4">Cộng</td>
            <td class="so"><xsl:value-of select="format-number(sum($sach/SoLuongTon), '#.##0', 'vn')" /></td>
            <td class="so"><xsl:value-of select="format-number($giaTriNhom, '#.##0', 'vn')" /> ₫</td>
          </tr>
        </tfoot>
      </table>
    </div>
  </xsl:template>

  <!--
    Tổng (GiaBan × SoLuongTon) của một tập Sach. Chia đôi tập rồi cộng hai nửa,
    nên độ sâu đệ quy chỉ khoảng log2(n), không tràn stack khi có nhiều sách.
  -->
  <xsl:template name="tong-gia-tri-ton">
    <xsl:param name="sach" />
    <xsl:variable name="n" select="count($sach)" />
    <xsl:choose>
      <xsl:when test="$n = 0">0</xsl:when>
      <xsl:when test="$n = 1"><xsl:value-of select="$sach/GiaBan * $sach/SoLuongTon" /></xsl:when>
      <xsl:otherwise>
        <xsl:variable name="nua" select="floor($n div 2)" />
        <xsl:variable name="dau">
          <xsl:call-template name="tong-gia-tri-ton">
            <xsl:with-param name="sach" select="$sach[position() &lt;= $nua]" />
          </xsl:call-template>
        </xsl:variable>
        <xsl:variable name="sau">
          <xsl:call-template name="tong-gia-tri-ton">
            <xsl:with-param name="sach" select="$sach[position() &gt; $nua]" />
          </xsl:call-template>
        </xsl:variable>
        <xsl:value-of select="$dau + $sau" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
