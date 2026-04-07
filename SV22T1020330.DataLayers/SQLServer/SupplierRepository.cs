using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Partner;

namespace SV22T1020330.DataLayers.SqlServer
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly string _connectionString;

        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Supplier>(
                @"SELECT * FROM Suppliers WHERE SupplierID = @id", new { id });
        }

        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Suppliers (SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);
            int rows = await connection.ExecuteAsync(
                @"UPDATE Suppliers 
                  SET SupplierName = @SupplierName,
                      ContactName = @ContactName,
                      Province = @Province,
                      Address = @Address,
                      Phone = @Phone,
                      Email = @Email
                  WHERE SupplierID = @SupplierID", data);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Suppliers WHERE SupplierID = @id", new { id });
            return rows > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM Products WHERE SupplierID = @id", new { id });
            return count > 0;
        }

        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            
            const string where = @"WHERE (@search = '' OR SupplierName LIKE @search OR ContactName LIKE @search)";
            
            var sql = $@"
                SELECT COUNT(*) FROM Suppliers {where};
                SELECT * FROM Suppliers {where}
                ORDER BY SupplierName
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            var param = new
            {
                search = $"%{input.SearchValue}%",
                offset = (input.Page - 1) * input.PageSize,
                pageSize = input.PageSize
            };

            using var multi = await connection.QueryMultipleAsync(sql, param);
            var count = (await multi.ReadAsync<int>()).Single();
            var data = (await multi.ReadAsync<Supplier>()).ToList();

            return new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data
            };
        }
    }
}
