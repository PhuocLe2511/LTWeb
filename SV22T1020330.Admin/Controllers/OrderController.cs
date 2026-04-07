using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020330.BusinessLayers;
using SV22T1020330.Models.Catalog;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Sales;
using SV22T1020330.DataLayers.SqlServer;
using System.Threading.Tasks;

namespace SV22T1020330.Admin.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const string PRODUCT_SEARCH = "SearchProductToSale";
        private const string ORDER_SEARCH = "OrderSearchInput";

        /// <summary>
        /// Hiển thị trang tìm kiếm đơn hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);

            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    Status = 0 // tất cả trạng thái
                };
            }

            return View(input);
        }

        /// <summary>
        /// Thực hiện tìm kiếm đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] OrderSearchInput input)
        {
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    Status = 0
                };
            }

            // Handle DateFrom and DateTo from query string if not bound properly
            var dateFromQuery = HttpContext.Request.Query["DateFrom"].ToString();
            var dateToQuery = HttpContext.Request.Query["DateTo"].ToString();
            
            if (!string.IsNullOrEmpty(dateFromQuery))
            {
                if (DateTime.TryParse(dateFromQuery, out DateTime parsedDateFrom))
                {
                    input.DateFrom = parsedDateFrom;
                }
            }
            
            if (!string.IsNullOrEmpty(dateToQuery))
            {
                if (DateTime.TryParse(dateToQuery, out DateTime parsedDateTo))
                {
                    input.DateTo = parsedDateTo;
                }
            }

            // Debug: Log DateFrom and DateTo values
            System.Diagnostics.Debug.WriteLine($"DateFrom: {input.DateFrom}, DateTo: {input.DateTo}, Status: {input.Status}, SearchValue: {input.SearchValue}");
            System.Diagnostics.Debug.WriteLine($"DateFromQuery: {dateFromQuery}, DateToQuery: {dateToQuery}");

            var result = await SalesDataService.ListOrdersAsync(input);

            ApplicationContext.SetSessionData(ORDER_SEARCH, input);

            return PartialView("Search", result);
        }
        /// <summary>
        /// Tạo mới một đơn hàng
        /// </summary>
        /// <returns></returns>
        /// 
        public async Task<IActionResult> Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    SearchValue = "",
                    
                };
            }

            // Load customers dropdown
            var customers = await PartnerDataService.ListCustomersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 100,
                SearchValue = ""
            });

            var customerItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "-- Chọn khách hàng --" }
            };

            foreach (var customer in customers.DataItems)
            {
                customerItems.Add(new SelectListItem
                {
                    Value = customer.CustomerID.ToString(),
                    Text = customer.CustomerName
                });
            }

            ViewBag.Customers = customerItems;

            // Load provinces dropdown from database
            try
            {
                var provinceRepo = new ProvinceRepository(Configuration.ConnectionString);
                var provincesData = await provinceRepo.ListAsync();
                
                var provinces = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Chọn Tỉnh/thành --" }
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
            }
            catch (Exception)
            {
                // Fallback to hardcoded provinces if database not available
                var provinces = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Chọn Tỉnh/thành --" },
                    new SelectListItem { Value = "Hà Nội", Text = "Hà Nội" },
                    new SelectListItem { Value = "TP.HCM", Text = "TP.HCM" },
                    new SelectListItem { Value = "Đà Nẵng", Text = "Đà Nẵng" },
                    new SelectListItem { Value = "Huế", Text = "Huế" },
                    new SelectListItem { Value = "Hải Phòng", Text = "Hải Phòng" },
                    new SelectListItem { Value = "Cần Thơ", Text = "Cần Thơ" },
                    new SelectListItem { Value = "Nha Trang", Text = "Nha Trang" },
                    new SelectListItem { Value = "Buôn Ma Thuột", Text = "Buôn Ma Thuột" },
                    new SelectListItem { Value = "Đà Lạt", Text = "Đà Lạt" },
                    new SelectListItem { Value = "Vũng Tàu", Text = "Vũng Tàu" }
                };

                ViewBag.Provinces = provinces;
            }

            return View(input);
        }
        /// <summary>
        /// tk vaf trar veef ds caafn basn
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            

            var result = await CatalogDataService.ListProductsAsync(input);

            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);

            return PartialView("SearchProduct", result);
        }

        public IActionResult showCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);


        }
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal price)
        {
            if(quantity <= 0)
            {
                return Json(new ApiResult(0,"Số lượng không hợp lệ"));
            }
            if(price < 0)
            {
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            }

            var Product = await CatalogDataService.GetProductAsync(productID);
            if(Product == null)
            {
                return Json(new ApiResult(0, "Sản phẩm không tồn tại"));
            }
            if(!Product.IsSelling)
            {
                return Json(new ApiResult(0, "Sản phẩm không còn bán"));
            }
            var item = new OrderDetailViewInfo()
            {
                ProductID = productID,
                ProductName = Product.ProductName,
                Quantity = quantity,
                SalePrice = price,
                Unit = Product.Unit,
                Photo = Product.Photo ?? "nophoto.png"
            };
            ShoppingCartService.AddCartItem(item);
            return Json(new ApiResult(1, "Thêm sản phẩm vào đơn hàng thành công"));


        }
        /// <summary>
        /// Chi tiết đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xem chi tiết</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            // Lấy đơn hàng
            var order = await SalesDataService.GetOrderAsync(id);

            if (order == null)
                return NotFound();

            // Lấy chi tiết (ĐÚNG TÊN HÀM)
            var details = await SalesDataService.ListDetailsAsync(id);

            // Tránh null
            if (details == null)
                details = new List<OrderDetailViewInfo>();

            return View(Tuple.Create(order, details));
        }

        /// <summary>
        /// Hiển thị thông tin cần cập nhật mặt hàng trong giỏ hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần cập nhật</param>
        /// <returns></returns>
        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }
        [HttpPost]
        public IActionResult UpdateCartItem(int productId, int quantity, decimal SalePrice)
        {
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            }
            if (SalePrice < 0)
            {
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            }
            
            ShoppingCartService.UpdateCartItem(productId, quantity, SalePrice);
            return Json(new ApiResult(1, "Cập nhật sản phẩm trong đơn hàng thành công"));
        }
        /// <summary>
        /// Xoá mặt hàng ra khỏi giỏ hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xoá</param>
        /// <returns></returns>
        public IActionResult DeleteCartItem(int productId = 0)
        {
            if(Request.Method == "POST")
            {
                ShoppingCartService.RemoveCartItem(productId);
                return Json(new ApiResult(1, "Xoá sản phẩm khỏi đơn hàng thành công"));
            }
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);

        }
        public IActionResult ClearCart()
        {
            if(Request.Method == "POST")
            {
                ShoppingCartService.ClearCart();
                return Json(new ApiResult(1, "Xoá giỏ hàng thành công"));
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int CustomerID = 0, string Province = "", string address = "")
        {
            try
            {
                var cart = ShoppingCartService.GetShoppingCart();

                // 1. Kiểm tra giỏ hàng
                if (cart == null || cart.Count == 0)
                {
                    return Json(new ApiResult(0, "Giỏ hàng trống, không thể tạo đơn hàng"));
                }

                // 2. Validate dữ liệu
                if (CustomerID <= 0) CustomerID = 1;
                if (string.IsNullOrWhiteSpace(Province)) Province = "Huế";
                if (string.IsNullOrWhiteSpace(address)) address = "Test";

                // 3. Tạo đơn hàng
                int orderID = await SalesDataService.AddOrderAsync(CustomerID, Province, address);

                if (orderID <= 0)
                {
                    return Json(new ApiResult(0, "Tạo đơn hàng thất bại"));
                }

                // 4. Thêm chi tiết đơn hàng (FIX đúng cú pháp)
                foreach (var item in cart)
                {
                    var detail = new OrderDetail()
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    };

                    await SalesDataService.AddDetailAsync(detail);
                }

                // 5. Xóa giỏ hàng
                ShoppingCartService.ClearCart();

                return Json(new ApiResult(orderID, "Tạo đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, $"Lỗi hệ thống: {ex.Message}"));
            }
        }
        public async Task<IActionResult> Accept(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();

            // Kiểm tra trạng thái đơn hàng
            if (order.Status != OrderStatusEnum.New)
            {
                ViewBag.ErrorMessage = $"Đơn hàng này đã được {order.Status.GetDescription().ToLower()}, không thể duyệt lại!";
                ViewBag.OrderStatus = order.Status;
                return View("StatusError");
            }

            if (Request.Method == "POST")
            {
                try
                {
                    // Lấy employeeID từ user hiện tại
                    var userData = User.GetUserData();
                    int employeeID = Convert.ToInt32(userData?.UserId ?? "1");
                    
                    bool success = await SalesDataService.AcceptOrderAsync(id, employeeID);
                    if (success)
                    {
                        return Json(new { success = true, message = "" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không thể duyệt đơn hàng!" });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
                }
            }

            return View(order);
        }

        public async Task<IActionResult> Shipping(int id, int shipperID = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();

            // Kiểm tra trạng thái đơn hàng
            if (order.Status != OrderStatusEnum.Accepted)
            {
                ViewBag.ErrorMessage = $"Đơn hàng này đang {order.Status.GetDescription().ToLower()}, không thể chuyển giao hàng!";
                ViewBag.OrderStatus = order.Status;
                return View("StatusError");
            }

            if (Request.Method == "POST")
            {
                try
                {
                    if (shipperID <= 0)
                    {
                        return Json(new { success = false, message = "Vui lòng chọn người giao hàng!" });
                    }
                    
                    bool success = await SalesDataService.ShipOrderAsync(id, shipperID);
                    if (success)
                    {
                        return Json(new { success = true, message = "" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không thể chuyển giao hàng!" });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
                }
            }

            // Load shippers cho dropdown
            var shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 100,
                SearchValue = ""
            });

            ViewBag.Shippers = shippers.DataItems;
            return View(order);
        }

        public async Task<IActionResult> Finish(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();

            if (order.Status != OrderStatusEnum.Shipping)
            {
                ModelState.AddModelError("", "Đơn hàng không ở trạng thái đang giao");
                return View(order);
            }

            if (Request.Method == "POST")
            {
                var result = await SalesDataService.CompleteOrderAsync(id);
                if (result)
                {
                    return RedirectToAction("Detail", new { id });
                }
                else
                {
                    ModelState.AddModelError("", "Không thể hoàn tất đơn hàng");
                }
            }

            return View(order);
        }
        public async Task<IActionResult> Reject(int id, string rejectReason = "")
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();

            if (order.Status != OrderStatusEnum.New)
            {
                ModelState.AddModelError("", "Đơn hàng không ở trạng thái chờ duyệt");
                return View(order);
            }

            if (Request.Method == "POST")
            {
                if (string.IsNullOrWhiteSpace(rejectReason))
                {
                    ModelState.AddModelError("", "Vui lòng nhập lý do từ chối");
                    return View(order);
                }

                // Lấy employeeID từ user hiện tại
                var userData = User.GetUserData();
                int employeeID = Convert.ToInt32(userData?.UserId ?? "1");
                
                var result = await SalesDataService.RejectOrderAsync(id, employeeID);
                if (result)
                {
                    // TODO: Lưu lý do từ chối vào database
                    return RedirectToAction("Detail", new { id });
                }
                else
                {
                    ModelState.AddModelError("", "Không thể từ chối đơn hàng");
                }
            }

            return View(order);
        }
        public async Task<IActionResult> Cancel(int id, string cancelReason = "")
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();

            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                ModelState.AddModelError("", "Đơn hàng không ở trạng thái có thể hủy");
                return View(order);
            }

            if (Request.Method == "POST")
            {
                if (string.IsNullOrWhiteSpace(cancelReason))
                {
                    ModelState.AddModelError("", "Vui lòng nhập lý do hủy");
                    return View(order);
                }

                var result = await SalesDataService.CancelOrderAsync(id);
                if (result)
                {
                    // TODO: Lưu lý do hủy vào database
                    return RedirectToAction("Detail", new { id });
                }
                else
                {
                    ModelState.AddModelError("", "Không thể hủy đơn hàng");
                }
            }

            return View(order);
        }
        public async Task<IActionResult> Delete(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();

            // Allow delete for all order statuses including completed
            // No validation needed - allow deletion of any order

            if (Request.Method == "POST")
            {
                var result = await SalesDataService.DeleteOrderAsync(id);
                if (result)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Không thể xóa đơn hàng");
                }
            }

            return View(order);
        }

    }
}
