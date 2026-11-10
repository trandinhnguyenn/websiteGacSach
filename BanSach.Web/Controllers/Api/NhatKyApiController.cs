using BanSach.BLL;
using BanSach.DTO;
using BanSach.Web.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanSach.Web.Controllers.Api
{
    [Route("api/nhatky")]
    [Authorize(Roles = VaiTro.Admin)]
    public class NhatKyApiController : ApiCoBanController
    {
        private readonly NhatKyBLL _nhatKyBll;

        public NhatKyApiController(NhatKyBLL nhatKyBll)
        {
            _nhatKyBll = nhatKyBll;
        }

        // GET /api/nhatky?doiTuong=&hanhDong=&trang=&kichThuoc=
        [HttpGet]
        public ActionResult<TrangKetQua<NhatKy>> LayDanhSach([FromQuery] BoLocNhatKy boLoc)
        {
            return _nhatKyBll.LayDanhSach(boLoc);
        }
    }
}
