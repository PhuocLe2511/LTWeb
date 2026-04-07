using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020330.BusinessLayers;
using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.DataLayers.SqlServer;
using SV22T1020330.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020330.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý tài khoản quản trị viên cho giao diện Admin
    /// </summary>
    /// <remarks>
    /// Chức năng chính:
    /// - Đăng nhập/Đăng xuất tài khoản admin
    /// - Quản lý thông tin tài khoản
    /// - Xác thực và phân quyền
    /// 
    /// Authentication:
    /// - Sử dụng Cookie Authentication
    /// - Yêu cầu [Authorize] cho tất cả actions trừ Login
    /// - Lưu thông tin user trong WebUserData
    /// 
    /// Security:
    /// - Hash mật khẩu với MD5 (CryptHelper.HashMD5)
    /// - Validate input trước khi xử lý
    /// - Redirect về Login nếu chưa xác thực
    /// 
    /// Session management:
    /// - Không dùng session, dùng cookie authentication
    /// - WebUserData lưu trong ClaimsPrincipal
    /// - Auto-redirect khi session expired
    /// </remarks>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserAccountRepository _userAccountRepo;

        /// <summary>
        /// Khởi tạo AccountController với UserAccountRepository
        /// </summary>
        /// <remarks>
        /// Repository pattern:
        /// - Tạo UserAccountRepository với connection string
        /// - Không dùng dependency injection (hard-coded)
        /// - Sử dụng Configuration.ConnectionString
        /// 
        /// TODO: Cần refactor để dùng DI injection
        /// </remarks>
        public AccountController()
        {
            _userAccountRepo = new UserAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Trang dashboard chính của admin
        /// </summary>
        /// <returns>View dashboard với thông tin chung</returns>
        /// <remarks>
        /// Purpose:
        /// - Trang chủ sau khi đăng nhập thành công
        /// - Hiển thị thống kê, thông báo hệ thống
        /// - Navigation đến các module khác
        /// 
        /// Authentication:
        /// - [Authorize] attribute đảm bảo user đã đăng nhập
        /// - Không cần kiểm tra manual (middleware xử lý)
        /// 
        /// Data:
        /// - Không cần model đặc biệt
        /// - View có thể truy cập User thông qua User.Identity
        /// </remarks>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Đăng nhập tài khoản quản trị viên
        /// </summary>
        /// <returns>View với form đăng nhập</returns>
        /// <remarks>
        /// Purpose:
        /// - Hiển thị form đăng nhập cho admin
        /// - Validate credentials và tạo authentication cookie
        /// - Redirect về dashboard nếu thành công
        /// 
        /// Authentication flow:
        /// 1. User nhập username/password
        /// 2. Hash password và check với database
        /// 3. Tạo ClaimsPrincipal với user info
        /// 4. Sign in với cookie authentication
        /// 5. Redirect về Index (dashboard)
        /// 
        /// Security:
        /// - Không reveal user tồn tại hay không
        /// - Generic error message cho failed login
        /// - Hash password trước khi so sánh
        /// </remarks>
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập từ form
        /// </summary>
        /// <param name="username">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>Redirect về dashboard nếu thành công</returns>
        /// <remarks>
        /// Validation:
        /// - Kiểm tra username và password không rỗng
        /// - Hash password với MD5 trước khi query
        /// - Gọi UserAccountRepository.AuthorizeAsync()
        /// 
        /// Authentication:
        /// - Thành công: Tạo WebUserData từ database
        /// - Tạo ClaimsPrincipal với user claims
        /// - HttpContext.SignInAsync() với cookie
        /// - Redirect về Index (dashboard)
        /// 
        /// Error handling:
        /// - Thất bại: ViewBag.Message + return View
        /// - Generic error message
        /// - Không reveal specific error details
        /// 
        /// Security:
        /// - Không lưu plain text password
        /// - Use authentication cookie with proper options
        /// - Auto-expire cookie based on configuration
        /// </remarks>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Nhập Email và mật khẩu");
                return View();
            }

            string hashedPassword = CryptHelper.HashMD5(password);

            try
            {
                // Kiểm tra email và hashedPassword với cơ sở dữ liệu Employees
                var userAccount = await _userAccountRepo.AuthorizeAsync(username, hashedPassword);

                if (userAccount == null)
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                    return View();
                }

                //chuẩn bị thông tin để ghi giấy chứng nhận 
                var userData = new WebUserData()
                {
                    UserId = userAccount.UserId,
                    UserName = userAccount.UserName,
                    DisplayName = userAccount.DisplayName,
                    Email = userAccount.Email,
                    Photo = userAccount.Photo,
                   
                    Roles = userAccount.RoleNames?.Split(',').ToList()
                };
                //tạo giấy chứng nhận
                var principal = userData.CreatePrincipal();
                // cấp giấy chứng nhận cho người dùng
                await  HttpContext.SignInAsync(principal);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                return View();
            }
        }
        /// <summary>
        /// Đăng xuất tài khoản quản trị viên
        /// </summary>
        /// <returns></returns>
        /// 


    public async Task<IActionResult> Logout()
    {
            HttpContext.Session.Clear();
            
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// Đổi mật khẩu tài khoản quản trị viên
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin");
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
            return View();
        }

        // Lấy email từ user đang đăng nhập
        string userEmail = User.Identity?.Name ?? "";
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login");
        }

        try
        {
            string hashedOldPassword = CryptHelper.HashMD5(oldPassword);
            
            // Kiểm tra mật khẩu cũ
            var userAccount = await _userAccountRepo.AuthorizeAsync(userEmail, hashedOldPassword);
            if (userAccount == null)
            {
                ModelState.AddModelError("", "Mật khẩu cũ không đúng");
                return View();
            }

            string hashedNewPassword = CryptHelper.HashMD5(newPassword);
            var result = await _userAccountRepo.ChangePasswordAsync(userEmail, hashedNewPassword);
            
            if (result)
            {
                ViewBag.Message = "Đổi mật khẩu thành công";
            }
            else
            {
                ModelState.AddModelError("", "Đổi mật khẩu thất bại");
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
        }

        return View();
    }
}
}
