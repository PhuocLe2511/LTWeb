using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Catalog;
using SV22T1020330.Models.Common;

namespace SV22T1020330.DataLayers.SqlServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy cập dữ liệu đối với bảng Categories
    /// trong cơ sở dữ liệu SQL Server
    /// </summary>
    /// <remarks>
    /// Cài đặt interface IGenericRepository với kiểu dữ liệu Category.
    /// Bao gồm các chức năng: tìm kiếm danh sách, lấy chi tiết, thêm mới,
    /// cập nhật, xóa và kiểm tra dữ liệu liên quan.
    /// Sử dụng Dapper để thao tác với cơ sở dữ liệu.
    /// </remarks>
    public class CategoryRepository : IGenericRepository<Category>
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo CategoryRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách loại hàng có phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang danh sách Category</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM Categories
                  WHERE CategoryName LIKE @search",
                new { search = $"%{input.SearchValue}%" });

            var data = await connection.QueryAsync<Category>(
                @"SELECT *
                  FROM Categories
                  WHERE CategoryName LIKE @search
                  ORDER BY CategoryName
                  OFFSET @offset ROWS
                  FETCH NEXT @pageSize ROWS ONLY",
                new
                {
                    search = $"%{input.SearchValue}%",
                    offset = (input.Page - 1) * input.PageSize,
                    pageSize = input.PageSize
                });

            return new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy thông tin một loại hàng theo mã
        /// </summary>
        /// <param name="id">Mã Category</param>
        /// <returns>Đối tượng Category hoặc null</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<Category>(
                @"SELECT *
                  FROM Categories
                  WHERE CategoryID = @id",
                new { id });
        }

        /// <summary>
        /// Thêm mới một loại hàng
        /// </summary>
        /// <param name="data">Thông tin Category</param>
        /// <returns>Mã CategoryID vừa thêm</returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Categories (CategoryName, Description)
                VALUES (@CategoryName, @Description);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="data">Thông tin cần cập nhật</param>
        /// <returns>True nếu thành công, ngược lại False</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE Categories
                  SET CategoryName = @CategoryName,
                      Description = @Description
                  WHERE CategoryID = @CategoryID",
                data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa một loại hàng theo mã
        /// </summary>
        /// <param name="id">Mã Category</param>
        /// <returns>True nếu xóa thành công, ngược lại False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Categories
                  WHERE CategoryID = @id",
                new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra loại hàng có đang được sử dụng hay không
        /// </summary>
        /// <param name="id">Mã Category</param>
        /// <returns>
        /// True nếu có dữ liệu liên quan (ví dụ: trong Products),
        /// ngược lại False
        /// </returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM Products
                  WHERE CategoryID = @id",
                new { id });

            return count > 0;
        }
    }
}