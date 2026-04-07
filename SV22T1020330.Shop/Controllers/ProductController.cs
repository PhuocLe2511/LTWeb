using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Catalog;
using SV22T1020330.Models.Common;
using SV22T1020330.Shop;
using SV22T1020330.Shop.Models;

namespace SV22T1020330.Shop.Controllers;

/// <summary>
/// Controller quản lý sản phẩm cho giao diện Shop
/// </summary>
/// <remarks>
/// Chức năng chính:
/// - Hiển thị danh sách sản phẩm với tìm kiếm, lọc, phân trang
/// - Chi tiết sản phẩm với thuộc tính và hình ảnh
/// - Tối ưu performance với caching và AJAX
/// - Giữ trạng thái tìm kiếm qua session
/// 
/// Performance optimizations:
/// - Categories cache 30 phút để giảm DB queries
/// - Products cache 5 phút cho pagination nhanh
/// - Browser cache 5 phút cho Search method
/// - AJAX loading để trang chủ load nhanh
/// 
/// Session management:
/// - Lưu ProductSearchInput trong session
/// - Chỉ lưu khi có thay đổi tìm kiếm
/// - Giữ trang hiện tại khi pagination
/// - Reset trang 1 khi tìm kiếm mới
/// </remarks>
public class ProductController : Controller
{
    private const string CategoriesCacheKey = "Shop:Categories:All";
    private static readonly TimeSpan CategoriesCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromMinutes(5); //  5 phút

    private readonly IProductRepository _productRepo;
    private readonly IGenericRepository<Category> _categoryRepo;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Khởi tạo ProductController với dependency injection
    /// </summary>
    /// <param name="productRepo">Repository xử lý sản phẩm</param>
    /// <param name="categoryRepo">Repository xử lý danh mục</param>
    /// <param name="cache">Memory cache cho performance</param>
    public ProductController(
        IProductRepository productRepo,
        IGenericRepository<Category> categoryRepo,
        IMemoryCache cache)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _cache = cache;
    }

    /// <summary>
    /// Hiển thị trang chủ danh sách sản phẩm
    /// </summary>
    /// <param name="input">Tham số tìm kiếm và phân trang</param>
    /// <returns>View với categories dropdown, products load qua AJAX</returns>
    /// <remarks>
    /// Performance:
    /// - Chỉ load categories với cache (30 phút)
    /// - Products load sau qua AJAX để trang load nhanh
    /// - Lấy search input từ session để giữ trang hiện tại
    /// 
    /// Session:
    /// - Đọc ProductSearchInput từ session
    /// - Giữ nguyên trang hiện tại khi user refresh
    /// - Nếu chưa có session thì tạo input mới
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(ProductSearchInput? input)
    {
        // Lấy search input từ session để giữ trang hiện tại
        var sessionInput = ApplicationContext.GetSessionData<ProductSearchInput>("ProductSearchInput");
        if (sessionInput != null)
        {
            input = sessionInput;
        }
        else
        {
            input ??= new ProductSearchInput();
        }
        
        if (input.PageSize <= 0)
            input.PageSize = 9; // 3 hàng x 3 cột = 9 sản phẩm mỗi trang
        input.OnlySelling = true;

        // Chỉ load categories với cache để trang load nhanh
        var categories = await _cache.GetOrCreateAsync(CategoriesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CategoriesCacheDuration;
            return await _categoryRepo.ListAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 200,
                SearchValue = ""
            });
        });

        var catItems = new List<SelectListItem>
        {
            new() { Value = "0", Text = "Tất cả", Selected = input.CategoryID == 0 }
        };
        foreach (var c in categories.DataItems)
        {
            catItems.Add(new SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.CategoryName,
                Selected = c.CategoryID == input.CategoryID
            });
        }
        ViewBag.Categories = catItems;
        ViewBag.SearchInput = input;
        
        // Trả về view với model rỗng, products sẽ load qua AJAX
        return View(new PagedResult<Product>());
    }

    /// <summary>
    /// AJAX endpoint để load sản phẩm theo tìm kiếm và phân trang
    /// </summary>
    /// <param name="input">Tham số tìm kiếm và phân trang</param>
    /// <returns>PartialView với danh sách sản phẩm</returns>
    /// <remarks>
    /// Performance:
    /// - Server cache 5 phút cho mỗi query combination
    /// - Browser cache 5 phút để giảm requests
    /// - PageSize tối đa 24 để query nhanh
    /// 
    /// Session management:
    /// - Chỉ lưu session khi có thay đổi tìm kiếm
    /// - Không lưu khi chỉ pagination để giữ trang
    /// - So sánh với session hiện tại để detect thay đổi
    /// 
    /// Cache key format:
    /// Shop:Products:{Page}:{PageSize}:{CategoryID}:{SupplierID}:{MinPrice}:{MaxPrice}:{SearchValue}:sell1
    /// </remarks>
    [HttpGet]
    [ResponseCache(Duration = 300)] // Cache 5 phút ở browser
    public async Task<IActionResult> Search(ProductSearchInput input)
    {
        input.OnlySelling = true;
        if (input.Page < 1)
            input.Page = 1;
        if (input.PageSize < 1 || input.PageSize > 24) // Giảm từ 48 xuống 24
            input.PageSize = 12; // Giữ mặc định 12

        // Chỉ lưu session khi có thay đổi tìm kiếm, không phải mỗi lần pagination
        var currentSession = ApplicationContext.GetSessionData<ProductSearchInput>("ProductSearchInput");
        if (currentSession == null || 
            currentSession.CategoryID != input.CategoryID ||
            currentSession.SupplierID != input.SupplierID ||
            currentSession.MinPrice != input.MinPrice ||
            currentSession.MaxPrice != input.MaxPrice ||
            currentSession.SearchValue != input.SearchValue ||
            currentSession.PageSize != input.PageSize)
        {
            ApplicationContext.SetSessionData("ProductSearchInput", input);
        }

        var cacheKey =
            $"Shop:Products:{input.Page}:{input.PageSize}:{input.CategoryID}:{input.SupplierID}:{input.MinPrice}:{input.MaxPrice}:{input.SearchValue}:sell1";

        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = SearchCacheDuration;
            return await _productRepo.ListAsync(input);
        });

        return PartialView("Search", result!);
    }

    /// <summary>
    /// Hiển thị chi tiết sản phẩm
    /// </summary>
    /// <param name="id">Mã sản phẩm</param>
    /// <returns>View với thông tin chi tiết sản phẩm</returns>
    /// <remarks>
    /// Validation:
    /// - Kiểm tra sản phẩm tồn tại
    /// - Chỉ hiển thị sản phẩm đang bán (IsSelling = true)
    /// - Return NotFound() nếu không tìm thấy
    /// 
    /// Data loading:
    /// - Load thông tin sản phẩm chính
    /// - Load danh sách thuộc tính (attributes)
    /// - Load danh sách hình ảnh (chỉ ảnh không bị ẩn)
    /// </remarks>
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productRepo.GetAsync(id);
        if (product == null || !product.IsSelling)
            return NotFound();

        ViewBag.Attributes = await _productRepo.ListAttributesAsync(id);
        ViewBag.Photos = (await _productRepo.ListPhotosAsync(id)).Where(p => !p.IsHidden).ToList();
        return View(product);
    }
}
