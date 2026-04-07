using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020330.BusinessLayers;
using SV22T1020330.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020330.Admin.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        /// <summary>
        /// Session lưu điều kiện tìm kiếm
        /// </summary>
        private const string SUPPLIER_SEARCH = "SupplierSearchInput";

        /// <summary>
        /// Hiển thị trang chính
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH);

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
        /// Tìm kiếm
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListSuppliersAsync(input);

            ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);

            return View(result);
        }


        /// <summary>
        /// Tạo mới một nhà cung cấp
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "...";
            return View("Edit");
        }
        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await PartnerDataService.GetSupplierAsync(id);
            if (supplier == null)
                return NotFound();

            ViewBag.Title = "Cập nhật nhà cung cấp";
            return View(supplier);
        }
        /// <summary>
        /// Xoá nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xoá</param>
        /// <returns></returns>
        public IActionResult Delete(int id)
        {
            return View();
        }
    }
}
