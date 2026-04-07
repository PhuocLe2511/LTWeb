using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020330.BusinessLayers;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.HR;
using SV22T1020330.DataLayers.SqlServer;
using SV22T1020330.Admin.Models;
using System.Threading.Tasks;
using System.Linq;

namespace SV22T1020330.Admin.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {


        private const string EMPLOYEE_SEARCH = "EmployeeSearchInput";

        /// <summary>
        /// Trang chính
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH);

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
            var result = await HRDataService.ListEmployeesAsync(input);

            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);

            return View(result);
        }
        /// <summary>
        /// Tạo mới một nhân viên
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch //(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xoá nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần xoá</param>
        /// <returns></returns>
        // GET: /Employee/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Kiểm tra có được xoá không
            ViewBag.CanDelete = !await HRDataService.IsUsedEmployeeAsync(id);

            return View(model);
        }

        // POST: /Employee/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, Employee model)
        {
            bool isUsed = await HRDataService.IsUsedEmployeeAsync(id);

            if (isUsed)
            {
                TempData["Error"] = "Không thể xóa nhân viên vì có dữ liệu liên quan.";
                return RedirectToAction("Delete", new { id });
            }

            await HRDataService.DeleteEmployeeAsync(id);

            TempData["Success"] = "Đã xóa nhân viên thành công.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return NotFound();
            
            return View(employee);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin");
                return View(employee);
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
                return View(employee);
            }

            try
            {
                var employeeRepo = new EmployeeAccountRepository(Configuration.ConnectionString);
                string hashedPassword = CryptHelper.HashMD5(newPassword);
                var result = await employeeRepo.ChangePasswordAsync(employee.Email, hashedPassword);
                
                if (result)
                {
                    TempData["Success"] = "Đổi mật khẩu thành công";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Đổi mật khẩu thất bại");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Tính năng đổi mật khẩu chưa khả dụng");
            }

            return View(employee);
        }

        /// <summary>
        /// Chỉnh vai trò (RoleNames trong bảng Employees).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return NotFound();

            ViewBag.Title = $"Vai trò — {employee.FullName}";
            var model = new EmployeeRoleEditViewModel
            {
                EmployeeID = employee.EmployeeID,
                EmployeeName = employee.FullName,
                Email = employee.Email,
                RoleNames = employee.RoleNames
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(EmployeeRoleEditViewModel model)
        {
            var employee = await HRDataService.GetEmployeeAsync(model.EmployeeID);
            if (employee == null)
                return NotFound();

            model.EmployeeName = employee.FullName;
            model.Email = employee.Email;
            ViewBag.Title = $"Vai trò — {employee.FullName}";

            try
            {
                var roleNames = string.IsNullOrWhiteSpace(model.RoleNames)
                    ? null
                    : model.RoleNames.Trim();

                var success = await HRDataService.UpdateEmployeeRoleNamesAsync(model.EmployeeID, roleNames);
                if (success)
                {
                    TempData["Success"] = $"Đã cập nhật vai trò cho nhân viên {employee.FullName}";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Không lưu được vai trò. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
            }

            return View(model);
        }
    }
}
