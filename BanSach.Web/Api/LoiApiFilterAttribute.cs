using BanSach.BLL;
using BanSach.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BanSach.Web.Api
{
    // Bắt exception trong API và đổi sang mã HTTP + LoiApi, controller không cần try/catch
    public class LoiApiFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case LoiNghiepVuException loi:
                    var maTrangThai = loi.Loai switch
                    {
                        LoaiLoi.KhongCoQuyen => StatusCodes.Status403Forbidden,
                        LoaiLoi.KhongTimThay => StatusCodes.Status404NotFound,
                        LoaiLoi.XungDot => StatusCodes.Status409Conflict,
                        _ => StatusCodes.Status400BadRequest
                    };
                    context.Result = new ObjectResult(new LoiApi(loi.Message, loi.ChiTiet)) { StatusCode = maTrangThai };
                    break;

                // BLL đã kiểm tra trước, lọt tới XSD nghĩa là dữ liệu sai ở chỗ BLL chưa lường hết: XSD là lớp chặn cuối
                case LoiXsdException loiXsd:
                    context.Result = new ObjectResult(new LoiApi(loiXsd.Message, loiXsd.ChiTiet)) { StatusCode = StatusCodes.Status400BadRequest };
                    break;

                // Lỗi không lường trước: ghi log đầy đủ, nhưng chỉ trả thông báo chung để không lộ chi tiết hệ thống
                default:
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<LoiApiFilterAttribute>>();
                    logger.LogError(context.Exception, "Lỗi không mong muốn ở {DuongDan}", context.HttpContext.Request.Path);
                    context.Result = new ObjectResult(new LoiApi("Có lỗi hệ thống, vui lòng thử lại sau")) { StatusCode = StatusCodes.Status500InternalServerError };
                    break;
            }

            context.ExceptionHandled = true;
        }
    }

    // Antiforgery token sai / thiếu: mặc định trả 400 rỗng, đổi thành thông báo dễ hiểu.
    // Order thấp để chạy trước filter mặc định của [ApiController] (filter đó đổi 400 sang ProblemDetails tiếng Anh).
    public class AntiforgeryLoiFilter : IAlwaysRunResultFilter, IOrderedFilter
    {
        public int Order => -3000;

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is IAntiforgeryValidationFailedResult)
            {
                context.Result = new ObjectResult(new LoiApi("Phiên làm việc không hợp lệ hoặc đã hết hạn, vui lòng tải lại trang (F5)"))
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
