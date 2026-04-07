using Microsoft.AspNetCore.Mvc;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Cart;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Sales;
using SV22T1020330.Models.Security;
using SV22T1020330.Shop.AppCodes;
using SV22T1020330.Shop.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020330.Models.DataDictionary;
using SV22T1020330.DataLayers.SqlServer;
using SV22T1020330.Shop;

namespace SV22T1020330.Shop.Controllers;

/// <summary>
/// Controller quản lý đơn hàng cho giao diện Shop
/// </summary>
/// <remarks>
/// Chức năng chính:
/// - Hiển thị danh sách đơn hàng của khách hàng
/// - Tạo đơn hàng từ giỏ hàng
/// - Chi tiết đơn hàng
/// - Trang xác nhận đặt hàng thành công
/// 
/// Authentication:
/// - Yêu cầu user đăng nhập cho tất cả operations
/// - Lấy CustomerID từ UserAccount session
/// - Redirect về Login nếu chưa đăng nhập
/// 
/// Data integration:
/// - Load provinces từ database cho dropdown
/// - Tự động xóa giỏ hàng sau khi đặt hàng thành công
/// - Validation cho địa chỉ và tỉnh thành
/// 
/// Session management:
/// - Giỏ hàng lưu trong session với key "Cart"
/// - User account lưu trong session với key "User"
/// - Xóa giỏ hàng sau khi tạo đơn thành công
/// </remarks>
public class OrderController : Controller
{
    private readonly IOrderRepository _orderRepo;
    private readonly SV22T1020330.DataLayers.Interfaces.IDataDictionaryRepository<SV22T1020330.Models.DataDictionary.Province> _provinceRepo;

    /// <summary>
    /// Khởi tạo OrderController với dependency injection
    /// </summary>
    /// <param name="orderRepo">Repository xử lý đơn hàng</param>
    /// <param name="provinceRepo">Repository xử lý tỉnh thành cho dropdown</param>
    public OrderController(IOrderRepository orderRepo, SV22T1020330.DataLayers.Interfaces.IDataDictionaryRepository<SV22T1020330.Models.DataDictionary.Province> provinceRepo)
    {
        _orderRepo = orderRepo;
        _provinceRepo = provinceRepo;
    }

    // ================= CART HELPERS =================

    /// <summary>
    /// Lấy giỏ hàng từ session
    /// </summary>
    /// <returns>Danh sách CartItem, null nếu không có</returns>
    /// <remarks>
    /// Session key: "Cart"
    /// - Nếu không có: trả về List rỗng
    /// - Nếu có: trả về danh sách hiện tại
    /// - Không tự động tạo mới như CartController
    /// </remarks>
    private List<CartItem> GetCart()
    {
        return HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
    }

    /// <summary>
    /// Lưu giỏ hàng vào session
    /// </summary>
    /// <param name="cart">Danh sách CartItem cần lưu</param>
    /// <remarks>
    /// Session key: "Cart"
    /// - Ghi đè toàn bộ giỏ hàng hiện tại
    /// - Dùng sau khi tạo đơn hàng thành công để xóa giỏ
    /// </remarks>
    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetObject("Cart", cart);
    }

    // ================= ORDER MANAGEMENT =================

    /// <summary>
    /// Hiển thị danh sách đơn hàng của khách hàng đã đăng nhập
    /// </summary>
    /// <param name="page">Trang hiện tại (mặc định = 1)</param>
    /// <returns>View với danh sách đơn hàng của user</returns>
    /// <remarks>
    /// Authentication:
    /// - Kiểm tra user đăng nhập trong session
    /// - Nếu chưa đăng nhập: redirect về Login
    /// 
    /// Authorization:
    /// - Chỉ hiển thị đơn hàng của user hiện tại
    /// - Lấy CustomerID từ UserAccount.UserId
    /// - Parse UserId sang int, nếu lỗi thì redirect Login
    /// 
    /// Data:
    /// - PageSize = 10 đơn hàng mỗi trang
    /// - Status = 0 (tất cả trạng thái)
    /// - SearchValue = "" (không tìm kiếm)
    /// </remarks>
    public async Task<IActionResult> Index(int page = 1)
    {
        var user = HttpContext.Session.GetObject<UserAccount>("User");
        if (user == null)
            return RedirectToAction("Login", "Account");

        var input = new OrderSearchInput()
        {
            Page = page,
            PageSize = 10,
            Status = 0,
            SearchValue = ""
        };

        if (!int.TryParse(user.UserId, out var customerId))
            return RedirectToAction("Login", "Account");

        var result = await _orderRepo.ListByCustomerAsync(input, customerId);
        return View(result);
    }

    // ================= ORDER CREATION =================

    /// <summary>
    /// Hiển thị trang xác nhận đặt hàng
    /// </summary>
    /// <returns>View với giỏ hàng và dropdown tỉnh thành</returns>
    /// <remarks>
    /// Authentication:
    /// - Kiểm tra user đăng nhập
    /// - Redirect về Login nếu chưa đăng nhập
    /// 
    /// Validation:
    /// - Kiểm tra giỏ hàng có sản phẩm
    /// - Nếu giỏ rỗng: redirect về Cart Index
    /// 
    /// Data loading:
    /// - Load provinces từ database qua ProvinceRepository
    /// - Tạo SelectList cho dropdown tỉnh thành
    /// - ViewBag.Provinces để bind vào view
    /// 
    /// Database integration:
    /// - Provinces từ table Provinces trong database
    /// - Đồng bộ với Admin panel
    /// - Không hardcode tỉnh thành
    /// </remarks>
    public async Task<IActionResult> Create()
    {
        var user = HttpContext.Session.GetObject<UserAccount>("User");
        if (user == null)
            return RedirectToAction("Login", "Account");

        var cart = GetCart();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        // Load provinces từ database
        var provincesData = await _provinceRepo.ListAsync();
        
        var provinces = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "-- Chọn Tỉnh/Thành --" }
        };

        foreach (var province in provincesData)
        {
            provinces.Add(new SelectListItem
            {
                Value = province.ProvinceName,
                Text = province.ProvinceName
            });
        }

        ViewBag.Provinces = provinces;

        return View(cart);
    }

    /// <summary>
    /// AJAX endpoint để tạo đơn hàng từ giỏ hàng
    /// </summary>
    /// <param name="input">Thông tin đơn hàng (address, province)</param>
    /// <returns>JSON với kết quả tạo đơn hàng</returns>
    /// <remarks>
    /// Authentication:
    /// - Kiểm tra user đăng nhập trong session
    /// - Parse CustomerID từ UserAccount.UserId
    /// 
    /// Validation:
    /// - Kiểm tra input không null
    /// - Kiểm tra address và province không rỗng
    /// - Kiểm tra giỏ hàng có sản phẩm
    /// 
    /// Business logic:
    /// - Tạo Order với Status = New
    /// - Tạo OrderDetail cho mỗi CartItem
    /// - Sử dụng SalePrice từ Product.Price
    /// - Xóa giỏ hàng sau khi tạo đơn thành công
    /// 
    /// Data flow:
    /// 1. Validate user và input
    /// 2. Create Order trong database
    /// 3. Create OrderDetail cho mỗi item
    /// 4. Remove cart session
    /// 5. Return JSON result
    /// 
    /// Error handling:
    /// - Return JSON với success = false nếu lỗi
    /// - Include error message trong response
    /// - Không throw exception ra client
    /// </remarks>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] OrderInput? input)
    {
        var user = HttpContext.Session.GetObject<UserAccount>("User");
        if (user == null)
            return Json(new { code = 0, message = "Bạn chưa đăng nhập" });

        if (input == null)
            return Json(new { code = 0, message = "Dữ liệu không hợp lệ" });

        if (!int.TryParse(user.UserId, out var customerId))
            return Json(new { code = 0, message = "Phiên đăng nhập không hợp lệ" });

        var cart = GetCart();
        if (!cart.Any())
            return Json(new { code = 0, message = "Giỏ hàng trống" });

        var address = input.Address?.Trim() ?? "";
        var province = input.Province?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(address))
            return Json(new { code = 0, message = "Địa chỉ không được để trống" });

        if (string.IsNullOrWhiteSpace(province))
            return Json(new { code = 0, message = "Tỉnh/Thành phố không được để trống" });

        var order = new Order
        {
            CustomerID = customerId,
            OrderTime = DateTime.Now,
            DeliveryAddress = address,
            DeliveryProvince = province,
            Status = OrderStatusEnum.New
        };

        int orderId = await _orderRepo.AddAsync(order);

        if (orderId <= 0)
            return Json(new { code = 0, message = "Tạo đơn thất bại" });

        foreach (var item in cart)
        {
            var detail = new OrderDetail
            {
                OrderID = orderId,
                ProductID = item.Product.ProductID,
                Quantity = item.Quantity,
                SalePrice = item.Product.Price
            };
            await _orderRepo.AddDetailAsync(detail);
        }

        HttpContext.Session.Remove("Cart");
        return Json(new { code = 1, message = "Đặt hàng thành công", orderId });
    }

    // ================= ORDER DETAILS =================

    /// <summary>
    /// Hiển thị chi tiết đơn hàng
    /// </summary>
    /// <param name="id">Mã đơn hàng cần xem</param>
    /// <returns>View với thông tin đơn hàng và chi tiết sản phẩm</returns>
    /// <remarks>
    /// Authentication:
    /// - Kiểm tra user đăng nhập trong session
    /// - Parse CustomerID từ UserAccount.UserId
    /// 
    /// Authorization:
    /// - Kiểm tra đơn hàng tồn tại
    /// - Kiểm tra đơn hàng thuộc về user hiện tại
    /// - Nếu không thuộc về user: return Forbid()
    /// 
    /// Data loading:
    /// - Load Order thông qua repository
    /// - Load danh sách OrderDetail của đơn hàng
    /// - Return Tuple<Order, List<OrderDetail>> cho view
    /// 
    /// Security:
    /// - User chỉ xem được đơn hàng của mình
    /// - Không thể xem đơn hàng của user khác
    /// - Return Forbid() nếu cố tình truy cập
    /// </remarks>
    public async Task<IActionResult> Details(int id)
    {
        var user = HttpContext.Session.GetObject<UserAccount>("User");
        if (user == null)
            return RedirectToAction("Login", "Account");

        if (!int.TryParse(user.UserId, out var customerId))
            return RedirectToAction("Login", "Account");

        var order = await _orderRepo.GetAsync(id);
        if (order == null)
            return NotFound();

        if (order.CustomerID != customerId)
            return Forbid();

        var details = await _orderRepo.ListDetailsAsync(id);
        return View(Tuple.Create(order, details));
    }

    // ================= ORDER SUCCESS =================

    /// <summary>
    /// Hiển thị trang xác nhận đặt hàng thành công
    /// </summary>
    /// <returns>View thông báo đặt hàng thành công</returns>
    /// <remarks>
    /// Purpose:
    /// - Trang landing sau khi đặt hàng thành công
    /// - Hiển thị thông báo cảm ơn và xác nhận
    /// - Cung cấp các link tiếp theo (về trang chủ, xem đơn hàng)
    /// 
    /// User Experience:
    /// - Giỏ hàng đã được xóa ở bước trước
    /// - User thấy thông báo thành công rõ ràng
    /// - Có thể tiếp tục mua sắm hoặc xem đơn hàng
    /// 
    /// Implementation:
    /// - Simple view không cần data
    /// - Static content với thông báo và links
    /// - Không cần authentication (đã kiểm tra ở bước trước)
    /// </remarks>
    public IActionResult Success()
    {
        return View();
    }
}