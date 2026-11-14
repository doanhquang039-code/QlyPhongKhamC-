using System;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

namespace QlyPhongKham
{
    public static class SessionState
    {
        private static readonly object SessionLock = new();
        private static TaiKhoan? _currentUser;
        private static string? _validatedMaNV;
        private static string? _validatedRole;
        private static DateTime _validationCacheExpires;
        private static bool _cachedActive;

        public static TaiKhoan? CurrentUser
        {
            get => _currentUser;
            set
            {
                lock (SessionLock)
                {
                    _currentUser = value;
                    _validatedMaNV = null;
                    _validatedRole = null;
                    _validationCacheExpires = DateTime.MinValue;
                    _cachedActive = false;
                }
            }
        }
        public static TaiKhoanBenhNhan? CurrentPatient { get; set; }
        public static bool IsLoggedIn => CurrentUser != null || CurrentPatient != null;
        public static bool IsPatient => CurrentPatient != null;

        public static bool IsCurrentUserActive => IsEmployeeSessionActive();
        public static bool IsAdmin => CurrentUser?.Quyen == "Admin" && IsEmployeeSessionActive();
        public static bool IsDoctor => CurrentUser?.Quyen == "BacSi" && IsEmployeeSessionActive();
        public static bool IsReceptionist => CurrentUser?.Quyen == "LeTan" && IsEmployeeSessionActive();
        public static bool IsCashier => CurrentUser?.Quyen == "LeTan" && IsEmployeeSessionActive();

        private static bool IsEmployeeSessionActive()
        {
            TaiKhoan? user = _currentUser;
            if (user == null || string.IsNullOrWhiteSpace(user.MaNV) || string.IsNullOrWhiteSpace(user.Quyen))
                return false;

            lock (SessionLock)
            {
                if (_validatedMaNV == user.MaNV &&
                    _validatedRole == user.Quyen &&
                    DateTime.UtcNow < _validationCacheExpires)
                {
                    return _cachedActive;
                }

                _cachedActive = ClinicDAL.TaiKhoanNhanVienHoatDong(user.MaNV, user.Quyen);
                _validatedMaNV = user.MaNV;
                _validatedRole = user.Quyen;
                _validationCacheExpires = DateTime.UtcNow.AddSeconds(1);
                return _cachedActive;
            }
        }
    }
}
