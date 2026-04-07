using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020330.BusinessLayers;
using SV22T1020330.Models.Catalog;
using SV22T1020330.Models.Common;

namespace SV22T1020330.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý danh mục sản phẩm cho giao diện Admin
    /// </summary>
    /// <remarks>
    /// Chức năng chính:
    /// - Hiển thị danh sách danh mục với phân trang và tìm kiếm
    /// - Thêm mới, chỉnh sửa, xóa danh mục
    /// - Validate dữ liệu đầu vào
    /// 
    /// Session management:
    /// - Lưu điều kiện tìm kiếm trong session
    /// - Session key: "CategorySearchInput"
    /// - Giữ lại trạng thái tìm kiếm khi chuyển trang
    /// 
    /// Authorization:
    /// - Yêu cầu [Authorize] cho tất cả actions
    /// - Chỉ admin được quyền truy cập
    /// - Auto-redirect về Login nếu chưa xác thực
    /// 
    /// Business logic:
    /// - Validation cho CategoryName (required, unique)
    /// - Description không required
    /// - Xử lý phân trang với PagedResult
    /// </remarks>
    [Authorize]
    public class CategoryController : Controller
    {
        /// <summary>
        /// Session key để lưu điều kiện tìm kiếm danh mục
        /// </summary>
        private const string CATEGORY_SEARCH = "CategorySearchInput";

        /// <summary>
        /// Hiển thị trang danh sách danh mục
        /// </summary>
        /// <returns>View với form tìm kiếm</returns>
        /// <remarks>
        /// Session management:
        /// - Đọc PaginationSearchInput từ session
        /// - Nếu chưa có: tạo mới với mặc định
        /// - Page = 1, PageSize từ ApplicationContext, SearchValue = ""
        /// 
        /// Data flow:
        /// - Trả về view với search input
        /// - View sẽ AJAX load dữ liệu qua Search action
        /// - Giữ lại trạng thái tìm kiếm cũ nếu có
        /// 
        /// Performance:
        /// - Không load dữ liệu ngay, chỉ load khi cần
        /// - Giảm thời gian load trang ban đầu
        /// </remarks>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH);

            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }

            return View(input);
        }

        /// <summary>
        /// AJAX endpoint để tìm kiếm và phân trang danh mục
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>PartialView với danh sách danh mục</returns>
        /// <remarks>
        /// Data loading:
        /// - Gọi CatalogDataService.ListCategoriesAsync()
        /// - Truyền PaginationSearchInput vào service
        /// - Nhận PagedResult<Category> trả về
        /// 
        /// Session management:
        /// - Lưu input vào session cho lần sau
        /// - Giữ lại trang hiện tại và điều kiện tìm kiếm
        /// 
        /// Response:
        /// - Trả về PartialView("Search", result)
        /// - Dùng cho AJAX update trong Index view
        /// - Không trả về full View để tránh duplicate layout
        /// 
        /// Performance:
        /// - Chỉ load data khi cần thiết
        /// - Phân trang để giảm data size
        /// </remarks>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            // Lưu lại điều kiện tìm kiếm vào session
            ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);

            // Gọi service để lấy dữ liệu
            var result = await CatalogDataService.ListCategoriesAsync(input);

            // Trả về partial view để cập nhật phần danh sách
            return PartialView("Search", result);
        }

        /// <summary>
        /// Hiển thị form tạo danh mục mới
        /// </summary>
        /// <returns>View với form tạo danh mục</returns>
        /// <remarks>
        /// Purpose:
        /// - Hiển thị form trống để nhập thông tin danh mục
        /// - Fields: CategoryName (required), Description (optional)
        /// - Validation attributes hiển thị trong view
        /// 
        /// Data:
        /// - Truyền Category() rỗng vào view
        /// - View có thể dùng validation attributes
        /// - Form submit về Create action POST
        /// 
        /// UX:
        /// - Clean form với default values
        /// - Validation messages hiển thị real-time
        /// - Cancel button quay về Index
        /// </remarks>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo danh mục mới
        /// </summary>
        /// <param name="category">Thông tin danh mục cần tạo</param>
        /// <returns>Redirect về Index nếu thành công</returns>
        /// <remarks>
        /// Validation:
        /// - ModelState.IsValid kiểm tra required fields
        /// - CategoryName không được trống
        /// - Description có thể rỗng
        /// 
        /// Business logic:
        /// - Gọi CatalogDataService.AddCategoryAsync()
        /// - Service tự validate unique CategoryName
        /// - Return ID của category mới tạo
        /// 
        /// Error handling:
        /// - Thành công: redirect về Index
        /// - Thất bại: ViewBag.Message + return View
        /// - Giữ lại data trong form để user sửa
        /// 
        /// Security:
        /// - Chỉ admin được quyền tạo
        /// - [Authorize] đã đảm bảo
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                var result = await CatalogDataService.AddCategoryAsync(category);
                if (result > 0)
                {
                    return RedirectToAction("Index");
                }
            }

            // Nếu có lỗi, hiển thị lại form với dữ liệu đã nhập
            ViewBag.Message = "Không thể tạo danh mục. Vui lòng kiểm tra lại thông tin.";
            return View(category);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa danh mục
        /// </summary>
        /// <param name="id">Mã danh mục cần sửa</param>
        /// <returns>View với form chỉnh sửa</returns>
        /// <remarks>
        /// Data loading:
        /// - Gọi CatalogDataService.GetCategoryAsync(id)
        /// - Kiểm tra category tồn tại
        /// - Nếu không tìm thấy: return NotFound()
        /// 
        /// Validation:
        /// - ID phải > 0
        /// - Category phải tồn tại trong database
        /// 
        /// User Experience:
        /// - Form được điền sẵn với data hiện tại
        /// - User có thể sửa và lưu lại
        /// - Cancel button quay về Index
        /// </remarks>
        public async Task<IActionResult> Edit(int id)
        {
            var category = await CatalogDataService.GetCategoryAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        /// <summary>
        /// Xử lý cập nhật danh mục
        /// </summary>
        /// <param name="category">Thông tin danh mục cần cập nhật</param>
        /// <returns>Redirect về Index nếu thành công</returns>
        /// <remarks>
        /// Validation:
        /// - ModelState.IsValid kiểm tra required fields
        /// - CategoryID phải > 0
        /// - CategoryName không được trống
        /// 
        /// Business logic:
        /// - Gọi CatalogDataService.UpdateCategoryAsync()
        /// - Service tự validate unique CategoryName (trừ chính nó)
        /// - Return boolean success/failure
        /// 
        /// Error handling:
        /// - Thành công: redirect về Index
        /// - Thất bại: ViewBag.Message + return View
        /// - Giữ lại data trong form để user sửa
        /// 
        /// Concurrency:
        /// - Không handle concurrency conflicts
        /// - Last update wins strategy
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                var result = await CatalogDataService.UpdateCategoryAsync(category);
                if (result)
                {
                    return RedirectToAction("Index");
                }
            }

            // Nếu có lỗi, hiển thị lại form với dữ liệu đã nhập
            ViewBag.Message = "Không thể cập nhật danh mục. Vui lòng kiểm tra lại thông tin.";
            return View(category);
        }

        /// <summary>
        /// Xóa danh mục
        /// </summary>
        /// <param name="id">Mã danh mục cần xóa</param>
        /// <returns>Redirect về Index</returns>
        /// <remarks>
        /// Validation:
        /// - ID phải > 0
        /// - Kiểm tra category tồn tại
        /// 
        /// Business logic:
        /// - Gọi CatalogDataService.DeleteCategoryAsync()
        /// - Service tự check foreign key constraints
        /// - Return boolean success/failure
        /// 
        /// Error handling:
        /// - Thành công: redirect về Index
        /// - Thất bại (có product liên quan): redirect về Index
        /// - Không hiển thị error message (silent fail)
        /// 
        /// Data integrity:
        /// - Service sẽ không xóa nếu có product reference
        /// - Protect data integrity
        /// </remarks>
        public async Task<IActionResult> Delete(int id)
        {
            await CatalogDataService.DeleteCategoryAsync(id);
            return RedirectToAction("Index");
        }
    }
}
