using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020330.BusinessLayers;
using SV22T1020330.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020330.Admin.Controllers
{
    [Authorize]
    public class ShipperController : Controller
    {
        /// <summary>
        /// Tên biến session lưu điều kiện tìm kiếm shipper
        /// </summary>
        private const string SHIPPER_SEARCH = "ShipperSearchInput";

        /// <summary>
        /// Hiển thị trang chính (chứa form search)
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH);

            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }

        /// <summary>
        /// Thực hiện tìm kiếm và trả về kết quả (Partial View)
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListShippersAsync(input);

            ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);

            return View(result);
        }
        /// <summary>
        /// Tạo mới một đơn vị vận chuyển
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm đơn vị vận chuyển";
            return View("Edit");
        }
        /// <summary>
        /// Cập nhật thông tin đơn vị vận chuyển
        /// </summary>
        /// <param name="id">Mã đơn vị vận chuyển cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            var shipper = await PartnerDataService.GetShipperAsync(id);
            if (shipper == null)
                return NotFound();

            ViewBag.Title = "Cập nhật đơn vị vận chuyển";
            return View(shipper);
        }
        /// <summary>
        /// Xoá đơn vị vận chuyển
        /// </summary>
        /// <param name="id">Mã đơn vị vận chuyển cần xoá</param>
        /// <returns></returns>
        public IActionResult Delete(int id)
        {
            return View();
        }
    }
}
