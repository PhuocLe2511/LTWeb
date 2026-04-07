using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020330.Admin;
using SV22T1020330.BusinessLayers;
using SV22T1020330.DataLayers.SQLServer;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020330.Admin.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        //private const int PAGE_SIZE = 10; //hard code
        
        /// <summary>
        /// Tên của biến dùng để lưu đk tìm kiếm khách hàng trong session
        /// </summary>
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm -> Tìm kiếm -> Hiển thị kết quả tìm kiếm
        /// </summary>
        /// <param name="page"></param>
        /// <param name="searchValue"></param>
        /// <returns></returns>


        public IActionResult Index()

            
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);
            if(input==null)
            input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = ""
            };

            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        { 
            var result = await PartnerDataService.ListCustomersAsync(input);

            ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);
            return View(result); 
        }

        /// <summary>
        /// Tao mới một khách hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";

            var model = new Customer()
            {
                CustomerID = 0
            };

            ViewBag.Provinces = await SelectListHelper.Provinces();

            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="id">Mã Khách hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
             
            ViewBag.Provinces = await SelectListHelper.Provinces();

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật khách hàng";
                //TODO: Kiểm tra tính hợp lệ của dữ liệu
                // sd ModelState để lưu các tình huống lỗi và thông báo lỗi cho người dùng (trên view)
                // Giả định: chỉ yc nhập tên kh, email vs tỉnh thành, các thông tin khác có thể để trống
                if (String.IsNullOrWhiteSpace(data.CustomerName))
                {
                    ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");
                }
                if (String.IsNullOrWhiteSpace(data.Email))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email không được để trống");
                }
                else if (!await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi khách hàng khác");
                }
                if (String.IsNullOrWhiteSpace(data.Province))
                {
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");
                }
                if (!ModelState.IsValid)
                {
                    ViewBag.Provinces = await SelectListHelper.Provinces();
                    return View("Edit", data);
                }

                // Tuỳ chọn : Hiệu chỉnh dữ liệu theo qui định của hệ thống (vd: chuẩn hoá tên khách hàng, chuẩn hoá email, v.v...)
                if (String.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";


                //TODO: Lưu dữ liệu vào DB
                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
                }
                return RedirectToAction("Index");

            }
            catch (Exception)
            {
                //ghi log lỗi dựa vào thông tin trong exception
                ModelState.AddModelError("Erorr", "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng thử lại sau.");
                return View("Edit", data);
            }
            
        }
                /// <summary>
                /// Xoá khách hàng
                /// </summary>
                /// <param name="id">Mã khách hàng cần xoá</param>
                /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            // Nếu method là POST thì thực hiện xoá, nếu là GET thì hiển thị form xác nhận xoá
            if (Request.Method=="POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }
            //GET - hiển thị form xác nhận xoá
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CanDelete = !await PartnerDataService.IsCustomerUsedAsync(id);// đổi thành IsUsed
            return View(model);
        }

        /// <summary>
        /// Đổi mật khẩu khách hàng (admin đặt mật khẩu mới, lưu MD5 như Shop).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin");
                return View(customer);
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
                return View(customer);
            }

            try
            {
                var accountRepo = new CustomerAccountRepository(Configuration.ConnectionString);
                string hashedPassword = CryptHelper.HashMD5(newPassword);
                var result = await accountRepo.ChangePasswordAsync(customer.Email, hashedPassword);

                if (result)
                {
                    TempData["Success"] = "Đổi mật khẩu khách hàng thành công";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Đổi mật khẩu thất bại");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Tính năng đổi mật khẩu chưa khả dụng");
            }

            return View(customer);
        }
    }
}
