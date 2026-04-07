namespace SV22T1020330.Admin.Models
{
    /// <summary>
    /// Form chỉnh sửa RoleNames của nhân viên (bảng Employees).
    /// </summary>
    public class EmployeeRoleEditViewModel
    {
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = "";
        public string Email { get; set; } = "";
        /// <summary>
        /// Vai trò cách nhau bởi dấu phẩy (ví dụ: admin,sales).
        /// </summary>
        public string? RoleNames { get; set; }
    }
}
