using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Partner;

namespace SV22T1020330.DataLayers.SqlServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy cập dữ liệu đối với bảng Customers
    /// trong cơ sở dữ liệu SQL Server
    /// </summary>
    /// <remarks>
    /// Cài đặt interface ICustomerRepository.
    /// Bao gồm các chức năng: tìm kiếm, lấy thông tin, thêm mới,
    /// cập nhật, xóa và kiểm tra email hợp lệ.
    /// Sử dụng Dapper để thao tác dữ liệu.
    /// </remarks>
    public class CustomerRepository : ICustomerRepository
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo CustomerRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách khách hàng có phân trang
        /// </summary>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM Customers
                  WHERE CustomerName LIKE @search",
                new { search = $"%{input.SearchValue}%" });

            var data = await connection.QueryAsync<Customer>(
                @"SELECT *
                  FROM Customers
                  WHERE CustomerName LIKE @search
                  ORDER BY CustomerName
                  OFFSET @offset ROWS
                  FETCH NEXT @pageSize ROWS ONLY",
                new
                {
                    search = $"%{input.SearchValue}%",
                    offset = (input.Page - 1) * input.PageSize,
                    pageSize = input.PageSize
                });

            return new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }
        

        /// <summary>
        /// Lấy thông tin khách hàng theo mã
        /// </summary>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<Customer>(
                @"SELECT *
                  FROM Customers
                  WHERE CustomerID = @id",
                new { id });
        }

        /// <summary>
        /// Thêm mới khách hàng
        /// </summary>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Customers
                (CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                VALUES
                (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE Customers
                  SET CustomerName = @CustomerName,
                      ContactName = @ContactName,
                      Province = @Province,
                      Address = @Address,
                      Phone = @Phone,
                      Email = @Email,
                      IsLocked = @IsLocked
                  WHERE CustomerID = @CustomerID",
                data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa khách hàng theo mã
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Customers
                  WHERE CustomerID = @id",
                new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra khách hàng có đang được sử dụng hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM Orders
                  WHERE CustomerID = @id",
                new { id });

            return count > 0;
        }

        /// <summary>
        /// Kiểm tra email có hợp lệ hay không (không bị trùng)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: kiểm tra khi thêm mới
        /// Nếu id <> 0: kiểm tra khi cập nhật (loại trừ chính nó)
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ (không trùng), False nếu đã tồn tại
        /// </returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);

            int count;

            if (id == 0)
            {
                // Thêm mới → check toàn bộ
                count = await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*)
                      FROM Customers
                      WHERE Email = @email",
                    new { email });
            }
            else
            {
                // Cập nhật → loại trừ chính nó
                count = await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*)
                      FROM Customers
                      WHERE Email = @email AND CustomerID <> @id",
                    new { email, id });
            }

            return count == 0;
        }
    }
}