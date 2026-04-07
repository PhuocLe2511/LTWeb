using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Security;

namespace SV22T1020330.DataLayers.SQLServer
{
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;
        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CAST(CustomerID AS nvarchar(20)) AS UserId,
                       Email AS UserName,
                       CustomerName AS DisplayName,
                       Email,
                       '' AS Photo,
                       N'Customer' AS RoleNames
                FROM Customers
                WHERE Email = @userName AND Password = @password
                  AND ISNULL(IsLocked, 0) = 0";

            return await connection.QuerySingleOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Customers SET Password=@password WHERE Email=@userName";
            int rows = await connection.ExecuteAsync(sql, new { userName, password });
            return rows > 0;
        }

        // ✅ Thêm register
        public async Task<bool> RegisterAsync(string customerName, string email, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string checkSql = "SELECT COUNT(*) FROM Customers WHERE Email=@Email";
            int exist = await connection.ExecuteScalarAsync<int>(checkSql, new { Email = email });
            if (exist > 0) return false; // Email đã tồn tại

            string sql = @"INSERT INTO Customers (CustomerName, ContactName, Email, Password) 
                           VALUES (@CustomerName, @ContactName, @Email, @Password)";
            int rows = await connection.ExecuteAsync(sql, new { CustomerName = customerName, ContactName = customerName, Email = email, Password = password });
            return rows > 0;
        }
    }
}