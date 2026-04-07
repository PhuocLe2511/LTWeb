using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.DataLayers.SqlServer;
using SV22T1020330.Models.Catalog;
using SV22T1020330.Models.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020330.BusinessLayers
{
    /// <summary>
    /// Cung cap các chuc nang xu ly du lieu lien quan den danh muc hàng hóa cua he thong, 
    /// bao gom: mat hàng (Product), thuoc tinh cua mat hàng (ProductAttribute) và anh cua mat hàng (ProductPhoto).
    /// </summary>
    public static class CatalogDataService
    {
        private static readonly IProductRepository productDB;
        private static readonly IGenericRepository<Category> categoryDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static CatalogDataService()
        {
            categoryDB = new CategoryRepository(Configuration.ConnectionString);
            productDB = new ProductRepository(Configuration.ConnectionString);
        }

        #region Category

        /// <summary>
        /// Tim kiem và lay danh sach loai hàng duoi dang phan trang.
        /// </summary>
        /// <param name="input">
        /// Thong tin tim kiem và phan trang (tu khoa tim kiem, trang can hien thi, so dòng moi trang).
        /// </param>
        /// <returns>
        /// Ket qua tim kiem duoi dang danh sach loai hàng co phan trang.
        /// </returns>
        public static async Task<PagedResult<Category>> ListCategoriesAsync(PaginationSearchInput input)
        {
            return await categoryDB.ListAsync(input);
        }

        /// <summary>
        /// Lay thong tin chi tiet cua mot loai hàng dua vao ma loai hàng.
        /// </summary>
        /// <param name="CategoryID">Ma loai hàng can tim.</param>
        /// <returns>
        /// Doi tuong Category neu tim thay, nguoc lai tra ve null.
        /// </returns>
        public static async Task<Category?> GetCategoryAsync(int CategoryID)
        {
            return await categoryDB.GetAsync(CategoryID);
        }

        /// <summary>
        /// Bo sung mot loai hàng moi vao he thong.
        /// </summary>
        /// <param name="data">Thong tin loai hàng can bo sung.</param>
        /// <returns>Ma loai hàng duoc tao moi.</returns>
        public static async Task<int> AddCategoryAsync(Category data)
        {
            // Ki?m tra du lieu hop le
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrWhiteSpace(data.CategoryName))
                throw new ArgumentException("Ten loai hàng không duoc de trong", nameof(data));

            if (data.CategoryName.Length > 100)
                throw new ArgumentException("Ten loai hàng không duoc vuot qua 100 ký tu", nameof(data));

            if (!string.IsNullOrWhiteSpace(data.Description) && data.Description.Length > 500)
                throw new ArgumentException("Mo ta loai hàng không duoc vuot qua 500 ký tu", nameof(data));

            return await categoryDB.AddAsync(data);
        }

        /// <summary>
        /// Cap nhat thong tin cua mot loai hàng.
        /// </summary>
        /// <param name="data">Thong tin loai hàng can cap nhat.</param>
        /// <returns>
        /// True neu cap nhat thanh cong, nguoc lai False.
        /// </returns>
        public static async Task<bool> UpdateCategoryAsync(Category data)
        {
            // Ki?m tra du lieu hop le
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.CategoryID <= 0)
                throw new ArgumentException("Ma loai hàng không hop le", nameof(data));

            if (string.IsNullOrWhiteSpace(data.CategoryName))
                throw new ArgumentException("Ten loai hàng không duoc de trong", nameof(data));

            if (data.CategoryName.Length > 100)
                throw new ArgumentException("Ten loai hàng không duoc vuot qua 100 ký tu", nameof(data));

            if (!string.IsNullOrWhiteSpace(data.Description) && data.Description.Length > 500)
                throw new ArgumentException("Mo ta loai hàng không duoc vuot qua 500 ký tu", nameof(data));

            // Ki?m tra loai hàng ton tai
            var existingCategory = await categoryDB.GetAsync(data.CategoryID);
            if (existingCategory == null)
                return false;

            return await categoryDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xoa mot loai hàng dua vao ma loai hàng.
        /// </summary>
        /// <param name="CategoryID">Ma loai hàng can xoa.</param>
        /// <returns>
        /// True neu xoa thanh cong, False neu loai hàng dang duoc su dung
        /// hoac viec xoa không thuc hien duoc.
        /// </returns>
        public static async Task<bool> DeleteCategoryAsync(int CategoryID)
        {
            if (await categoryDB.IsUsedAsync(CategoryID))
                return false;

            return await categoryDB.DeleteAsync(CategoryID);
        }

        /// <summary>
        /// Kiem tra xem mot loai hàng co dang duoc su dung trong du lieu hay không.
        /// </summary>
        /// <param name="CategoryID">Ma loai hàng can kiem tra.</param>
        /// <returns>
        /// True neu loai hàng dang duoc su dung, nguoc lai False.
        /// </returns>
        public static async Task<bool> IsUsedCategoryAsync(int CategoryID)
        {
            return await categoryDB.IsUsedAsync(CategoryID);
        }

        #endregion

        #region Product

        /// <summary>
        /// Tim kiem và lay danh sach mat hàng duoi dang phan trang.
        /// </summary>
        /// <param name="input">
        /// Thong tin tim kiem và phan trang mat hàng.
        /// </param>
        /// <returns>
        /// Ket qua tim kiem duoi dang danh sach mat hàng co phan trang.
        /// </returns>
        public static async Task<PagedResult<Product>> ListProductsAsync(ProductSearchInput input)
        {
            return await productDB.ListAsync(input);
        }

        /// <summary>
        /// Lay thong tin chi tiet cua mot mat hàng.
        /// </summary>
        /// <param name="productID">Ma mat hàng can tim.</param>
        /// <returns>
        /// Doi tuong Product neu tim thay, nguoc lai tra ve null.
        /// </returns>
        public static async Task<Product?> GetProductAsync(int productID)
        {
            return await productDB.GetAsync(productID);
        }

        /// <summary>
        /// Bo sung mot mat hàng moi vao he thong.
        /// </summary>
        /// <param name="data">Thong tin mat hàng can bo sung.</param>
        /// <returns>Ma mat hàng duoc tao moi.</returns>
        public static async Task<int> AddProductAsync(Product data)
        {
            // Ki?m tra du lieu hop le
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrWhiteSpace(data.ProductName))
                throw new ArgumentException("Ten hàng hóa không duoc de trong", nameof(data));

            if (data.ProductName.Length > 200)
                throw new ArgumentException("Ten hàng hóa không duoc vuot qua 200 ký tu", nameof(data));

            if (!string.IsNullOrWhiteSpace(data.ProductDescription) && data.ProductDescription.Length > 1000)
                throw new ArgumentException("Mo ta hàng hóa không duoc vuot qua 1000 ký tu", nameof(data));

            if (string.IsNullOrWhiteSpace(data.Unit))
                throw new ArgumentException("Don vi tính không duoc de trong", nameof(data));

            if (data.Unit.Length > 20)
                throw new ArgumentException("Don vi tính không duoc vuot qua 20 ký tu", nameof(data));

            if (data.Price < 0)
                throw new ArgumentException("Gia hàng hóa phai lon hon hoac bang 0", nameof(data));

            if (data.CategoryID.HasValue && data.CategoryID.Value <= 0)
                throw new ArgumentException("Ma loai hàng không hop le", nameof(data));

            if (data.SupplierID.HasValue && data.SupplierID.Value <= 0)
                throw new ArgumentException("Ma nhà cung cap không hop le", nameof(data));

            // Ki?m tra loai hàng ton tai (neu có)
            if (data.CategoryID.HasValue)
            {
                var category = await categoryDB.GetAsync(data.CategoryID.Value);
                if (category == null)
                    throw new Exception("Loai hàng không ton tai");
            }

            return await productDB.AddAsync(data);
        }

        /// <summary>
        /// Cap nhat thong tin cua mot mat hàng.
        /// </summary>
        /// <param name="data">Thong tin mat hàng can cap nhat.</param>
        /// <returns>
        /// True neu cap nhat thanh cong, nguoc lai False.
        /// </returns>
        public static async Task<bool> UpdateProductAsync(Product data)
        {
            // Ki?m tra du lieu hop le
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.ProductID <= 0)
                throw new ArgumentException("Ma hàng hóa không hop le", nameof(data));

            if (string.IsNullOrWhiteSpace(data.ProductName))
                throw new ArgumentException("Ten hàng hóa không duoc de trong", nameof(data));

            if (data.ProductName.Length > 200)
                throw new ArgumentException("Ten hàng hóa không duoc vuot qua 200 ký tu", nameof(data));

            if (!string.IsNullOrWhiteSpace(data.ProductDescription) && data.ProductDescription.Length > 1000)
                throw new ArgumentException("Mo ta hàng hóa không duoc vuot qua 1000 ký tu", nameof(data));

            if (string.IsNullOrWhiteSpace(data.Unit))
                throw new ArgumentException("Don vi tính không duoc de trong", nameof(data));

            if (data.Unit.Length > 20)
                throw new ArgumentException("Don vi tính không duoc vuot qua 20 ký tu", nameof(data));

            if (data.Price < 0)
                throw new ArgumentException("Gia hàng hóa phai lon hon hoac bang 0", nameof(data));

            if (data.CategoryID.HasValue && data.CategoryID.Value <= 0)
                throw new ArgumentException("Ma loai hàng không hop le", nameof(data));

            if (data.SupplierID.HasValue && data.SupplierID.Value <= 0)
                throw new ArgumentException("Ma nhà cung cap không hop le", nameof(data));

            // Ki?m tra hàng hóa ton tai
            var existingProduct = await productDB.GetAsync(data.ProductID);
            if (existingProduct == null)
                return false;

            // Ki?m tra loai hàng ton tai (neu có)
            if (data.CategoryID.HasValue)
            {
                var category = await categoryDB.GetAsync(data.CategoryID.Value);
                if (category == null)
                    throw new Exception("Loai hàng không ton tai");
            }

            return await productDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xoa mot mat hàng dua vao ma mat hàng.
        /// </summary>
        /// <param name="productID">Ma mat hàng can xoa.</param>
        /// <returns>
        /// True neu xoa thanh cong, False neu mat hàng dang duoc su dung
        /// hoac viec xoa không thuc hien duoc.
        /// </returns>
        public static async Task<bool> DeleteProductAsync(int productID)
        {
            if (await productDB.IsUsedAsync(productID))
                return false;

            return await productDB.DeleteAsync(productID);
        }

        /// <summary>
        /// Kiem tra xem mot mat hàng co dang duoc su dung trong du lieu hay không.
        /// </summary>
        /// <param name="productID">Ma mat hàng can kiem tra.</param>
        /// <returns>
        /// True neu mat hàng dang duoc su dung, nguoc lai False.
        /// </returns>
        public static async Task<bool> IsUsedProductAsync(int productID)
        {
            return await productDB.IsUsedAsync(productID);
        }

        #endregion

        #region ProductAttribute

        /// <summary>
        /// Lay danh sach các thuoc tinh cua mot mat hàng.
        /// </summary>
        /// <param name="productID">Ma mat hàng.</param>
        /// <returns>
        /// Danh sach các thuoc tinh cua mat hàng.
        /// </returns>
        public static async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            return await productDB.ListAttributesAsync(productID);
        }

        /// <summary>
        /// Lay thong tin chi tiet cua mot thuoc tinh cua mat hàng.
        /// </summary>
        /// <param name="attributeID">Ma thuoc tinh.</param>
        /// <returns>
        /// Doi tuong ProductAttribute neu tim thay, nguoc lai tra ve null.
        /// </returns>
        public static async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            return await productDB.GetAttributeAsync(attributeID);
        }

        /// <summary>
        /// Bo sung mot thuoc tinh moi cho mat hàng.
        /// </summary>
        /// <param name="data">Thong tin thuoc tinh can bo sung.</param>
        /// <returns>Ma thuoc tinh duoc tao moi.</returns>
        public static async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            // Ki?m tra du lieu hop le
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.ProductID <= 0)
                throw new ArgumentException("Ma san pham không hop le", nameof(data));

            if (string.IsNullOrWhiteSpace(data.AttributeName))
                throw new ArgumentException("Ten thuoc tinh không duoc de trong", nameof(data));

            if (data.AttributeName.Length > 200)
                throw new ArgumentException("Ten thuoc tinh không duoc vuot qua 200 ký tu", nameof(data));

            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                throw new ArgumentException("Gia tri thuoc tinh không duoc de trong", nameof(data));

            if (data.AttributeValue.Length > 500)
                throw new ArgumentException("Gia tri thuoc tinh không duoc vuot qua 500 ký tu", nameof(data));

            if (data.DisplayOrder < 0)
                throw new ArgumentException("Thu tu hien thi phai lon hon hoac bang 0", nameof(data));

            // Ki?m tra san pham ton tai
            var product = await productDB.GetAsync(data.ProductID);
            if (product == null)
                throw new Exception("San pham không ton tai");

            return await productDB.AddAttributeAsync(data);
        }

        /// <summary>
        /// Cap nhat thong tin cua mot thuoc tinh mat hàng.
        /// </summary>
        /// <param name="data">Thong tin thuoc tinh can cap nhat.</param>
        /// <returns>
        /// True neu cap nhat thanh cong, nguoc lai False.
        /// </returns>
        public static async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            // Ki?m tra du lieu hop le
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.AttributeID <= 0)
                throw new ArgumentException("Ma thuoc tinh không hop le", nameof(data));

            if (data.ProductID <= 0)
                throw new ArgumentException("Ma san pham không hop le", nameof(data));

            if (string.IsNullOrWhiteSpace(data.AttributeName))
                throw new ArgumentException("Ten thuoc tinh không duoc de trong", nameof(data));

            if (data.AttributeName.Length > 200)
                throw new ArgumentException("Ten thuoc tinh không duoc vuot qua 200 ký tu", nameof(data));

            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                throw new ArgumentException("Gia tri thuoc tinh không duoc de trong", nameof(data));

            if (data.AttributeValue.Length > 500)
                throw new ArgumentException("Gia tri thuoc tinh không duoc vuot qua 500 ký tu", nameof(data));

            if (data.DisplayOrder < 0)
                throw new ArgumentException("Thu tu hien thi phai lon hon hoac bang 0", nameof(data));

            // Ki?m tra thuoc tinh ton tai
            var existingAttribute = await productDB.GetAttributeAsync(data.AttributeID);
            if (existingAttribute == null)
                return false;

            return await productDB.UpdateAttributeAsync(data);
        }

        /// <summary>
        /// Xoa mot thuoc tinh cua mat hàng.
        /// </summary>
        /// <param name="attributeID">Ma thuoc tinh can xoa.</param>
        /// <returns>
        /// True neu xoa thanh cong, nguoc lai False.
        /// </returns>
        public static async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            // Ki?m tra attributeID hop le
            if (attributeID <= 0)
                throw new ArgumentException("Ma thuoc tinh không hop le", nameof(attributeID));

            // Ki?m tra thuoc tinh ton tai
            var existingAttribute = await productDB.GetAttributeAsync(attributeID);
            if (existingAttribute == null)
                return false;

            return await productDB.DeleteAttributeAsync(attributeID);
        }

        #endregion

        #region ProductPhoto

        /// <summary>
        /// Lay danh sach anh cua mot mat hàng.
        /// </summary>
        /// <param name="productID">Ma mat hàng.</param>
        /// <returns>
        /// Danh sach anh cua mat hàng.
        /// </returns>
        public static async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            return await productDB.ListPhotosAsync(productID);
        }

        /// <summary>
        /// Lay thong tin chi tiet cua mot anh cua mat hàng.
        /// </summary>
        /// <param name="photoID">Ma anh.</param>
        /// <returns>
        /// Doi tuong ProductPhoto neu tim thay, nguoc lai tra ve null.
        /// </returns>
        public static async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            return await productDB.GetPhotoAsync(photoID);
        }

        /// <summary>
        /// Bo sung mot anh moi cho mat hàng.
        /// </summary>
        /// <param name="data">Thong tin anh can bo sung.</param>
        /// <returns>Ma anh duoc tao moi.</returns>
        public static async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            // Ki?m tra du lieu hop le
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.ProductID <= 0)
                throw new ArgumentException("Ma san pham không hop le", nameof(data));

            // Photo field chi validate neu có gia tri (cho upload file)
            if (!string.IsNullOrWhiteSpace(data.Photo) && data.Photo.Length > 100)
                throw new ArgumentException("Ten file anh không duoc vuot qua 100 ký tu", nameof(data));

            if (!string.IsNullOrWhiteSpace(data.Description) && data.Description.Length > 500)
                throw new ArgumentException("Mo ta anh không duoc vuot qua 500 ký tu", nameof(data));

            if (data.DisplayOrder < 0)
                throw new ArgumentException("Thu tu hien thi phai lon hon hoac bang 0", nameof(data));

            // Ki?m tra san pham ton tai
            var product = await productDB.GetAsync(data.ProductID);
            if (product == null)
                throw new Exception("San pham không ton tai");

            return await productDB.AddPhotoAsync(data);
        }

        /// <summary>
        /// Cap nhat thong tin cua mot anh mat hàng.
        /// </summary>
        /// <param name="data">Thong tin anh can cap nhat.</param>
        /// <returns>
        /// True neu cap nhat thanh cong, nguoc lai False.
        /// </returns>
        public static async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            // Ki?m tra du lieu hop le
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.PhotoID <= 0)
                throw new ArgumentException("Ma anh không hop le", nameof(data));

            if (data.ProductID <= 0)
                throw new ArgumentException("Ma san pham không hop le", nameof(data));

            // Photo field chi validate neu có gia tri
            if (!string.IsNullOrWhiteSpace(data.Photo) && data.Photo.Length > 100)
                throw new ArgumentException("Ten file anh không duoc vuot qua 100 ký tu", nameof(data));

            if (!string.IsNullOrWhiteSpace(data.Description) && data.Description.Length > 500)
                throw new ArgumentException("Mo ta anh không duoc vuot qua 500 ký tu", nameof(data));

            if (data.DisplayOrder < 0)
                throw new ArgumentException("Thu tu hien thi phai lon hon hoac bang 0", nameof(data));

            // Ki?m tra anh ton tai
            var existingPhoto = await productDB.GetPhotoAsync(data.PhotoID);
            if (existingPhoto == null)
                return false;

            return await productDB.UpdatePhotoAsync(data);
        }

        /// <summary>
        /// Xoa mot anh cua mat hàng.
        /// </summary>
        /// <param name="photoID">Ma anh can xoa.</param>
        /// <returns>
        /// True neu xoa thanh cong, nguoc lai False.
        /// </returns>
        public static async Task<bool> DeletePhotoAsync(long photoID)
        {
            // Ki?m tra photoID hop le
            if (photoID <= 0)
                throw new ArgumentException("Ma anh không hop le", nameof(photoID));

            // Ki?m tra anh ton tai
            var existingPhoto = await productDB.GetPhotoAsync(photoID);
            if (existingPhoto == null)
                return false;

            return await productDB.DeletePhotoAsync(photoID);
        }

        #endregion
    }
}
