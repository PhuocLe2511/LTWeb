using SV22T1020330.Models.Sales;
using SV22T1020330.Admin;

namespace SV22T1020330.Admin
{
    /// <summary>
    /// Cung cấp các chức năng xử lý trên giỏ hàng
    /// (Giỏ hàng lưu trong session)
    /// </summary>
    public static class ShoppingCartService
    {
        /// <summary>
        /// Tên biến để lưu giỏ hàng trong session
        /// </summary>
        private const string CART = "ShoppingCart";

        /// <summary>
        /// Lấy giỏ hàng từ session
        /// </summary>
        /// <returns></returns>
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }
            return cart;
        }
        /// <summary>
        /// Lấy thông tin 1 mặt hàng từ giỏ hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public static OrderDetailViewInfo? GetCartItem(int productID)
        {
            var cart = GetShoppingCart();
            return cart.Find(m => m.ProductID == productID);
        }
        /// <summary>
        /// Thêm hàng vào giỏ hàng
        /// </summary>
        /// <param name="item"></param>
        public static void AddCartItem(OrderDetailViewInfo item)
        {
            var cart = GetShoppingCart();
            var existsItem = cart.Find(m => m.ProductID == item.ProductID);
            if (existsItem == null)
            {
                cart.Add(item);
            }
            else
            {
                existsItem.Quantity += item.Quantity;
                existsItem.SalePrice = item.SalePrice;
            }
            ApplicationContext.SetSessionData(CART, cart);
        }
        /// <summary>
        /// Cập nhật số lượng và giá của một mặt hàng trong giỏ hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            if (item != null)
            {
                item.Quantity = quantity;
                item.SalePrice = salePrice;
                ApplicationContext.SetSessionData(CART, cart);
            }
        }
        /// <summary>
        /// Xóa một mặt hàng ra khỏi giỏ hàng
        /// </summary>
        /// <param name="productID"></param>
        public static void RemoveCartItem(int productID)
        {
            var cart = GetShoppingCart();
            int index = cart.FindIndex(m => m.ProductID == productID);
            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }
        /// <summary>
        /// Xóa giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            var cart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(CART, cart);
        }
    }
}