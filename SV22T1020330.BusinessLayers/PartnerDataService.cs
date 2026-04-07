using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.DataLayers.SqlServer;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.Partner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020330.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng tác nghiệp liên quan đến dữ liệu của các đối tác (Customer, Supplier, Shipper, v.v.)
    /// </summary>
    public static class PartnerDataService
    {
        private static readonly IGenericRepository<Supplier> SupplierDB;
        private static readonly IGenericRepository<Shipper> ShipperDB;
        private static readonly ICustomerRepository CustomerDB;



        /// <summary>
        /// Constructor tĩnh để khởi tạo các đối tượng truy cập dữ liệu cho các đối tác
        /// </summary>
        static PartnerDataService()
        {
            SupplierDB = new SupplierRepository(Configuration.ConnectionString);
            ShipperDB = new ShipperRepository(Configuration.ConnectionString);
            CustomerDB = new CustomerRepository(Configuration.ConnectionString);
        }
        //== CÁC CHỨC NĂNG LIÊN QUAN ĐẾN NHÀ CUNG CẤP (SUPPLIER) ==//

        public static async Task<PagedResult<Supplier>> ListSuppliersAsync(PaginationSearchInput input)
        {
            return await SupplierDB.ListAsync(input);
        }
        /// <summary>
        /// Lấy thông tinmootj nhà cung cấp dựa vào mã nhà cung cấp (SupplierID)
        /// </summary>
        /// <param name="SupplierID"></param>
        /// <returns></returns>
        public static async Task<Supplier?> GetSupplierAsync(int SupplierID)
        {
            
            return await SupplierDB.GetAsync(SupplierID);
        }
        /// <summary>
        /// Thêm mới một nhà cung cấp vào hệ thống sau khi kiểm tra tính hợp lệ của dữ liệu
        /// </summary>
        /// <param name="supplier">Thông tin nhà cung cấp cần bổ sung.</param>
        /// <returns>ID của nhà cung cấp được tạo mới.</returns>
        /// <remarks>
        /// Validation Rules:
        /// - Supplier không được null (ArgumentNullException)
        /// - SupplierName: Bắt buộc, không rỗng, max 100 ký tự (Exception)
        /// - ContactName: Không bắt buộc, nhưng nếu có thì max 100 ký tự (Exception)
        /// - Phone: Không bắt buộc, nhưng nếu có thì max 20 ký tự (Exception)
        /// - Email: Không bắt buộc, nhưng nếu có phải valid format (Exception)
        /// - Address: Không bắt buộc, nhưng nếu có thì max 500 ký tự (Exception)
        /// 
        /// Business Logic:
        /// - Kiểm tra toàn bộ data trước khi insert
        /// - Validate email format với regex pattern
        /// - Đảm bảo data quality và consistency
        /// - Repository pattern: delegate actual operations cho SupplierRepository
        /// 
        /// Data Integrity:
        /// - Prevent invalid supplier data
        /// - Ensure required fields are filled
        /// - Validate email format để avoid communication issues
        /// - Limit string lengths để avoid database errors
        /// 
        /// Error Handling:
        /// - ArgumentNullException: supplier object null
        /// - Exception: validation failures với message rõ ràng
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Usage Examples:
        /// var supplier = new Supplier 
        /// { 
        ///     SupplierName = "Công ty ABC",
        ///     ContactName = "Nguyễn Văn A",
        ///     Phone = "0901234567",
        ///     Email = "contact@abc.com"
        /// };
        /// var supplierId = await PartnerDataService.AddSupplierAsync(supplier);
        /// </remarks>
        public static async Task<int> AddSupplierAsync(Supplier supplier)
        {
            // Validation: Check null
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            // Validation: SupplierName (Required)
            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                throw new Exception("Tên nhà cung cấp không được để trống");

            if (supplier.SupplierName.Length > 100)
                throw new Exception("Tên nhà cung cấp không được vượt quá 100 ký tự");

            // Validation: ContactName (Optional but limited)
            if (!string.IsNullOrWhiteSpace(supplier.ContactName) && supplier.ContactName.Length > 100)
                throw new Exception("Tên giao dịch không được vượt quá 100 ký tự");

            // Validation: Phone (Optional but limited)
            if (!string.IsNullOrWhiteSpace(supplier.Phone))
            {
                if (supplier.Phone.Length > 20)
                    throw new Exception("Số điện thoại không được vượt quá 20 ký tự");
                
                // Basic phone format validation (digits, spaces, hyphens, plus)
                if (!System.Text.RegularExpressions.Regex.IsMatch(supplier.Phone, @"^[0-9\s\-\+\(\)]+$"))
                    throw new Exception("Số điện thoại không hợp lệ. Chỉ cho phép số, dấu -, +, và ()");
            }

            // Validation: Email (Optional but must be valid if provided)
            if (!string.IsNullOrWhiteSpace(supplier.Email))
            {
                if (supplier.Email.Length > 100)
                    throw new Exception("Email không được vượt quá 100 ký tự");
                
                // Email format validation
                if (!System.Text.RegularExpressions.Regex.IsMatch(supplier.Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                    throw new Exception("Email không đúng định dạng");
            }

            // Validation: Address (Optional but limited)
            if (!string.IsNullOrWhiteSpace(supplier.Address) && supplier.Address.Length > 500)
                throw new Exception("Địa chỉ không được vượt quá 500 ký tự");

            // Validation: Province (Optional but limited)
            if (!string.IsNullOrWhiteSpace(supplier.Province) && supplier.Province.Length > 100)
                throw new Exception("Tỉnh/Thành phố không được vượt quá 100 ký tự");

            return await SupplierDB.AddAsync(supplier);
        }
        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp sau khi kiểm tra tính hợp lệ của dữ liệu
        /// </summary>
        /// <param name="supplier">Thông tin nhà cung cấp cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy nhà cung cấp.</returns>
        /// <remarks>
        /// Validation Rules:
        /// - Supplier không được null (ArgumentNullException)
        /// - SupplierID phải > 0 (ArgumentException)
        /// - SupplierName: Bắt buộc, không rỗng, max 100 ký tự (Exception)
        /// - ContactName: Không bắt buộc, nhưng nếu có thì max 100 ký tự (Exception)
        /// - Phone: Không bắt buộc, nhưng nếu có thì max 20 ký tự và valid format (Exception)
        /// - Email: Không bắt buộc, nhưng nếu có phải valid format (Exception)
        /// - Address: Không bắt buộc, nhưng nếu có thì max 500 ký tự (Exception)
        /// - Province: Không bắt buộc, nhưng nếu có thì max 100 ký tự (Exception)
        /// - Supplier phải tồn tại trong database (return false nếu không tìm thấy)
        /// 
        /// Business Logic:
        /// - Kiểm tra toàn bộ data trước khi update
        /// - Đảm bảo supplier tồn tại trước khi cập nhật
        /// - Validate email và phone format với regex patterns
        /// - Repository pattern: delegate actual update operations cho SupplierRepository
        /// 
        /// Data Integrity:
        /// - Prevent invalid supplier updates
        /// - Ensure supplier exists before modification
        /// - Maintain data consistency across system
        /// - Validate communication data (email, phone)
        /// 
        /// Error Handling:
        /// - ArgumentNullException: supplier object null
        /// - ArgumentException: invalid SupplierID
        /// - Exception: validation failures với message rõ ràng
        /// - Return false: supplier không tồn tại hoặc update failed
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Usage Examples:
        /// var supplier = new Supplier 
        /// { 
        ///     SupplierID = 1,
        ///     SupplierName = "Công ty ABC Cập nhật",
        ///     Phone = "0901234567"
        /// };
        /// var result = await PartnerDataService.UpdateSupplierAsync(supplier);
        /// if (!result) { /* handle failure */ }
        /// </remarks>
        public static async Task<bool> UpdateSupplierAsync(Supplier supplier)
        {
            // Validation: Check null
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            // Validation: Check SupplierID
            if (supplier.SupplierID <= 0)
                throw new ArgumentException("Mã nhà cung cấp không hợp lệ", nameof(supplier));

            // Validation: Check if supplier exists
            var existingSupplier = await SupplierDB.GetAsync(supplier.SupplierID);
            if (existingSupplier == null)
                return false;

            // Validation: SupplierName (Required)
            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                throw new Exception("Tên nhà cung cấp không được để trống");

            if (supplier.SupplierName.Length > 100)
                throw new Exception("Tên nhà cung cấp không được vượt quá 100 ký tự");

            // Validation: ContactName (Optional but limited)
            if (!string.IsNullOrWhiteSpace(supplier.ContactName) && supplier.ContactName.Length > 100)
                throw new Exception("Tên giao dịch không được vượt quá 100 ký tự");

            // Validation: Phone (Optional but limited)
            if (!string.IsNullOrWhiteSpace(supplier.Phone))
            {
                if (supplier.Phone.Length > 20)
                    throw new Exception("Số điện thoại không được vượt quá 20 ký tự");
                
                // Basic phone format validation
                if (!System.Text.RegularExpressions.Regex.IsMatch(supplier.Phone, @"^[0-9\s\-\+\(\)]+$"))
                    throw new Exception("Số điện thoại không hợp lệ. Chỉ cho phép số, dấu -, +, và ()");
            }

            // Validation: Email (Optional but must be valid if provided)
            if (!string.IsNullOrWhiteSpace(supplier.Email))
            {
                if (supplier.Email.Length > 100)
                    throw new Exception("Email không được vượt quá 100 ký tự");
                
                // Email format validation
                if (!System.Text.RegularExpressions.Regex.IsMatch(supplier.Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                    throw new Exception("Email không đúng định dạng");
            }

            // Validation: Address (Optional but limited)
            if (!string.IsNullOrWhiteSpace(supplier.Address) && supplier.Address.Length > 500)
                throw new Exception("Địa chỉ không được vượt quá 500 ký tự");

            // Validation: Province (Optional but limited)
            if (!string.IsNullOrWhiteSpace(supplier.Province) && supplier.Province.Length > 100)
                throw new Exception("Tỉnh/Thành phố không được vượt quá 100 ký tự");

            return await SupplierDB.UpdateAsync(supplier);
        }
        public static async Task<bool> DeleteSupplierAsync(int SupplierID)
        {
            if(await SupplierDB.IsUsedAsync(SupplierID))// đổi thành IsUsedAsync
            {
                return false;
            }
            return await SupplierDB.DeleteAsync(SupplierID);
        }
        /// <summary>
        /// kiểm tra xem một nhà cung cấp hiện cí mặt hàng liên quan hay không
        /// (kiểm tra xem có cho phép xoá hay không)
        /// </summary>
        /// <param name="SupplierID"></param>
        /// <returns></returns>

        public static async Task<bool> IsSupplierUsedAsync(int SupplierID)
        {
            return await SupplierDB.IsUsedAsync(SupplierID);// đổi thành IsUsedAsync
        }


        //== CÁC CHỨC NĂNG LIÊN QUAN ĐẾN NHÀ VẬN CHUYỂN (SHIPPER) ==//
        //== CÁC CHỨC NĂNG LIÊN QUAN ĐẾN NHÀ VẬN CHUYỂN (SHIPPER) ==//


        /// <summary>
        /// Lấy danh sách nhà vận chuyển có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin phân trang và giá trị tìm kiếm</param>
        /// <returns>Danh sách shipper</returns>
        public static async Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput input)
        {
            return await ShipperDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin một nhà vận chuyển dựa vào mã ShipperID
        /// </summary>
        /// <param name="ShipperID">Mã nhà vận chuyển</param>
        /// <returns>Thông tin shipper</returns>
        public static async Task<Shipper?> GetShipperAsync(int ShipperID)
        {
            return await ShipperDB.GetAsync(ShipperID);
        }

        /// <summary>
        /// Bổ sung một nhà vận chuyển mới
        /// </summary>
        /// <param name="shipper">Thông tin shipper cần thêm</param>
        /// <returns>ID của shipper được thêm</returns>
        public static async Task<int> AddShipperAsync(Shipper shipper)
        {
            if (shipper == null)
                throw new ArgumentNullException(nameof(shipper));

            // 1. Tên bắt buộc
            if (string.IsNullOrWhiteSpace(shipper.ShipperName))
                throw new Exception("Tên người giao hàng không được để trống");

            if (shipper.ShipperName.Length > 100)
                throw new Exception("Tên người giao hàng không vượt quá 100 ký tự");

            // 2. Phone (không bắt buộc nhưng nếu có thì check)
            if (!string.IsNullOrWhiteSpace(shipper.Phone))
            {
                if (shipper.Phone.Length > 20)
                    throw new Exception("Số điện thoại không hợp lệ");
            }

            return await ShipperDB.AddAsync(shipper);
        }

        /// <summary>
        /// Cập nhật thông tin nhà vận chuyển
        /// </summary>
        /// <param name="shipper">Thông tin shipper cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public static async Task<bool> UpdateShipperAsync(Shipper shipper)
        {
            if (shipper == null)
                throw new ArgumentNullException(nameof(shipper));

            // ID bắt buộc hợp lệ
            if (shipper.ShipperID <= 0)
                throw new Exception("Mã người giao hàng không hợp lệ");

            // Tên bắt buộc
            if (string.IsNullOrWhiteSpace(shipper.ShipperName))
                throw new Exception("Tên người giao hàng không được để trống");

            if (shipper.ShipperName.Length > 100)
                throw new Exception("Tên người giao hàng không vượt quá 100 ký tự");

            // Phone
            if (!string.IsNullOrWhiteSpace(shipper.Phone) && shipper.Phone.Length > 20)
                throw new Exception("Số điện thoại không hợp lệ");

            return await ShipperDB.UpdateAsync(shipper);
        }

        /// <summary>
        /// Xóa một nhà vận chuyển
        /// </summary>
        /// <param name="ShipperID">Mã nhà vận chuyển cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public static async Task<bool> DeleteShipperAsync(int ShipperID)
        {
            if (await ShipperDB.IsUsedAsync(ShipperID))
                return false;

            return await ShipperDB.DeleteAsync(ShipperID);
        }

        /// <summary>
        /// Kiểm tra xem nhà vận chuyển có đang được sử dụng hay không
        /// </summary>
        /// <param name="ShipperID">Mã shipper</param>
        /// <returns>true nếu đang được sử dụng</returns>
        public static async Task<bool> IsShipperUsedAsync(int ShipperID)
        {
            return await ShipperDB.IsUsedAsync(ShipperID);
        }


        //== CÁC CHỨC NĂNG LIÊN QUAN ĐẾN KHÁCH HÀNG (CUSTOMER) ==//
        //== CÁC CHỨC NĂNG LIÊN QUAN ĐẾN KHÁCH HÀNG (CUSTOMER) ==//

        /// <summary>
        /// Lấy danh sách khách hàng có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin phân trang và giá trị tìm kiếm</param>
        /// <returns>Danh sách khách hàng</returns>
        public static async Task<PagedResult<Customer>> ListCustomersAsync(PaginationSearchInput input)
        {
            return await CustomerDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin một khách hàng dựa vào mã CustomerID
        /// </summary>
        /// <param name="CustomerID">Mã khách hàng</param>
        /// <returns>Thông tin khách hàng</returns>
        public static async Task<Customer?> GetCustomerAsync(int CustomerID)
        {
            return await CustomerDB.GetAsync(CustomerID);
        }

        /// <summary>
        /// Bổ sung một khách hàng mới
        /// </summary>
        /// <param name="customer">Thông tin khách hàng</param>
        /// <returns>ID khách hàng được thêm</returns>
        public static async Task<int> AddCustomerAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            // 1. Tên khách hàng (bắt buộc)
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                throw new Exception("Tên khách hàng không được để trống");

            if (customer.CustomerName.Length > 100)
                throw new Exception("Tên khách hàng không vượt quá 100 ký tự");

            // 2. Tên giao dịch
            if (!string.IsNullOrWhiteSpace(customer.ContactName) && customer.ContactName.Length > 100)
                throw new Exception("Tên giao dịch không vượt quá 100 ký tự");

            // 3. Email (BẮT BUỘC)
            if (string.IsNullOrWhiteSpace(customer.Email))
                throw new Exception("Email không được để trống");

            if (!customer.Email.Contains("@"))
                throw new Exception("Email không hợp lệ");

            // 🔥 Quan trọng: check trùng email
            if (!await CustomerDB.ValidateEmailAsync(customer.Email))
                throw new Exception("Email đã tồn tại");

            // 4. Phone
            if (!string.IsNullOrWhiteSpace(customer.Phone) && customer.Phone.Length > 20)
                throw new Exception("Số điện thoại không hợp lệ");

            // 5. Address
            if (!string.IsNullOrWhiteSpace(customer.Address) && customer.Address.Length > 200)
                throw new Exception("Địa chỉ quá dài");

            if (!string.IsNullOrWhiteSpace(customer.Province) && customer.Province.Length > 50)
                throw new Exception("Tỉnh/thành quá dài");

            return await CustomerDB.AddAsync(customer);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="customer">Thông tin khách hàng</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public static async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            // 1. ID hợp lệ
            if (customer.CustomerID <= 0)
                throw new Exception("Mã khách hàng không hợp lệ");

            // 2. Tên
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                throw new Exception("Tên khách hàng không được để trống");

            if (customer.CustomerName.Length > 100)
                throw new Exception("Tên khách hàng không vượt quá 100 ký tự");

            // 3. Email
            if (string.IsNullOrWhiteSpace(customer.Email))
                throw new Exception("Email không được để trống");

            if (!customer.Email.Contains("@"))
                throw new Exception("Email không hợp lệ");

            // 🔥 Check trùng nhưng loại trừ chính nó
            if (!await CustomerDB.ValidateEmailAsync(customer.Email, customer.CustomerID))
                throw new Exception("Email đã tồn tại");

            // 4. Phone
            if (!string.IsNullOrWhiteSpace(customer.Phone) && customer.Phone.Length > 20)
                throw new Exception("Số điện thoại không hợp lệ");

            return await CustomerDB.UpdateAsync(customer);
        }

        /// <summary>
        /// Xóa một khách hàng
        /// </summary>
        /// <param name="CustomerID">Mã khách hàng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public static async Task<bool> DeleteCustomerAsync(int CustomerID)
        {
            if (await CustomerDB.IsUsedAsync(CustomerID))
                return false;

            return await CustomerDB.DeleteAsync(CustomerID);
        }

        /// <summary>
        /// Kiểm tra xem khách hàng có dữ liệu liên quan hay không
        /// </summary>
        /// <param name="CustomerID">Mã khách hàng</param>
        /// <returns>true nếu đang được sử dụng</returns>
        public static async Task<bool> IsCustomerUsedAsync(int CustomerID)
        {
            return await CustomerDB.IsUsedAsync(CustomerID);
        }
        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ cho khách hàng hay không
        /// </summary>
        /// <param name="email">Địa chỉ email cần kiểm tra.</param>
        /// <param name="CustomerID">
        /// Mã khách hàng (mặc định = 0). 
        /// Nếu bằng 0: kiểm tra email cho khách hàng mới.
        /// Nếu khác 0: kiểm tra email cho khách hàng hiện có (loại trừ chính nó).
        /// </param>
        /// <returns>True nếu email hợp lệ (chưa tồn tại), False nếu email đã tồn tại.</returns>
        /// <remarks>
        /// Validation Logic:
        /// - Email không được rỗng hoặc chỉ chứa whitespace
        /// - Email phải đúng định dạng (regex validation)
        /// - Email không được trùng với khách hàng khác trong database
        /// 
        /// Business Rules:
        /// - Mỗi khách hàng chỉ có một email duy nhất
        /// - Email là bắt buộc cho khách hàng (required field)
        /// - Khi cập nhật: cho phép giữ lại email hiện tại của khách hàng
        /// - Khi thêm mới: email phải hoàn toàn mới
        /// 
        /// Database Operations:
        /// - Gọi CustomerDB.ValidateEmailAsync(email, CustomerID)
        /// - Repository pattern: delegate actual database query
        /// - SQL: SELECT COUNT(*) FROM Customers WHERE Email = @Email AND CustomerID != @CustomerID
        /// 
        /// Use Cases:
        /// - Register new customer: CustomerID = 0
        /// - Update existing customer: CustomerID = existing ID
        /// - Form validation: real-time email availability check
        /// 
        /// Error Handling:
        /// - Return false cho email rỗng hoặc invalid format
        /// - Return false nếu email đã tồn tại
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Security:
        /// - Prevent email spoofing và duplicate accounts
        /// - Ensure unique identification cho mỗi khách hàng
        /// - Validate input để avoid SQL injection
        /// 
        /// Usage Examples:
        /// // Check email for new customer
        /// bool isValid = await ValidateCustomerEmailAsync("new@example.com");
        /// 
        /// // Check email for existing customer update
        /// bool isValid = await ValidateCustomerEmailAsync("update@example.com", 123);
        /// </remarks>
        public static async Task<bool> ValidateCustomerEmailAsync(string email, int CustomerID = 0)
        {
            // Validation: Check if email is provided
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Validation: Check email format using regex
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                return false;

            // Delegate to repository for database validation
            return await CustomerDB.ValidateEmailAsync(email, CustomerID);
        }
    }
}
