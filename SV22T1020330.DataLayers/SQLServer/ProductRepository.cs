using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Catalog;
using SV22T1020330.Models.Common;

namespace SV22T1020330.DataLayers.SqlServer
{
    /// <summary>
    /// Thực hiện các thao tác dữ liệu cho mặt hàng (Products),
    /// thuộc tính (ProductAttributes) và ảnh (ProductPhotos)
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region ===== PRODUCT =====

        /// <summary>
        /// Tìm kiếm + phân trang danh sách sản phẩm
        /// </summary>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            const string where = @"WHERE (@search = '' OR ProductName LIKE @search)
                             AND (@categoryID = 0 OR CategoryID = @categoryID)
                             AND (@supplierID = 0 OR SupplierID = @supplierID)
                             AND (@minPrice = 0 OR Price >= @minPrice)
                             AND (@maxPrice = 0 OR Price <= @maxPrice)
                             AND (@onlySelling = 0 OR IsSelling = 1)";

            // Một lần gọi SQL: COUNT + trang dữ liệu (giảm độ trễ mạng so với 2 round-trip)
            var sql = $@"
SELECT COUNT(*) FROM Products {where};
SELECT * FROM Products {where}
ORDER BY ProductName
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            var param = new
            {
                search = $"%{input.SearchValue}%",
                categoryID = input.CategoryID,
                supplierID = input.SupplierID,
                minPrice = input.MinPrice,
                maxPrice = input.MaxPrice,
                onlySelling = input.OnlySelling ? 1 : 0,
                offset = (input.Page - 1) * input.PageSize,
                pageSize = input.PageSize
            };

            using var multi = await connection.QueryMultipleAsync(sql, param);
            var count = (await multi.ReadAsync<int>()).Single();
            var data = (await multi.ReadAsync<Product>()).ToList();

            return new PagedResult<Product>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin 1 sản phẩm
        /// </summary>
        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<Product>(
                @"SELECT * FROM Products WHERE ProductID = @productID",
                new { productID });
        }

        /// <summary>
        /// Thêm sản phẩm
        /// </summary>
        public async Task<int> AddAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Products
                (ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                VALUES
                (@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật sản phẩm
        /// </summary>
        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE Products
                  SET ProductName = @ProductName,
                      ProductDescription = @ProductDescription,
                      SupplierID = @SupplierID,
                      CategoryID = @CategoryID,
                      Unit = @Unit,
                      Price = @Price,
                      Photo = @Photo,
                      IsSelling = @IsSelling
                  WHERE ProductID = @ProductID",
                data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa sản phẩm (xóa luôn attribute + photo trước)
        /// </summary>
        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.ExecuteAsync(
                @"DELETE FROM ProductAttributes WHERE ProductID = @productID",
                new { productID });

            await connection.ExecuteAsync(
                @"DELETE FROM ProductPhotos WHERE ProductID = @productID",
                new { productID });

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Products WHERE ProductID = @productID",
                new { productID });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra sản phẩm có đang được sử dụng hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM OrderDetails WHERE ProductID = @productID",
                new { productID });

            return count > 0;
        }

        #endregion

        #region ===== ATTRIBUTE =====

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<ProductAttribute>(
                @"SELECT * FROM ProductAttributes
                  WHERE ProductID = @productID
                  ORDER BY DisplayOrder",
                new { productID });

            return data.ToList();
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(
                @"SELECT * FROM ProductAttributes WHERE AttributeID = @attributeID",
                new { attributeID });
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.ExecuteScalarAsync<long>(
                @"INSERT INTO ProductAttributes (ProductID, AttributeName, AttributeValue, DisplayOrder)
                  VALUES (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                  SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                data);
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE ProductAttributes
                  SET AttributeName = @AttributeName,
                      AttributeValue = @AttributeValue,
                      DisplayOrder = @DisplayOrder
                  WHERE AttributeID = @AttributeID",
                data);

            return rows > 0;
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM ProductAttributes WHERE AttributeID = @attributeID",
                new { attributeID });

            return rows > 0;
        }

        #endregion

        #region ===== PHOTO =====

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<ProductPhoto>(
                @"SELECT * FROM ProductPhotos
                  WHERE ProductID = @productID
                  ORDER BY DisplayOrder",
                new { productID });

            return data.ToList();
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(
                @"SELECT * FROM ProductPhotos WHERE PhotoID = @photoID",
                new { photoID });
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.ExecuteScalarAsync<long>(
                @"INSERT INTO ProductPhotos (ProductID, Photo, Description, DisplayOrder, IsHidden)
                  VALUES (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                  SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                data);
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE ProductPhotos
                  SET Photo = @Photo,
                      Description = @Description,
                      DisplayOrder = @DisplayOrder,
                      IsHidden = @IsHidden
                  WHERE PhotoID = @PhotoID",
                data);

            return rows > 0;
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM ProductPhotos WHERE PhotoID = @photoID",
                new { photoID });

            return rows > 0;
        }

        #endregion
    }
}