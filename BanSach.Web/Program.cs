using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using BanSach.BLL;
using BanSach.DAL;
using BanSach.Web.Api;
using BanSach.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// ===== Tầng DAL / BLL: một thể hiện dùng chung cho cả ứng dụng =====
var thuMucDuLieu = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
builder.Services.AddSingleton(new XmlStore(thuMucDuLieu));

builder.Services.AddSingleton<TheLoaiDAL>();
builder.Services.AddSingleton<SachDAL>();
builder.Services.AddSingleton<KhachHangDAL>();
builder.Services.AddSingleton<TaiKhoanDAL>();
builder.Services.AddSingleton<DonHangDAL>();
builder.Services.AddSingleton<NhatKyDAL>();

builder.Services.AddSingleton<NhatKyBLL>();
builder.Services.AddSingleton<TheLoaiBLL>();
builder.Services.AddSingleton<SachBLL>();
builder.Services.AddSingleton<KhachHangBLL>();
builder.Services.AddSingleton<TaiKhoanBLL>();
builder.Services.AddSingleton<GioHangBLL>();
builder.Services.AddSingleton<DonHangBLL>();
builder.Services.AddSingleton<DuLieuBLL>();
// Báo cáo XSLT: file .xslt để trong thư mục Xslt cạnh App_Data
builder.Services.AddSingleton(sp => new BaoCaoBLL(
    sp.GetRequiredService<XmlStore>(),
    Path.Combine(builder.Environment.ContentRootPath, "Xslt")));

builder.Services.AddSingleton<AnhBiaService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ChongDoMatKhau>();

// ===== MVC (trang) + API =====
builder.Services.AddControllersWithViews(options =>
    {
        // Mọi request POST / PUT / DELETE phải kèm antiforgery token (chống CSRF)
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        options.Filters.Add(new AntiforgeryLoiFilter());
        // Không tự bắt buộc các string non-nullable, để BLL kiểm tra và báo lỗi tiếng Việt
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    // API trả XML khi client gửi header Accept: application/xml (mặc định vẫn là JSON)
    .AddXmlSerializerFormatters()
    .AddJsonOptions(options =>
    {
        // Giữ nguyên tiếng Việt trong JSON thay vì ả...
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context => LoiApi.TuModelState(context.ModelState);
    });

// XML trả về có thụt lề cho dễ đọc khi demo
builder.Services.Configure<MvcOptions>(options =>
{
    foreach (var formatter in options.OutputFormatters.OfType<XmlSerializerOutputFormatter>())
    {
        formatter.WriterSettings.Indent = true;
    }
});

// Giữ nguyên tiếng Việt có dấu trong HTML thay vì đổi thành &#x...;
builder.Services.Configure<WebEncoderOptions>(options =>
{
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

// ===== Bảo mật =====

// JavaScript gửi token qua header RequestVerificationToken (lấy từ thẻ <meta name="csrf-token"> trong layout)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "BanSach.Antiforgery";
});

// Đăng nhập bằng cookie: HttpOnly (JavaScript không đọc được), SameSite, Secure khi chạy https
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BanSach.DangNhap";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/TaiKhoan/DangNhap";
        options.AccessDeniedPath = "/TaiKhoan/KhongCoQuyen";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;

        // Mỗi request đối chiếu cookie với TaiKhoan.xml: đổi mật khẩu ở một nơi thì các phiên khác bị đăng xuất
        options.Events.OnValidatePrincipal = PhienDangNhap.KiemTraAsync;

        // Gọi API mà chưa đăng nhập / sai quyền thì trả 401 / 403 dạng JSON, không chuyển hướng sang trang HTML.
        // Bị đăng xuất vì mật khẩu vừa đổi ở nơi khác thì nói rõ lý do (trang đăng nhập nhận ?hetPhien=1).
        options.Events.OnRedirectToLogin = context =>
        {
            var hetPhien = PhienDangNhap.VuaHetPhien(context.HttpContext);
            if (LaApi(context.Request))
            {
                return LoiApi.GhiAsync(context.Response, StatusCodes.Status401Unauthorized, hetPhien
                    ? "Phiên đăng nhập đã hết hiệu lực, có thể do mật khẩu vừa được đổi. Hãy đăng nhập lại."
                    : "Bạn cần đăng nhập");
            }
            context.Response.Redirect(hetPhien ? context.RedirectUri + "&hetPhien=1" : context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (LaApi(context.Request))
            {
                return LoiApi.GhiAsync(context.Response, StatusCodes.Status403Forbidden, "Bạn không có quyền thực hiện thao tác này");
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// Giỏ hàng lưu trong session (bộ nhớ server); cookie phiên HttpOnly, chỉ chứa mã phiên
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "BanSach.Phien";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

// Chặn rộng: mỗi IP tối đa 30 lần gọi đăng nhập / phút (chống dò hàng loạt nhiều tài khoản).
// Khóa theo từng tài khoản khi sai nhiều lần nằm ở ChongDoMatKhau.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(ChinhSach.GioiHanDangNhap, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "khong-ro",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = (context, _) =>
        new ValueTask(LoiApi.GhiAsync(context.HttpContext.Response, StatusCodes.Status429TooManyRequests,
            "Máy này gửi quá nhiều yêu cầu đăng nhập. Đợi 1 phút rồi thử lại."));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Header bảo mật cho mọi response
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";   // trình duyệt không đoán sai kiểu file
    context.Response.Headers["X-Frame-Options"] = "DENY";             // không cho nhúng trang vào iframe (chống clickjacking)
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static bool LaApi(HttpRequest request) => request.Path.StartsWithSegments("/api");
