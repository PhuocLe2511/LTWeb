using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020330.Admin;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Catalog;
using SV22T1020330.BusinessLayers;

namespace SV22T1020330.Admin.Controllers
{
    /// <summary>
    /// Sales: chi xem danh sách/chi tiết mặt hàng. Admin + DataManager: CRUD đầy đủ (lập đơn dùng tìm sản phẩm qua Order/SearchProducts).
    /// </summary>
    [Authorize]
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;
        private const string PRODUCT_SEARCH_CONDITION = "ProductSearchCondition";

        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public ProductController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        private async Task<string?> SaveUploadedPhotoAsync(IFormFile? file, string subfolder)
        {
            if (file == null || file.Length == 0)
                return null;

            if (subfolder != "products")
                subfolder = "products";

            string folder = Path.Combine(MediaPaths.ResolveRoot(_env, _configuration), subfolder);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        [Authorize(Roles = WebUserRoles.AllStaff)]
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH_CONDITION);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = PAGE_SIZE,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            return View(input);
        }

        [Authorize(Roles = WebUserRoles.AllStaff)]
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            input.PageSize = PAGE_SIZE;
            ApplicationContext.SetSessionData(PRODUCT_SEARCH_CONDITION, input);
            var data = await CatalogDataService.ListProductsAsync(input);
            return PartialView(data);
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AllStaff)]
        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            return View(product);
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });

            var product = new Product { IsSelling = true };
            return View("Edit", product);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> Create(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                if (uploadPhoto != null)
                {
                    string? fileName = await SaveUploadedPhotoAsync(uploadPhoto, "products");
                    if (fileName != null)
                        data.Photo = fileName;
                }

                int productId = await CatalogDataService.AddProductAsync(data);
                if (productId > 0)
                    return RedirectToAction("Edit", new { id = productId });

                ModelState.AddModelError("", "Không able to create product. Please try again.");
            }
            catch (ArgumentNullException ex)
            {
                ModelState.AddModelError("", "Danh sách không duoc de trong: " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", "Danh sách không hop le: " + ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có loi xay ra: " + ex.Message);
            }

            // If we get here, something went wrong, reload the view
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            return View("Edit", data);
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            ViewBag.ProductID = id;
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> Edit(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                if (uploadPhoto != null)
                {
                    string? fileName = await SaveUploadedPhotoAsync(uploadPhoto, "products");
                    if (fileName != null)
                        data.Photo = fileName;
                }
                else
                {
                    var existing = await CatalogDataService.GetProductAsync(data.ProductID);
                    if (existing != null)
                        data.Photo = existing.Photo;
                }

                bool success = await CatalogDataService.UpdateProductAsync(data);
                if (success)
                    return RedirectToAction("Edit", new { id = data.ProductID });

                ModelState.AddModelError("", "Không able to update product. Please try again.");
            }
            catch (ArgumentNullException ex)
            {
                ModelState.AddModelError("", "Danh sách không duoc de trong: " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", "Danh sách không hop le: " + ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có loi xay ra: " + ex.Message);
            }

            // If we get here, something went wrong, reload the view
            ViewBag.ProductID = data.ProductID;
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(data.ProductID);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(data.ProductID);
            return View(data);
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            ViewBag.IsUsed = await CatalogDataService.IsUsedProductAsync(id);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> Delete(int id, string confirm)
        {
            if (!string.IsNullOrEmpty(confirm))
                await CatalogDataService.DeleteProductAsync(id);
            return RedirectToAction("Index");
        }

        // Attributes
        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> ListAttributes(int id)
        {
            var attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.ProductID = id;
            return View(attributes);
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public IActionResult CreateAttribute(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            var attr = new ProductAttribute { ProductID = id, DisplayOrder = 1 };
            ViewBag.ProductID = id;
            return View("EditAttribute", attr);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> CreateAttribute(ProductAttribute data)
        {
            try
            {
                if (data.ProductID <= 0)
                    return RedirectToAction("Index");

                await CatalogDataService.AddAttributeAsync(data);
                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (ArgumentNullException ex)
            {
                ModelState.AddModelError("", "Danh sách không duoc de trong: " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", "Danh sách không hop le: " + ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có loi xay ra: " + ex.Message);
            }

            // If we get here, something went wrong, reload the view
            ViewBag.ProductID = data.ProductID;
            return View("EditAttribute", data);
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> EditAttribute(int id, long attributeId)
        {
            var attr = await CatalogDataService.GetAttributeAsync(attributeId);
            if (attr == null)
                return RedirectToAction("Edit", new { id });
            ViewBag.ProductID = id;
            return View(attr);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> EditAttribute(ProductAttribute data)
        {
            await CatalogDataService.UpdateAttributeAsync(data);
            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            var attr = await CatalogDataService.GetAttributeAsync(attributeId);
            if (attr == null)
                return RedirectToAction("Edit", new { id });
            ViewBag.ProductID = id;
            return View(attr);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId, string confirm)
        {
            if (!string.IsNullOrEmpty(confirm))
                await CatalogDataService.DeleteAttributeAsync(attributeId);
            return RedirectToAction("Edit", new { id });
        }

        // Photos
        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> ListPhotos(int id)
        {
            var photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.ProductID = id;
            return View(photos);
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public IActionResult CreatePhoto(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            var photo = new ProductPhoto { ProductID = id, DisplayOrder = 1 };
            ViewBag.ProductID = id;
            return View("EditPhoto", photo);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> CreatePhoto(int id, IFormFile? Photo, string? Description, int DisplayOrder, bool IsHidden)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            string? fileName = await SaveUploadedPhotoAsync(Photo, "products");

            var data = new ProductPhoto
            {
                ProductID = id,
                Photo = fileName ?? "",
                Description = Description ?? "",
                DisplayOrder = DisplayOrder,
                IsHidden = IsHidden
            };

            await CatalogDataService.AddPhotoAsync(data);
            return RedirectToAction("Edit", new { id });
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> EditPhoto(int id, long photoId)
        {
            var photo = await CatalogDataService.GetPhotoAsync(photoId);
            if (photo == null)
                return RedirectToAction("Edit", new { id });
            ViewBag.ProductID = id;
            return View(photo);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> EditPhoto(int id, long photoId, IFormFile? Photo, string? Description, int DisplayOrder, bool IsHidden)
        {
            var existing = await CatalogDataService.GetPhotoAsync(photoId);
            if (existing == null)
                return RedirectToAction("Edit", new { id });

            string? fileName = await SaveUploadedPhotoAsync(Photo, "products");

            existing.Photo = fileName ?? existing.Photo;
            existing.Description = Description ?? "";
            existing.DisplayOrder = DisplayOrder;
            existing.IsHidden = IsHidden;

            await CatalogDataService.UpdatePhotoAsync(existing);
            return RedirectToAction("Edit", new { id });
        }

        [HttpGet]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            var photo = await CatalogDataService.GetPhotoAsync(photoId);
            if (photo == null)
                return RedirectToAction("Edit", new { id });
            ViewBag.ProductID = id;
            return View(photo);
        }

        [HttpPost]
        [Authorize(Roles = WebUserRoles.AdminManager)]
        public async Task<IActionResult> DeletePhoto(int id, long photoId, string confirm)
        {
            if (!string.IsNullOrEmpty(confirm))
                await CatalogDataService.DeletePhotoAsync(photoId);
            return RedirectToAction("Edit", new { id });
        }

    }
}
