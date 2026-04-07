using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Security;

namespace SV22T1020330.DataLayers.SqlServer
{
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;
        
        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CAST(EmployeeID AS nvarchar(20)) AS UserId,
                       Email AS UserName,
                       FullName AS DisplayName,
                       Email,
                       ISNULL(Photo, '') AS Photo,
                       ISNULL(RoleNames, '') AS RoleNames
                FROM Employees
                WHERE Email = @userName AND Password = @password AND IsWorking = 1";

            return await connection.QuerySingleOrDefaultAsync<UserAccount>(
                sql, new { userName, password });
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Employees SET Password=@password WHERE Email=@userName";
            int rows = await connection.ExecuteAsync(sql, new { userName, password });
            return rows > 0;
        }

        public async Task<bool> RegisterAsync(string customerName, string email, string password)
        {
            // Employee không cần register function
            return false;
        }
    }
}
