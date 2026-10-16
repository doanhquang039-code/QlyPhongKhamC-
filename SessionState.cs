using QlyPhongKham.Models;

namespace QlyPhongKham
{
    public static class SessionState
    {
        public static TaiKhoan? CurrentUser { get; set; }
        public static bool IsLoggedIn => CurrentUser != null;

        public static bool IsAdmin => CurrentUser?.Quyen == "Admin";
        public static bool IsDoctor => CurrentUser?.Quyen == "BacSi";
        // LeTan đảm nhận cả Lễ tân (tiếp đón) lẫn Thu ngân (hóa đơn)
        public static bool IsReceptionist => CurrentUser?.Quyen == "LeTan";
        public static bool IsCashier => CurrentUser?.Quyen == "LeTan"; // alias: cùng role với IsReceptionist
    }
}
