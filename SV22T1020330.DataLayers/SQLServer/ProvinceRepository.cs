using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.DataDictionary;

namespace SV22T1020330.DataLayers.SqlServer
{
    /// <summary>
    /// Lớp thực hiện truy vấn dữ liệu danh mục Tỉnh/Thành phố
    /// từ cơ sở dữ liệu SQL Server
    /// </summary>
    /// <remarks>
    /// Cài đặt interface IDataDictionaryRepository với kiểu dữ liệu Province.
    /// Chỉ cung cấp chức năng lấy toàn bộ danh sách (không phân trang).
    /// Sử dụng Dapper để truy vấn dữ liệu.
    /// </remarks>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo ProvinceRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ tỉnh/thành
        /// </summary>
        /// <returns>Danh sách Province</returns>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<Province>(
                @"SELECT ProvinceName
                  FROM Provinces
                  ORDER BY ProvinceName");

            return data.ToList();
        }
    }
}