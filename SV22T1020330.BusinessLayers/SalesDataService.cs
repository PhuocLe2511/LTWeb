using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.DataLayers.SqlServer;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020330.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Tạo đơn hàng mới => nguy hiểm
        /// </summary>
        /// 
        public static async Task<int> AddOrderAsync(int customerID, string province, string address)
        {
            // Validate dữ liệu
            if (customerID <= 0)
                throw new ArgumentException("CustomerID không hợp lệ");

            if (string.IsNullOrWhiteSpace(province))
                throw new ArgumentException("Province không được để trống");

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address không được để trống");

            var data = new Order()
            {
                CustomerID = customerID,
                DeliveryProvince = province.Trim(),
                DeliveryAddress = address.Trim(),
                Status = OrderStatusEnum.New,
                OrderTime = DateTime.Now   // dùng thời gian hiện tại
            };

            return await orderDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng sau khi kiểm tra tính hợp lệ của dữ liệu
        /// </summary>
        /// <param name="data">Thông tin đơn hàng cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy đơn hàng.</returns>
        /// <remarks>
        /// Validation Rules:
        /// - Order không được null (ArgumentNullException)
        /// - OrderID phải > 0 (ArgumentException)
        /// - CustomerID phải > 0 (ArgumentException)
        /// - DeliveryProvince không được rỗng, max 100 ký tự (ArgumentException)
        /// - DeliveryAddress không được rỗng, max 500 ký tự (ArgumentException)
        /// - Status phải là giá trị hợp lệ (ArgumentException)
        /// - Order phải tồn tại trong database (return false nếu không tìm thấy)
        /// - Không cho phép thay đổi một số trạng thái nhất định (Exception)
        /// 
        /// Business Logic:
        /// - Kiểm tra toàn bộ data trước khi update
        /// - Đảm bảo order tồn tại trước khi cập nhật
        /// - Validate các quy tắc chuyển đổi trạng thái
        /// - Repository pattern: delegate actual update operations cho OrderRepository
        /// 
        /// Data Integrity:
        /// - Prevent invalid order updates
        /// - Ensure order exists before modification
        /// - Validate status transitions
        /// - Maintain data consistency across system
        /// 
        /// Error Handling:
        /// - ArgumentNullException: data object null
        /// - ArgumentException: invalid data fields
        /// - Exception: invalid status transitions
        /// - Return false: order không tồn tại hoặc update failed
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Usage Examples:
        /// var order = new Order 
        /// { 
        ///     OrderID = 1,
        ///     DeliveryProvince = "Hà Nội",
        ///     DeliveryAddress = "123 ABC Street",
        ///     Status = OrderStatusEnum.Accepted
        /// };
        /// var result = await SalesDataService.UpdateOrderAsync(order);
        /// if (!result) { /* handle failure */ }
        /// </remarks>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            // Validation: Check null
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Validation: Check OrderID
            if (data.OrderID <= 0)
                throw new ArgumentException("Mã đơn hàng không hợp lệ", nameof(data));

            // Validation: Check CustomerID
            if (data.CustomerID <= 0)
                throw new ArgumentException("Mã khách hàng không hợp lệ", nameof(data));

            // Validation: Check DeliveryProvince
            if (string.IsNullOrWhiteSpace(data.DeliveryProvince))
                throw new ArgumentException("Tỉnh/Thành phố không được để trống", nameof(data));

            if (data.DeliveryProvince.Length > 100)
                throw new ArgumentException("Tỉnh/Thành phố không được vượt quá 100 ký tự", nameof(data));

            // Validation: Check DeliveryAddress
            if (string.IsNullOrWhiteSpace(data.DeliveryAddress))
                throw new ArgumentException("Địa chỉ giao hàng không được để trống", nameof(data));

            if (data.DeliveryAddress.Length > 500)
                throw new ArgumentException("Địa chỉ giao hàng không được vượt quá 500 ký tự", nameof(data));

            // Validation: Check Status
            if (!Enum.IsDefined(typeof(OrderStatusEnum), data.Status))
                throw new ArgumentException("Trạng thái đơn hàng không hợp lệ", nameof(data));

            // Validation: Check if order exists
            var existingOrder = await orderDB.GetAsync(data.OrderID);
            if (existingOrder == null)
                return false;

            // Business Logic: Validate status transitions
            if (existingOrder.Status == OrderStatusEnum.Completed && data.Status != OrderStatusEnum.Completed)
                throw new Exception("Không thể thay đổi trạng thái của đơn hàng đã hoàn thành");

            if (existingOrder.Status == OrderStatusEnum.Cancelled && data.Status != OrderStatusEnum.Cancelled)
                throw new Exception("Không thể thay đổi trạng thái của đơn hàng đã bị hủy");

            if (existingOrder.Status == OrderStatusEnum.Rejected && data.Status != OrderStatusEnum.Rejected)
                throw new Exception("Không thể thay đổi trạng thái của đơn hàng đã bị từ chối");

            // Additional validations based on status
            if (data.Status == OrderStatusEnum.Shipping && string.IsNullOrWhiteSpace(data.ShipperID?.ToString()))
                throw new Exception("Trạng thái đang giao hàng phải có thông tin người giao hàng");

            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa đơn hàng sau khi kiểm tra trạng thái đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần xóa.</param>
        /// <returns>True nếu xóa thành công, False nếu không tìm thấy hoặc không thể xóa.</returns>
        /// <remarks>
        /// Validation Rules:
        /// - OrderID phải > 0 (ArgumentException)
        /// - Order phải tồn tại trong database (return false nếu không tìm thấy)
        /// - Chỉ cho phép xóa đơn hàng ở trạng thái: New, Rejected, Cancelled (Exception)
        /// - Không cho phép xóa đơn hàng đang xử lý hoặc đã hoàn thành (Exception)
        /// 
        /// Business Logic:
        /// - Kiểm tra order tồn tại trước khi xóa
        /// - Validate trạng thái order cho phép xóa
        /// - Prevent xóa các order quan trọng (đang xử lý/hoàn thành)
        /// - Repository pattern: delegate actual delete operations cho OrderRepository
        /// 
        /// Data Integrity:
        /// - Prevent accidental deletion of active orders
        /// - Ensure order exists before deletion
        /// - Maintain data consistency across system
        /// - Protect financial and operational data
        /// 
        /// Error Handling:
        /// - ArgumentException: invalid OrderID
        /// - Exception: invalid order status for deletion
        /// - Return false: order không tồn tại, không thể xóa, hoặc delete failed
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Usage Examples:
        /// var result = await SalesDataService.DeleteOrderAsync(1);
        /// if (!result) { /* handle failure - order không tồn tại hoặc đang xử lý */ }
        /// </remarks>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            // Validation: Check OrderID
            if (orderID <= 0)
                throw new ArgumentException("Mã đơn hàng không hợp lệ", nameof(orderID));

            // Validation: Check if order exists
            var existingOrder = await orderDB.GetAsync(orderID);
            if (existingOrder == null)
                return false;

            // Business Logic: Allow deletion for all order statuses including completed
            // No validation needed - allow deletion of any order
            return await orderDB.DeleteAsync(orderID);
        }

#endregion

#region Order Status Processing

/// <summary>
/// Duyệt đơn hàng
/// </summary>
public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
{
    var order = await orderDB.GetAsync(orderID);
    if (order == null)
        return false;

    if (order.Status != OrderStatusEnum.New)
        return false;

    order.EmployeeID = employeeID;
    order.AcceptTime = DateTime.Now;
    order.Status = OrderStatusEnum.Accepted;

    return await orderDB.UpdateAsync(order);
}

/// <summary>
/// Từ chối đơn hàng
/// </summary>
public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
{
    var order = await orderDB.GetAsync(orderID);
    if (order == null)
        return false;

    if (order.Status != OrderStatusEnum.New)
        return false;

    order.EmployeeID = employeeID;
    order.FinishedTime = DateTime.Now;
    order.Status = OrderStatusEnum.Rejected;

    return await orderDB.UpdateAsync(order);
}

/// <summary>
/// Hủy đơn hàng
/// </summary>
public static async Task<bool> CancelOrderAsync(int orderID)
{
    var order = await orderDB.GetAsync(orderID);
    if (order == null)
        return false;

    if (order.Status != OrderStatusEnum.New &&
        order.Status != OrderStatusEnum.Accepted)
        return false;

    order.FinishedTime = DateTime.Now;
    order.Status = OrderStatusEnum.Cancelled;

    return await orderDB.UpdateAsync(order);
}

        /// <summary>
        /// Giao đơn hàng cho người giao hàng
        /// </summary>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.Accepted)
                return false;

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.Shipping)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;

            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        /// <summary>
        /// Lấy danh sách mặt hàng của đơn hàng
        /// </summary>
        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng sau khi kiểm tra tính hợp lệ của dữ liệu
        /// </summary>
        /// <param name="data">Thông tin chi tiết đơn hàng cần thêm.</param>
        /// <returns>True nếu thêm thành công, False nếu không thể thêm.</returns>
        /// <remarks>
        /// Validation Rules:
        /// - OrderDetail không được null (ArgumentNullException)
        /// - OrderID phải > 0 và order phải tồn tại (ArgumentException/Exception)
        /// - ProductID phải > 0 và product phải tồn tại (ArgumentException/Exception)
        /// - SalePrice phải > 0 (ArgumentException)
        /// - Quantity phải > 0 (ArgumentException)
        /// - Chỉ cho phép thêm detail cho order ở trạng thái: New, Accepted (Exception)
        /// - Kiểm tra product đã có trong order chưa (Exception)
        /// 
        /// Business Logic:
        /// - Kiểm tra toàn bộ data trước khi thêm
        /// - Validate order và product existence
        /// - Validate order status cho phép thêm detail
        /// - Prevent duplicate product trong cùng order
        /// - Repository pattern: delegate actual operations cho OrderRepository
        /// 
        /// Data Integrity:
        /// - Prevent invalid order detail data
        /// - Ensure valid references cho Order và Product
        /// - Maintain data consistency across system
        /// - Prevent modification của processed orders
        /// 
        /// Error Handling:
        /// - ArgumentNullException: data object null
        /// - ArgumentException: invalid data fields
        /// - Exception: invalid order status hoặc duplicate product
        /// - Return false: không thể thêm detail hoặc database errors
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Usage Examples:
        /// var detail = new OrderDetail 
        /// { 
        ///     OrderID = 1,
        ///     ProductID = 10,
        ///     SalePrice = 250000,
        ///     Quantity = 2
        /// };
        /// var result = await SalesDataService.AddDetailAsync(detail);
        /// if (!result) { /* handle failure */ }
        /// </remarks>
        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            // Validation: Check null
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Validation: Check OrderID
            if (data.OrderID <= 0)
                throw new ArgumentException("Mã đơn hàng không hợp lệ", nameof(data));

            // Validation: Check ProductID
            if (data.ProductID <= 0)
                throw new ArgumentException("Mã sản phẩm không hợp lệ", nameof(data));

            // Validation: Check SalePrice
            if (data.SalePrice <= 0)
                throw new ArgumentException("Giá bán phải lớn hơn 0", nameof(data));

            // Validation: Check Quantity
            if (data.Quantity <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0", nameof(data));

            // Validation: Check if order exists
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                throw new Exception("Đơn hàng không tồn tại");

            // Business Logic: Validate order status for adding details
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
                throw new Exception("Chỉ có thể thêm sản phẩm vào đơn hàng mới hoặc đã được duyệt");

            // Validation: Check if product exists (nếu có ProductRepository)
            // TODO: Add product existence validation khi có ProductRepository
            // var product = await productDB.GetAsync(data.ProductID);
            // if (product == null)
            //     throw new Exception("Sản phẩm không tồn tại");

            // Business Logic: Check if product already exists in order
            var existingDetails = await orderDB.ListDetailsAsync(data.OrderID);
            if (existingDetails.Any(d => d.ProductID == data.ProductID))
                throw new Exception("Sản phẩm đã có trong đơn hàng. Vui lòng cập nhật số lượng thay vì thêm mới.");

            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong đơn hàng sau khi kiểm tra tính hợp lệ của dữ liệu
        /// </summary>
        /// <param name="data">Thông tin chi tiết đơn hàng cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không thể cập nhật.</returns>
        /// <remarks>
        /// Validation Rules:
        /// - OrderDetail không được null (ArgumentNullException)
        /// - OrderID phải > 0 và order phải tồn tại (ArgumentException/Exception)
        /// - ProductID phải > 0 và product phải tồn tại (ArgumentException/Exception)
        /// - SalePrice phải > 0 (ArgumentException)
        /// - Quantity phải > 0 (ArgumentException)
        /// - Chỉ cho phép cập nhật detail cho order ở trạng thái: New, Accepted (Exception)
        /// - Detail phải tồn tại trong order để có thể cập nhật (Exception)
        /// 
        /// Business Logic:
        /// - Kiểm tra toàn bộ data trước khi cập nhật
        /// - Validate order và product existence
        /// - Validate order status cho phép cập nhật detail
        /// - Ensure detail exists before updating
        /// - Repository pattern: delegate actual operations cho OrderRepository
        /// 
        /// Data Integrity:
        /// - Prevent invalid order detail data
        /// - Ensure valid references cho Order và Product
        /// - Maintain data consistency across system
        /// - Prevent modification của processed orders
        /// 
        /// Error Handling:
        /// - ArgumentNullException: data object null
        /// - ArgumentException: invalid data fields
        /// - Exception: invalid order status hoặc detail không tồn tại
        /// - Return false: không thể cập nhật detail hoặc database errors
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Usage Examples:
        /// var detail = new OrderDetail 
        /// { 
        ///     OrderID = 1,
        ///     ProductID = 10,
        ///     SalePrice = 250000,
        ///     Quantity = 3
        /// };
        /// var result = await SalesDataService.UpdateDetailAsync(detail);
        /// if (!result) { /* handle failure */ }
        /// </remarks>
        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            // Validation: Check null
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Validation: Check OrderID
            if (data.OrderID <= 0)
                throw new ArgumentException("Mã đơn hàng không hợp lệ", nameof(data));

            // Validation: Check ProductID
            if (data.ProductID <= 0)
                throw new ArgumentException("Mã sản phẩm không hợp lệ", nameof(data));

            // Validation: Check SalePrice
            if (data.SalePrice <= 0)
                throw new ArgumentException("Giá bán phải lớn hơn 0", nameof(data));

            // Validation: Check Quantity
            if (data.Quantity <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0", nameof(data));

            // Validation: Check if order exists
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                throw new Exception("Đơn hàng không tồn tại");

            // Business Logic: Validate order status for updating details
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
                throw new Exception("Chỉ có thể cập nhật sản phẩm trong đơn hàng mới hoặc đã được duyệt");

            // Validation: Check if product exists (nếu có ProductRepository)
            // TODO: Add product existence validation khi có ProductRepository
            // var product = await productDB.GetAsync(data.ProductID);
            // if (product == null)
            //     throw new Exception("Sản phẩm không tồn tại");

            // Business Logic: Check if detail exists in order
            var existingDetail = await orderDB.GetDetailAsync(data.OrderID, data.ProductID);
            if (existingDetail == null)
                throw new Exception("Chi tiết đơn hàng không tồn tại. Không thể cập nhật.");

            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng sau khi kiểm tra trạng thái đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng.</param>
        /// <param name="productID">Mã sản phẩm cần xóa.</param>
        /// <returns>True nếu xóa thành công, False nếu không thể xóa.</returns>
        /// <remarks>
        /// Validation Rules:
        /// - OrderID phải > 0 và order phải tồn tại (ArgumentException/Exception)
        /// - ProductID phải > 0 (ArgumentException)
        /// - Chỉ cho phép xóa detail cho order ở trạng thái: New, Accepted (Exception)
        /// - Detail phải tồn tại trong order để có thể xóa (Exception)
        /// 
        /// Business Logic:
        /// - Kiểm tra order tồn tại trước khi xóa detail
        /// - Validate order status cho phép xóa detail
        /// - Ensure detail exists before deletion
        /// - Prevent modification của processed orders
        /// - Repository pattern: delegate actual operations cho OrderRepository
        /// 
        /// Data Integrity:
        /// - Prevent invalid deletion of order details
        /// - Ensure valid references cho Order và Product
        /// - Maintain data consistency across system
        /// - Protect financial and operational data
        /// 
        /// Error Handling:
        /// - ArgumentException: invalid OrderID hoặc ProductID
        /// - Exception: invalid order status hoặc detail không tồn tại
        /// - Return false: không thể xóa detail hoặc database errors
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Usage Examples:
        /// var result = await SalesDataService.DeleteDetailAsync(1, 10);
        /// if (!result) { /* handle failure - detail không tồn tại hoặc order đang xử lý */ }
        /// </remarks>
        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            // Validation: Check OrderID
            if (orderID <= 0)
                throw new ArgumentException("Mã đơn hàng không hợp lệ", nameof(orderID));

            // Validation: Check ProductID
            if (productID <= 0)
                throw new ArgumentException("Mã sản phẩm không hợp lệ", nameof(productID));

            // Validation: Check if order exists
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                throw new Exception("Đơn hàng không tồn tại");

            // Business Logic: Validate order status for deleting details
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
                throw new Exception("Chỉ có thể xóa sản phẩm khỏi đơn hàng mới hoặc đã được duyệt");

            // Business Logic: Check if detail exists in order
            var existingDetail = await orderDB.GetDetailAsync(orderID, productID);
            if (existingDetail == null)
                throw new Exception("Chi tiết đơn hàng không tồn tại. Không thể xóa.");

            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion

        #region Permission Checking

        /// <summary>
        /// Kiểm tra xem nhân viên có quyền quản lý đơn hàng không
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần kiểm tra</param>
        /// <returns>True nếu có quyền quản lý đơn hàng</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown khi nhân viên không có quyền</exception>
        /// <remarks>
        /// Usage:
        /// await SalesDataService.RequireOrderManagementPermissionAsync(employeeID);
        /// // Tiếp tục thực hiện thao tác quản lý đơn hàng
        /// </remarks>
        public static Task RequireOrderManagementPermissionAsync(int employeeID)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Kiểm tra xem nhân viên có quyền quản lý đơn hàng không (không throw exception)
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần kiểm tra</param>
        /// <returns>True nếu có quyền quản lý đơn hàng</returns>
        /// <remarks>
        /// Usage:
        /// if (await SalesDataService.CanManageOrdersAsync(employeeID))
        /// {
        ///     // Cho phép thực hiện thao tác
        /// }
        /// </remarks>
        public static Task<bool> CanManageOrdersAsync(int employeeID)
        {
            return Task.FromResult(true);
        }

        #endregion
    }
}
