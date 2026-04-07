using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Partner;

namespace SV22T1020330.DataLayers.SqlServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy cập dữ liệu đối với bảng Shippers
    /// trong cơ sở dữ liệu SQL Server
    /// </summary>
    /// <remarks>
    /// Lớp này cài đặt interface IGenericRepository với kiểu dữ liệu Shipper.
    /// Các chức năng bao gồm: tìm kiếm danh sách, lấy thông tin, thêm mới,
    /// cập nhật, xóa và kiểm tra dữ liệu liên quan.
    /// Sử dụng thư viện Dapper để thao tác dữ liệu.
    /// </remarks>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng ShipperRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách người giao hàng có phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang danh sách Shipper</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM Shippers
                  WHERE ShipperName LIKE @search",
                new { search = $"%{input.SearchValue}%" });

            var data = await connection.QueryAsync<Shipper>(
                @"SELECT *
                  FROM Shippers
                  WHERE ShipperName LIKE @search
                  ORDER BY ShipperName
                  OFFSET @offset ROWS
                  FETCH NEXT @pageSize ROWS ONLY",
                new
                {
                    search = $"%{input.SearchValue}%",
                    offset = (input.Page - 1) * input.PageSize,
                    pageSize = input.PageSize
                });

            return new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy thông tin một người giao hàng theo mã
        /// </summary>
        /// <param name="id">Mã Shipper</param>
        /// <returns>Đối tượng Shipper hoặc null nếu không tồn tại</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<Shipper>(
                @"SELECT *
                  FROM Shippers
                  WHERE ShipperID = @id",
                new { id });
        }

        /// <summary>
        /// Thêm mới một người giao hàng
        /// </summary>
        /// <param name="data">Thông tin Shipper</param>
        /// <returns>Mã ShipperID vừa được thêm</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Shippers (ShipperName, Phone)
                VALUES (@ShipperName, @Phone);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        /// <param name="data">Thông tin cần cập nhật</param>
        /// <returns>True nếu thành công, False nếu thất bại</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE Shippers
                  SET ShipperName = @ShipperName,
                      Phone = @Phone
                  WHERE ShipperID = @ShipperID",
                data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa một người giao hàng theo mã
        /// </summary>
        /// <param name="id">Mã Shipper</param>
        /// <returns>True nếu xóa thành công, ngược lại False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Shippers
                  WHERE ShipperID = @id",
                new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra người giao hàng có đang được sử dụng hay không
        /// </summary>
        /// <param name="id">Mã Shipper</param>
        /// <returns>
        /// True nếu có dữ liệu liên quan (ví dụ: nằm trong Orders),
        /// ngược lại False
        /// </returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM Orders
                  WHERE ShipperID = @id",
                new { id });

            return count > 0;
        }
    }
}