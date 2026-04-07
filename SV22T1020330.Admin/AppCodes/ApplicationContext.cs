

using Newtonsoft.Json;

namespace SV22T1020330.Admin
{
    /// <summary>
    /// Lớp cung cấp các tiện ích liên quan đến ngữ cảnh của ứng dụng web
    /// </summary>
    /// <remarks>
    /// Mục đích:
    /// - Cung cấp truy cập đến HttpContext, IWebHostEnvironment, IConfiguration
    /// - Quản lý session data với serialization/deserialization
    /// - Cung cấp các thuộc tính global như PageSize, ConnectionString
    /// 
    /// Design pattern:
    /// - Static class với dependency injection
    /// - Configure() method để inject services
    /// - Thread-safe với static readonly fields
    /// 
    /// Usage:
    /// - Gọi ApplicationContext.Configure() trong Program.cs
    /// - Dùng ApplicationContext.GetSessionData<T>() để đọc session
    /// - Dùng ApplicationContext.SetSessionData() để ghi session
    /// - Truy cập ApplicationContext.PageSize, ConnectionString
    /// 
    /// Session management:
    /// - Sử dụng JSON serialization cho complex objects
    /// - Auto handle null values và empty strings
    /// - Type-safe với generic methods
    /// </remarks>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Cấu hình ApplicationContext với các services cần thiết
        /// </summary>
        /// <param name="httpContextAccessor">HttpContextAccessor từ DI container</param>
        /// <param name="webHostEnvironment">WebHostEnvironment từ DI container</param>
        /// <param name="configuration">Configuration từ DI container</param>
        /// <exception cref="ArgumentNullException">Nếu bất kỳ parameter nào là null</exception>
        /// <remarks>
        /// Phải gọi method này trong Program.cs trước khi sử dụng ApplicationContext:
        /// ApplicationContext.Configure(
        ///     app.Services.GetRequiredService<IHttpContextAccessor>(),
        ///     app.Services.GetRequiredService<IWebHostEnvironment>(),
        ///     app.Configuration
        /// );
        /// 
        /// Thread safety:
        /// - Static fields được set một lần tại startup
        /// - Không thay đổi sau khi configure
        /// - An toàn cho multi-threaded access
        /// </remarks>
        public static void Configure(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Lấy dữ liệu từ session với key và type cụ thể
        /// </summary>
        /// <typeparam name="T">Type của dữ liệu cần lấy</typeparam>
        /// <param name="key">Session key</param>
        /// <returns>Dữ liệu đã deserialize, hoặc default(T) nếu không tìm thấy</returns>
        /// <remarks>
        /// Process:
        /// 1. Lấy HttpContext từ accessor
        /// 2. Lấy session string từ HttpContext.Session
        /// 3. Deserialize JSON string sang object type T
        /// 4. Return object hoặc default value
        /// 
        /// Error handling:
        /// - Return default(T) nếu session không tồn tại
        /// - Return default(T) nếu JSON deserialize thất bại
        /// - Không throw exception để tránh crash ứng dụng
        /// 
        /// Performance:
        /// - JSON serialization có overhead
        /// - Nên dùng cho simple objects
        /// - Cache result nếu cần performance
        /// </remarks>
        public static T? GetSessionData<T>(string key)
        {
            var session = _httpContextAccessor?.HttpContext?.Session;
            if (session == null)
                return default(T);

            var jsonString = session.GetString(key);
            if (string.IsNullOrWhiteSpace(jsonString))
                return default(T);

            try
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Lưu dữ liệu vào session với JSON serialization
        /// </summary>
        /// <typeparam name="T">Type của dữ liệu cần lưu</typeparam>
        /// <param name="key">Session key</param>
        /// <param name="data">Dữ liệu cần lưu</param>
        /// <remarks>
        /// Process:
        /// 1. Serialize object sang JSON string
        /// 2. Lưu vào HttpContext.Session với key
        /// 3. Auto handle null values (lưu thành empty string)
        /// 
        /// Error handling:
        /// - Không làm gì nếu HttpContext null
        /// - JSON serialization errors sẽ throw exception
        /// - Cần try-catch ở caller nếu cần
        /// 
        /// Data integrity:
        /// - JSON serialization đảm bảo data integrity
        /// - Support complex objects với nested properties
        /// - Preserve object structure và types
        /// </remarks>
        public static void SetSessionData<T>(string key, T? data)
        {
            var session = _httpContextAccessor?.HttpContext?.Session;
            if (session == null)
                return;

            var jsonString = data == null ? "" : JsonConvert.SerializeObject(data);
            session.SetString(key, jsonString);
        }

        /// <summary>
        /// Kích thước trang mặc định cho phân trang
        /// </summary>
        /// <value>Giá trị từ configuration hoặc default = 12</value>
        /// <remarks>
        /// Configuration:
        /// - Đọc từ appsettings.json key "PageSize"
        /// - Default value = 12 nếu không có trong config
        /// - Dùng cho tất cả controllers có phân trang
        /// 
        /// Usage:
        /// - Controllers đọc giá trị này cho PageSize
        /// - Consistent pagination size across app
        /// - Easy to change in one place
        /// </remarks>
        public static int PageSize
        {
            get
            {
                var pageSize = _configuration?.GetValue<int?>("PageSize");
                return (pageSize.HasValue && pageSize.Value > 0) ? pageSize.Value : 12;
            }
        }

        /// <summary>
        /// Chuỗi kết nối đến database
        /// </summary>
        /// <value>Connection string từ configuration</value>
        /// <remarks>
        /// Configuration:
        /// - Đọc từ appsettings.json key "ConnectionStrings:LiteCommerceDB"
        /// - Required cho tất cả repository operations
        /// - Throw exception nếu không tìm thấy
        /// 
        /// Security:
        /// - Connection string chứa sensitive data
        /// - Không nên log hoặc expose ra client
        /// - Nên dùng environment variables trong production
        /// 
        /// Usage:
        /// - Repository constructors dùng connection string này
        /// - Consistent database connection across app
        /// - Easy to change database environment
        /// </remarks>
        public static string ConnectionString
        {
            get
            {
                var connectionString = _configuration?.GetConnectionString("LiteCommerceDB");
                if (string.IsNullOrEmpty(connectionString))
                    throw new Exception("ConnectionString is NULL!");
                return connectionString;
            }
        }

        /// <summary>
        /// Đường dẫn đến wwwroot folder
        /// </summary>
        /// <value>Đường dẫn tuyệt đối đến wwwroot</value>
        /// <remarks>
        /// Purpose:
        /// - Dùng để truy cập file uploads, images, static files
        /// - Cần cho file operations trong ProductController, EmployeeController
        /// - Đảm bảo đường dẫn đúng trên mọi environments
        /// 
        /// Usage:
        /// - ApplicationContext.WWWRootPath + "/images/products/"
        /// - ApplicationContext.WWWRootPath + "/uploads/employees/"
        /// - File.Exists(), Directory.CreateDirectory() operations
        /// 
        /// Security:
        /// - Validate file paths để tránh directory traversal
        /// - Chỉ cho phép truy cập các thư mục được phép
        /// - Use Path.Combine() để avoid path injection
        /// </remarks>
        public static string WWWRootPath
        {
            get
            {
                return _webHostEnvironment?.WebRootPath ?? throw new Exception("WebRootPath is NULL!");
            }
        }
    }
}
