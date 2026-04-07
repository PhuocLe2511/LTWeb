using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.HR;

namespace SV22T1020330.DataLayers.SqlServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy cập dữ liệu đối với bảng Employees
    /// trong cơ sở dữ liệu SQL Server
    /// </summary>
    /// <remarks>
    /// Cài đặt interface IEmployeeRepository.
    /// Bao gồm các chức năng: tìm kiếm, lấy thông tin, thêm mới,
    /// cập nhật, xóa và kiểm tra email hợp lệ.
    /// Sử dụng Dapper để thao tác dữ liệu.
    /// </remarks>
    public class EmployeeRepository : IEmployeeRepository
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo EmployeeRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách nhân viên có phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang danh sách nhân viên</returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM Employees
                  WHERE FullName LIKE @search",
                new { search = $"%{input.SearchValue}%" });

            var data = await connection.QueryAsync<Employee>(
                @"SELECT *
                  FROM Employees
                  WHERE FullName LIKE @search
                  ORDER BY FullName
                  OFFSET @offset ROWS
                  FETCH NEXT @pageSize ROWS ONLY",
                new
                {
                    search = $"%{input.SearchValue}%",
                    offset = (input.Page - 1) * input.PageSize,
                    pageSize = input.PageSize
                });

            return new PagedResult<Employee>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy thông tin một nhân viên theo mã
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Đối tượng Employee hoặc null</returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<Employee>(
                @"SELECT *
                  FROM Employees
                  WHERE EmployeeID = @id",
                new { id });
        }

        /// <summary>
        /// Thêm mới một nhân viên
        /// </summary>
        /// <param name="data">Thông tin nhân viên</param>
        /// <returns>Mã EmployeeID vừa thêm</returns>
        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Employees
                (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking, RoleNames)
                VALUES
                (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking, @RoleNames);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="data">Thông tin cần cập nhật</param>
        /// <returns>True nếu thành công, ngược lại False</returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE Employees
                  SET FullName = @FullName,
                      BirthDate = @BirthDate,
                      Address = @Address,
                      Phone = @Phone,
                      Email = @Email,
                      Photo = @Photo,
                      IsWorking = @IsWorking,
                      RoleNames = @RoleNames
                  WHERE EmployeeID = @EmployeeID",
                data);

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateRoleNamesAsync(int employeeID, string? roleNames)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE Employees SET RoleNames = @roleNames WHERE EmployeeID = @employeeID",
                new { employeeID, roleNames });

            return rows > 0;
        }

        /// <summary>
        /// Xóa một nhân viên theo mã
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu xóa thành công, ngược lại False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Employees
                  WHERE EmployeeID = @id",
                new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra nhân viên có đang được sử dụng hay không
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>
        /// True nếu có dữ liệu liên quan (ví dụ: trong Orders),
        /// ngược lại False
        /// </returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM Orders
                  WHERE EmployeeID = @id",
                new { id });

            return count > 0;
        }

        /// <summary>
        /// Kiểm tra email của nhân viên có hợp lệ hay không (không bị trùng)
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
                count = await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*)
                      FROM Employees
                      WHERE Email = @email",
                    new { email });
            }
            else
            {
                count = await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*)
                      FROM Employees
                      WHERE Email = @email AND EmployeeID <> @id",
                    new { email, id });
            }

            return count == 0;
        }
    }
}