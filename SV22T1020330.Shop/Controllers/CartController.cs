using Microsoft.AspNetCore.Mvc;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Cart;
using SV22T1020330.Shop.AppCodes;

namespace SV22T1020330.Shop.Controllers;

/// <summary>
/// Controller quản lý giỏ hàng cho giao diện Shop
/// </summary>
/// <remarks>
/// Chức năng chính:
/// - Thêm sản phẩm vào giỏ hàng
/// - Hiển thị danh sách sản phẩm trong giỏ
/// - Cập nhật số lượng (+/- buttons)
/// - Xóa sản phẩm khỏi giỏ hàng
/// 
/// Session management:
/// - Lưu giỏ hàng trong session với key "Cart"
/// - Tự động tạo giỏ hàng mới nếu chưa có
/// - Lưu lại session sau mỗi thao tác
/// 
/// Business logic:
/// - Kiểm tra sản phẩm tồn tại trước khi thêm
/// - Tăng số lượng nếu sản phẩm đã có trong giỏ
/// - Giới hạn số lượng tối đa 99 sản phẩm
/// - Tự động xóa sản phẩm khi số lượng <= 0
/// </remarks>
public class CartController : Controller
{
    private readonly IProductRepository _productRepo;

    /// <summary>
    /// Khởi tạo CartController với dependency injection
    /// </summary>
    /// <param name="productRepo">Repository xử lý sản phẩm để validate</param>
    public CartController(IProductRepository productRepo)
    {
        _productRepo = productRepo;
    }

    /// <summary>
    /// Lấy giỏ hàng từ session, tạo mới nếu chưa có
    /// </summary>
    /// <returns>Danh sách CartItem trong giỏ hàng</returns>
    /// <remarks>
    /// Session key: "Cart"
    /// - Nếu session null: tạo List rỗng và lưu vào session
    /// - Nếu session có: trả về danh sách hiện tại
    /// - Luôn trả về List không null để tránh lỗi
    /// </remarks>
    private List<CartItem> GetCart()
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");

        if (cart == null)
        {
            cart = new List<CartItem>();
            HttpContext.Session.SetObject("Cart", cart);
        }

        return cart;
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng
    /// </summary>
    /// <param name="productId">Mã sản phẩm cần thêm</param>
    /// <param name="quantity">Số lượng thêm (mặc định = 1)</param>
    /// <returns>Redirect về trang giỏ hàng</returns>
    /// <remarks>
    /// Validation:
    /// - Kiểm tra sản phẩm tồn tại trong database
    /// - Nếu không tồn tại: redirect về trang sản phẩm
    /// 
    /// Business logic:
    /// - Nếu sản phẩm chưa có trong giỏ: thêm mới CartItem
    /// - Nếu sản phẩm đã có: tăng số lượng hiện tại
    /// - Lưu lại session sau khi cập nhật
    /// 
    /// Performance:
    /// - Query database để lấy thông tin sản phẩm đầy đủ
    /// - Lưu product object vào CartItem để không query lại
    /// </remarks>
    public async Task<IActionResult> Add(int productId, int quantity)
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart")
                   ?? new List<CartItem>();

        // 🔥 load product từ DB
        var product = await _productRepo.GetAsync(productId);

        if (product == null)
            return RedirectToAction("Index", "Product");

        var item = cart.FirstOrDefault(x => x.Product.ProductID == productId);

        if (item == null)
        {
            cart.Add(new CartItem
            {
                Product = product,
                Quantity = quantity
            });
        }
        else
        {
            item.Quantity += quantity;
        }

        HttpContext.Session.SetObject("Cart", cart);

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Hiển thị trang giỏ hàng
    /// </summary>
    /// <returns>View với danh sách sản phẩm trong giỏ hàng</returns>
    /// <remarks>
    /// Data:
    /// - Lấy giỏ hàng từ session qua GetCart()
    /// - Truyền List<CartItem> vào view
    /// - View hiển thị: ảnh, tên, đơn giá, số lượng, thành tiền
    /// 
    /// UI Features:
    /// - +/- buttons để tăng/giảm số lượng
    /// - Nút xóa từng sản phẩm
    /// - Tổng cộng tiền
    /// - Nút thanh toán/đặt hàng
    /// </remarks>
    public IActionResult Index()
    {
        return View(GetCart());
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm trong giỏ hàng
    /// </summary>
    /// <param name="productId">Mã sản phẩm cần cập nhật</param>
    /// <param name="change">Thay đổi số lượng (+1 hoặc -1)</param>
    /// <returns>Redirect về trang giỏ hàng</returns>
    /// <remarks>
    /// Business logic:
    /// - Tìm CartItem theo productId
    /// - Cộng change vào quantity hiện tại
    /// - Nếu quantity <= 0: xóa sản phẩm khỏi giỏ
    /// - Nếu quantity > 99: giới hạn lại thành 99
    /// 
    /// Validation:
    /// - Kiểm tra sản phẩm tồn tại trong giỏ
    /// - Không làm gì nếu không tìm thấy
    /// 
    /// Session:
    /// - Lưu lại giỏ hàng sau khi cập nhật
    /// - Redirect về Index để refresh UI
    /// </remarks>
    [HttpPost]
    public IActionResult UpdateQuantity(int productId, int change)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.Product.ProductID == productId);
        
        if (item != null)
        {
            item.Quantity += change;
            if (item.Quantity <= 0)
            {
                cart.Remove(item);
            }
            else if (item.Quantity > 99) // Giới hạn tối đa 99
            {
                item.Quantity = 99;
            }
        }
        
        HttpContext.Session.SetObject("Cart", cart);
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Xóa sản phẩm khỏi giỏ hàng
    /// </summary>
    /// <param name="id">Mã sản phẩm cần xóa</param>
    /// <returns>Redirect về trang giỏ hàng</returns>
    /// <remarks>
    /// Business logic:
    /// - Xóa tất cả CartItem có ProductID = id
    /// - Dùng RemoveAll để xử lý trường hợp trùng lặp
    /// - Lưu lại session sau khi xóa
    /// 
    /// Performance:
    /// - O(n) operation, n = số lượng item trong giỏ
    /// - Thường nhỏ nên không ảnh hưởng performance
    /// 
    /// User Experience:
    /// - User click nút "Xóa" trên từng sản phẩm
    /// - Trang refresh và không còn sản phẩm đã xóa
    /// </remarks>
    public IActionResult Remove(int id)
    {
        var cart = GetCart();
        cart.RemoveAll(x => x.Product.ProductID == id);
        HttpContext.Session.SetObject("Cart", cart);
        return RedirectToAction("Index");
    }
}