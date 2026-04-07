
namespace SV22T1020330.BusinessLayers
{
    /// <summary>
    /// Lớp lưu trữ các cấu hình sử dụng Business Layer
    /// </summary>
    public static class Configuration
    {
        private static string? _connectionString = "";
        /// <summary>
        /// khởi tạo các cấu hình sử dụng trong Business Layer, hiện tại chỉ có chuỗi kết nối đến cơ sở dữ liệu
        /// hàm này sẽ được được gọi trước khi chạy ứng dụng
        /// </summary>
        /// <param name="ConnectionString"></param>
        public static void Initialize(string ConnectionString)
        {

          _connectionString = ConnectionString;


        }
        /// <summary>
        /// Lấy chuỗi tham số kết nối đến cơ sở dữ liệu sử dụng trong hệ thống
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
