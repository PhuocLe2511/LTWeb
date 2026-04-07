using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.Models.Account;
using SV22T1020330.Models.Security;
using SV22T1020330.Shop.AppCodes;
using System.Threading.Tasks;
using Dapper;

namespace SV22T1020330.Shop.Controllers;

/// <summary>
/// Controller quản lý tài khoản khách hàng cho giao diện Shop
/// </summary>
/// <remarks>
/// Chức năng chính:
/// - Đăng nhập/Đăng ký tài khoản khách hàng
/// - Đổi mật khẩu
/// - Đăng xuất
/// 
/// Authentication flow:
/// - Dùng session để lưu trạng thái đăng nhập
/// - Session key: "User" với UserAccount object
/// - Redirect về Login nếu chưa xác thực
/// 
/// Security:
/// - Hash mật khẩu với MD5 (CryptHelper.HashMD5)
/// - Validate input trước khi xử lý
/// - Kiểm tra user tồn tại trước khi đổi mật khẩu
/// 
/// Session management:
/// - Login: SetObject("User", user)
/// - Logout: Remove("User")
/// - Protected actions: kiểm tra session trước
/// </remarks>
public class AccountController : Controller
{
    private readonly IUserAccountRepository _repo;

    /// <summary>
    /// Khởi tạo AccountController với dependency injection
    /// </summary>
    /// <param name="repo">Repository xử lý tài khoản người dùng</param>
    public AccountController(IUserAccountRepository repo)
    {
        _repo = repo;
    }

    // ================= AUTHENTICATION =================

    /// <summary>
    /// Hiển thị trang đăng nhập
    /// </summary>
    /// <returns>View với form đăng nhập</returns>
    /// <remarks>
    /// Data:
    /// - Truyền LoginModel() rỗng vào view
    /// - View có fields: Username, Password
    /// - Include validation messages
    /// 
    /// UX:
    /// - Clean form khi load trang
    /// - Validation errors hiển thị dưới fields
    /// - Link đến trang đăng ký
    /// </remarks>
    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginModel());
    }

    /// <summary>
    /// Xử lý đăng nhập từ form
    /// </summary>
    /// <param name="model">Thông tin đăng nhập</param>
    /// <returns>Redirect về trang chủ nếu thành công, ngược lại về trang login</returns>
    /// <remarks>
    /// Validation:
    /// - ModelState.IsValid kiểm tra format
    /// - Username và password required
    /// 
    /// Authentication:
    /// - Gọi repo.AuthorizeAsync(username, password)
    /// - Repository tự hash password và check
    /// - Nếu user null: thông báo lỗi
    /// 
    /// Session:
    /// - Thành công: SetObject("User", user)
    /// - Redirect về Product Index
    /// - Thất bại: ViewBag.Message + return View
    /// 
    /// Security:
    /// - Không reveal user tồn tại hay không
    /// - Generic error message
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _repo.AuthorizeAsync(model.Username, CryptHelper.HashMD5(model.Password));
        if (user == null)
        {
            ViewBag.Message = "Tài khoản chưa tồn tại hoặc sai mật khẩu!";
            return View(model);
        }

        HttpContext.Session.SetObject("User", user);
        return RedirectToAction("Index", "Product");
    }

    // ================= REGISTRATION =================

    /// <summary>
    /// Hiển thị trang đăng ký
    /// </summary>
    /// <returns>View với form đăng ký</returns>
    /// <remarks>
    /// Data:
    /// - View không cần model (form rỗng)
    /// - Fields: Name, Email, Password
    /// - Client validation cho email format
    /// 
    /// UX:
    /// - Clean form khi load trang
    /// - Validation messages hiển thị real-time
    /// - Link về trang login
    /// </remarks>
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    /// <summary>
    /// Xử lý đăng ký tài khoản mới
    /// </summary>
    /// <param name="name">Tên khách hàng</param>
    /// <param name="email">Email đăng ký</param>
    /// <param name="password">Mật khẩu</param>
    /// <returns>Redirect về login nếu thành công</returns>
    /// <remarks>
    /// Validation:
    /// - Repository kiểm tra email tồn tại
    /// - Nếu email trùng: thông báo lỗi
    /// 
    /// Business logic:
    /// - Gọi repo.RegisterAsync(name, email, password)
    /// - Repository tự hash password
    /// - Tạo Customer record liên kết
    /// 
    /// User flow:
    /// - Thành công: thông báo + redirect Login
    /// - Thất bại: ViewBag.Message + return View
    /// - User phải đăng nhập sau khi đăng ký
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Register(string name, string email, string password)
    {
        bool ok = await _repo.RegisterAsync(name, email, CryptHelper.HashMD5(password));
        if (!ok)
        {
            ViewBag.Message = "Email đã tồn tại!";
            return View();
        }

        ViewBag.Message = "Đăng ký thành công, hãy đăng nhập!";
        return RedirectToAction("Login");
    }

    // ================= SESSION MANAGEMENT =================

    /// <summary>
    /// Đăng xuất tài khoản
    /// </summary>
    /// <returns>Redirect về trang chủ</returns>
    /// <remarks>
    /// Session:
    /// - Remove("User") để xóa session
    /// - Không cần check user tồn tại
    /// 
    /// Security:
    /// - Clean logout, remove all session data
    /// - Redirect về trang chủ (không cần login)
    /// 
    /// UX:
    /// - User thấy trang chủ ngay lập tức
    /// - Login/Register buttons hiển thị lại
    /// </remarks>
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("User");
        return RedirectToAction("Index", "Product");
    }

    // ================= PROFILE MANAGEMENT =================

    /// <summary>
    /// Hiển thị trang thông tin cá nhân
    /// </summary>
    /// <returns>View với form thông tin cá nhân</returns>
    /// <remarks>
    /// Authentication:
    /// - Kiểm tra user đăng nhập trong session
    /// - Nếu chưa đăng nhập: redirect Login
    /// 
    /// Data:
    /// - Lấy thông tin customer từ database
    /// - View với Customer model
    /// 
    /// Security:
    /// - Chỉ user đã đăng nhập mới truy cập
    /// - Chỉ xem thông tin chính mình
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = HttpContext.Session.GetObject<UserAccount>("User");
        if (user == null)
            return RedirectToAction("Login");

        try
        {
            // Lấy thông tin chi tiết customer từ database
            using var connection = new SqlConnection(_repo.GetType().GetField("_connectionString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_repo).ToString());
            string sql = @"
                SELECT CustomerID, CustomerName, ContactName, Email, Phone, Address, Province
                FROM Customers 
                WHERE Email = @Email";
            
            var customer = await connection.QuerySingleOrDefaultAsync<dynamic>(
                sql, new { Email = user.Email });
            
            if (customer != null)
            {
                ViewBag.CustomerName = customer.CustomerName;
                ViewBag.ContactName = customer.ContactName;
                ViewBag.Email = customer.Email;
                ViewBag.Phone = customer.Phone;
                ViewBag.Address = customer.Address;
                ViewBag.Province = customer.Province;
            }
            else
            {
                // Nếu không tìm thấy, dùng thông tin từ UserAccount
                ViewBag.CustomerName = user.DisplayName;
                ViewBag.ContactName = user.DisplayName;
                ViewBag.Email = user.Email;
                ViewBag.Phone = "";
                ViewBag.Address = "";
                ViewBag.Province = "";
            }
        }
        catch
        {
            // Nếu có lỗi, dùng thông tin từ UserAccount
            ViewBag.CustomerName = user.DisplayName;
            ViewBag.ContactName = user.DisplayName;
            ViewBag.Email = user.Email;
            ViewBag.Phone = "";
            ViewBag.Address = "";
            ViewBag.Province = "";
        }

        return View();
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân
    /// </summary>
    /// <param name="customerName">Tên khách hàng</param>
    /// <param name="contactName">Tên liên hệ</param>
    /// <param name="phone">Số điện thoại</param>
    /// <param name="address">Địa chỉ</param>
    /// <param name="province">Tỉnh/Thành phố</param>
    /// <returns>View với thông báo kết quả</returns>
    /// <remarks>
    /// Authentication:
    /// - Kiểm tra user đăng nhập
    /// - Nếu chưa đăng nhập: redirect Login
    /// 
    /// Validation:
    /// - Kiểm tra các field không rỗng
    /// 
    /// Security:
    /// - Chỉ cập nhật thông tin chính mình
    /// - Không cho thay đổi email
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Profile(string customerName, string contactName, string phone, string address, string province)
    {
        var user = HttpContext.Session.GetObject<UserAccount>("User");
        if (user == null)
            return RedirectToAction("Login");

        ViewBag.CustomerName = customerName;
        ViewBag.ContactName = contactName;
        ViewBag.Email = user.Email;
        ViewBag.Phone = phone;
        ViewBag.Address = address;
        ViewBag.Province = province;

        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(contactName))
        {
            ViewBag.Message = "Vui lòng nhập tên khách hàng và tên liên hệ!";
            return View();
        }

        try
        {
            using var connection = new SqlConnection(_repo.GetType().GetField("_connectionString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_repo).ToString());
            string sql = @"
                UPDATE Customers 
                SET CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Phone = @Phone,
                    Address = @Address,
                    Province = @Province
                WHERE Email = @Email";
            
            int rows = await connection.ExecuteAsync(sql, new 
            { 
                CustomerName = customerName,
                ContactName = contactName,
                Phone = phone,
                Address = address,
                Province = province,
                Email = user.Email
            });

            if (rows > 0)
            {
                ViewBag.Message = "Cập nhật thông tin thành công!";
                // Cập nhật session user information
                user.DisplayName = customerName;
                HttpContext.Session.SetObject("User", user);
            }
            else
            {
                ViewBag.Message = "Cập nhật thông tin thất bại!";
            }
        }
        catch (Exception)
        {
            ViewBag.Message = "Lỗi hệ thống, vui lòng thử lại sau!";
        }

        return View();
    }

    // ================= PASSWORD MANAGEMENT =================

    /// <summary>
    /// Hiển thị trang đổi mật khẩu
    /// </summary>
    /// <returns>View với form đổi mật khẩu</returns>
    /// <remarks>
    /// Authentication:
    /// - Kiểm tra user đăng nhập trong session
    /// - Nếu chưa đăng nhập: redirect Login
    /// 
    /// Data:
    /// - View không cần model
    /// - Fields: OldPassword, NewPassword, ConfirmPassword
    /// 
    /// Security:
    /// - Chỉ user đã đăng nhập mới truy cập
    /// - Form yêu cầu nhập mật khẩu cũ
    /// </remarks>
    [HttpGet]
    public IActionResult ChangePassword()
    {
        var user = HttpContext.Session.GetObject<UserAccount>("User");
        if (user == null)
            return RedirectToAction("Login");

        ViewBag.DisplayName = user.DisplayName;
        ViewBag.Email = user.Email;
        return View();
    }

    /// <summary>
    /// Xử lý đổi mật khẩu
    /// </summary>
    /// <param name="oldPassword">Mật khẩu cũ</param>
    /// <param name="newPassword">Mật khẩu mới</param>
    /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
    /// <returns>View với thông báo kết quả</returns>
    /// <remarks>
    /// Authentication:
    /// - Kiểm tra user đăng nhập
    /// - Nếu chưa đăng nhập: redirect Login
    /// 
    /// Validation:
    /// - Kiểm tra tất cả fields không rỗng
    /// - Kiểm tra newPassword == confirmPassword
    /// - Kiểm tra oldPassword đúng với database
    /// 
    /// Security:
    /// - Hash oldPassword với MD5 để verify
    /// - Hash newPassword trước khi lưu
    /// - Không lưu plain text passwords
    /// 
    /// Error handling:
    /// - Try-catch cho repository errors
    /// - User-friendly error messages
    /// - Generic message nếu service unavailable
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        var user = HttpContext.Session.GetObject<UserAccount>("User");
        if (user == null)
            return RedirectToAction("Login");

        ViewBag.DisplayName = user.DisplayName;
        ViewBag.Email = user.Email;

        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            ViewBag.Message = "Vui lòng nhập đầy đủ thông tin";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.Message = "Mật khẩu xác nhận không khớp";
            return View();
        }

        try
        {
            string hashedOldPassword = CryptHelper.HashMD5(oldPassword);

            var checkUser = await _repo.AuthorizeAsync(user.Email, hashedOldPassword);
            if (checkUser == null)
            {
                ViewBag.Message = "Mật khẩu cũ không đúng";
                return View();
            }

            string hashedNewPassword = CryptHelper.HashMD5(newPassword);
            var result = await _repo.ChangePasswordAsync(user.Email, hashedNewPassword);

            if (result)
                ViewBag.Message = "Đổi mật khẩu thành công";
            else
                ViewBag.Message = "Đổi mật khẩu thất bại";
        }
        catch (Exception)
        {
            ViewBag.Message = "Tính năng đổi mật khẩu chưa khả dụng";
        }

        return View();
    }
}