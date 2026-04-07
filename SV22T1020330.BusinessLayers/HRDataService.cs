using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.DataLayers.SqlServer;
using SV22T1020330.Models.Common;
using SV22T1020330.Models.HR;
namespace SV22T1020330.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nhân sự của hệ thống    
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static HRDataService()
        {
            employeeDB = new SV22T1020330.DataLayers.SqlServer.EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang.
        /// </summary>
        /// <param name="input">
        /// Thông tin tìm kiếm và phân trang (từ khóa tìm kiếm, trang cần hiển thị, số dòng mỗi trang).
        /// </param>
        /// <returns>
        /// Kết quả tìm kiếm dưới dạng danh sách nhân viên có phân trang.
        /// </returns>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần tìm.</param>
        /// <returns>
        /// Đối tượng Employee nếu tìm thấy, ngược lại trả về null.
        /// </returns>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            return await employeeDB.GetAsync(employeeID);
        }

        /// <summary>
        /// Thêm mới một nhân viên vào hệ thống sau khi kiểm tra tính hợp lệ của dữ liệu
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần bổ sung.</param>
        /// <returns>Mã nhân viên được tạo mới.</returns>
        /// <remarks>
        /// Validation Rules:
        /// - Employee không được null (ArgumentNullException)
        /// - FullName: Bắt buộc, không rỗng, max 100 ký tự (Exception)
        /// - Email: Bắt buộc, phải valid format và unique (Exception)
        /// - Phone: Không bắt buộc, nhưng nếu có thì max 20 ký tự và valid format (Exception)
        /// - BirthDate: Không bắt buộc, nhưng nếu có phải là ngày hợp lệ và không phải ngày tương lai (Exception)
        /// - Address: Không bắt buộc, nhưng nếu có thì max 500 ký tự (Exception)
        /// 
        /// Business Logic:
        /// - Kiểm tra toàn bộ data trước khi insert
        /// - Validate email uniqueness với ValidateEmployeeEmailAsync
        /// - Đảm bảo dates hợp lệ và logic
        /// - Repository pattern: delegate actual operations cho EmployeeRepository
        /// 
        /// Data Integrity:
        /// - Prevent invalid employee data
        /// - Ensure email uniqueness across employees
        /// - Validate date logic để avoid impossible scenarios
        /// - Ensure reasonable age limits
        /// 
        /// Error Handling:
        /// - ArgumentNullException: data object null
        /// - Exception: validation failures với message rõ ràng
        /// - SqlException: database errors (handled by repository)
        /// 
        /// Usage Examples:
        /// var employee = new Employee 
        /// { 
        ///     FullName = "Nguyễn Văn A",
        ///     Email = "employee@company.com",
        ///     BirthDate = new DateTime(1990, 1, 1),
        ///     Phone = "0901234567"
        /// };
        /// var employeeId = await HRDataService.AddEmployeeAsync(employee);
        /// </remarks>
        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            // Validation: Check null
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Validation: FullName (Required)
            if (string.IsNullOrWhiteSpace(data.FullName))
                throw new Exception("Họ và tên nhân viên không được để trống");

            if (data.FullName.Length > 100)
                throw new Exception("Họ và tên nhân viên không được vượt quá 100 ký tự");

            // Validation: Email (Required)
            if (string.IsNullOrWhiteSpace(data.Email))
                throw new Exception("Email nhân viên không được để trống");

            if (data.Email.Length > 100)
                throw new Exception("Email không được vượt quá 100 ký tự");

            // Email format validation
            if (!System.Text.RegularExpressions.Regex.IsMatch(data.Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                throw new Exception("Email không đúng định dạng");

            // Email uniqueness validation
            if (!await ValidateEmployeeEmailAsync(data.Email))
                throw new Exception("Email đã tồn tại trong hệ thống");

            // Validation: Phone (Optional but limited)
            if (!string.IsNullOrWhiteSpace(data.Phone))
            {
                if (data.Phone.Length > 20)
                    throw new Exception("Số điện thoại không được vượt quá 20 ký tự");

                // Basic phone format validation
                if (!System.Text.RegularExpressions.Regex.IsMatch(data.Phone, @"^[0-9\s\-\+\(\)]+$"))
                    throw new Exception("Số điện thoại không hợp lệ. Chỉ cho phép số, dấu -, +, và ()");
            }

            // Validation: BirthDate (Optional but must be valid if provided)
            if (data.BirthDate.HasValue)
            {
                if (data.BirthDate.Value > DateTime.Now)
                    throw new Exception("Ngày sinh không thể là ngày trong tương lai");

                // Check reasonable age (between 16 and 100 years old)
                var age = DateTime.Now.Year - data.BirthDate.Value.Year;
                if (age < 16 || age > 100)
                    throw new Exception("Độ tuổi không hợp lệ (phải từ 16 đến 100 tuổi)");
            }

            // Validation: Address (Optional but limited)
            if (!string.IsNullOrWhiteSpace(data.Address) && data.Address.Length > 500)
                throw new Exception("Địa chỉ không được vượt quá 500 ký tự");

            return await employeeDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần cập nhật.</param>
        /// <returns>
        /// True nếu cập nhật thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            //TODO: Kiểm tra dữ liệu hợp lệ
            return await employeeDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công, False nếu nhân viên đang được sử dụng
        /// hoặc việc xóa không thực hiện được.
        /// </returns>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            if (await employeeDB.IsUsedAsync(employeeID))
                return false;

            return await employeeDB.DeleteAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem một nhân viên có đang được sử dụng trong dữ liệu hay không.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần kiểm tra.</param>
        /// <returns>
        /// True nếu nhân viên đang được sử dụng, ngược lại False.
        /// </returns>
        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            return await employeeDB.IsUsedAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// (không bị trùng với email của nhân viên khác).
        /// </summary>
        /// <param name="email">Địa chỉ email cần kiểm tra.</param>
        /// <param name="employeeID">
        /// Nếu employeeID = 0: kiểm tra email đối với nhân viên mới.
        /// Nếu employeeID khác 0: kiểm tra email của nhân viên có mã là employeeID.
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ (không trùng), ngược lại False.
        /// </returns>
        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
        {
            return await employeeDB.ValidateEmailAsync(email, employeeID);
        }

        /// <summary>
        /// Cập nhật RoleNames của nhân viên (phân quyền theo chuỗi trong CSDL).
        /// </summary>
        public static async Task<bool> UpdateEmployeeRoleNamesAsync(int employeeID, string? roleNames)
        {
            return await employeeDB.UpdateRoleNamesAsync(employeeID, roleNames);
        }

        #endregion
    }
}