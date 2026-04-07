using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Sales;

namespace SV22T1020330.DataLayers.SqlServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy cập dữ liệu đối với đơn hàng (Orders)
    /// và chi tiết đơn hàng (OrderDetails) trong SQL Server
    /// </summary>
    /// <remarks>
    /// Cài đặt interface IOrderRepository.
    /// Bao gồm:
    /// - Quản lý đơn hàng (CRUD)
    /// - Quản lý chi tiết đơn hàng
    /// - Tìm kiếm + phân trang
    /// Sử dụng Dapper để thao tác dữ liệu
    /// </remarks>
    public class OrderRepository : IOrderRepository
    {
        /// <summary>
        /// Chuỗi kết nối đến cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region ===== ORDER =====

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng có phân trang
        /// </summary>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            string where = @"WHERE (@search = '' OR C.CustomerName LIKE @search)
                             AND (@status = 0 OR O.Status = @status)";

            int count = await connection.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)
                   FROM Orders O
                   LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
                   {where}",
                new
                {
                    search = $"%{input.SearchValue}%",
                    status = input.Status
                });

            var data = await connection.QueryAsync<OrderViewInfo>(
                $@"SELECT O.*,
                          C.CustomerName,
                          C.ContactName AS CustomerContactName,
                          C.Email AS CustomerEmail,
                          C.Phone AS CustomerPhone,
                          C.Address AS CustomerAddress,
                          E.FullName AS EmployeeName,
                          S.ShipperName,
                          S.Phone AS ShipperPhone
                   FROM Orders O
                   LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
                   LEFT JOIN Employees E ON O.EmployeeID = E.EmployeeID
                   LEFT JOIN Shippers S ON O.ShipperID = S.ShipperID
                   {where}
                   ORDER BY O.OrderTime DESC
                   OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY",
                new
                {
                    search = $"%{input.SearchValue}%",
                    status = input.Status,
                    offset = (input.Page - 1) * input.PageSize,
                    pageSize = input.PageSize
                });

            return new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }



        #region ===== FILTER BY CUSTOMER =====

        public async Task<PagedResult<OrderViewInfo>> ListByCustomerAsync(OrderSearchInput input, int customerId)
        {
            using var connection = new SqlConnection(_connectionString);

            string where = @"WHERE O.CustomerID = @customerId
                     AND (@search = '' OR C.CustomerName LIKE @search)
                     AND (@status = 0 OR O.Status = @status)";

            int count = await connection.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)
           FROM Orders O
           LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
           {where}",
                new
                {
                    customerId,
                    search = $"%{input.SearchValue}%",
                    status = input.Status
                });

            var data = await connection.QueryAsync<OrderViewInfo>(
                $@"SELECT O.*,
                  C.CustomerName,
                  C.ContactName AS CustomerContactName,
                  C.Email AS CustomerEmail,
                  C.Phone AS CustomerPhone,
                  C.Address AS CustomerAddress,
                  E.FullName AS EmployeeName,
                  S.ShipperName,
                  S.Phone AS ShipperPhone
           FROM Orders O
           LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
           LEFT JOIN Employees E ON O.EmployeeID = E.EmployeeID
           LEFT JOIN Shippers S ON O.ShipperID = S.ShipperID
           {where}
           ORDER BY O.OrderTime DESC
           OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY",
                new
                {
                    customerId,
                    search = $"%{input.SearchValue}%",
                    status = input.Status,
                    offset = (input.Page - 1) * input.PageSize,
                    pageSize = input.PageSize
                });

            return new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        #endregion



        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng theo mã
        /// </summary>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(
                @"SELECT O.*,
                         C.CustomerName,
                         C.ContactName AS CustomerContactName,
                         C.Email AS CustomerEmail,
                         C.Phone AS CustomerPhone,
                         C.Address AS CustomerAddress,
                         E.FullName AS EmployeeName,
                         S.ShipperName,
                         S.Phone AS ShipperPhone
                  FROM Orders O
                  LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
                  LEFT JOIN Employees E ON O.EmployeeID = E.EmployeeID
                  LEFT JOIN Shippers S ON O.ShipperID = S.ShipperID
                  WHERE O.OrderID = @orderID",
                new { orderID });
        }

        /// <summary>
        /// Thêm mới một đơn hàng
        /// </summary>
        public async Task<int> AddAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Orders
                (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, Status)
                VALUES
                (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @Status);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE Orders
                  SET CustomerID = @CustomerID,
                      DeliveryProvince = @DeliveryProvince,
                      DeliveryAddress = @DeliveryAddress,
                      EmployeeID = @EmployeeID,
                      AcceptTime = @AcceptTime,
                      ShipperID = @ShipperID,
                      ShippedTime = @ShippedTime,
                      FinishedTime = @FinishedTime,
                      Status = @Status
                  WHERE OrderID = @OrderID",
                data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa đơn hàng (xóa cả chi tiết trước)
        /// </summary>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.ExecuteAsync(
                @"DELETE FROM OrderDetails WHERE OrderID = @orderID",
                new { orderID });

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Orders WHERE OrderID = @orderID",
                new { orderID });

            return rows > 0;
        }

        #endregion

        #region ===== ORDER DETAIL =====

        /// <summary>
        /// Lấy danh sách chi tiết đơn hàng
        /// </summary>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<OrderDetailViewInfo>(
                @"SELECT OD.*,
                         P.ProductName,
                         P.Unit,
                         P.Photo
                  FROM OrderDetails OD
                  JOIN Products P ON OD.ProductID = P.ProductID
                  WHERE OD.OrderID = @orderID",
                new { orderID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(
                @"SELECT OD.*,
                         P.ProductName,
                         P.Unit,
                         P.Photo
                  FROM OrderDetails OD
                  JOIN Products P ON OD.ProductID = P.ProductID
                  WHERE OD.OrderID = @orderID AND OD.ProductID = @productID",
                new { orderID, productID });
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng
        /// </summary>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                  VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)",
                data);

            return rows > 0;
        }

        /// <summary>
        /// Cập nhật chi tiết đơn hàng
        /// </summary>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE OrderDetails
                  SET Quantity = @Quantity,
                      SalePrice = @SalePrice
                  WHERE OrderID = @OrderID AND ProductID = @ProductID",
                data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM OrderDetails
                  WHERE OrderID = @orderID AND ProductID = @productID",
                new { orderID, productID });

            return rows > 0;
        }

        #endregion
    }
}