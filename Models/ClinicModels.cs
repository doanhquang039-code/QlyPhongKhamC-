using System;

namespace QlyPhongKham.Models
{
    public class NhanVien
    {
        public string MaNV { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty; // Admin, BacSi, ThuNgan, LeTan
        public string? ChuyenKhoa { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public bool TrangThai { get; set; } = true;
    }

    public class TaiKhoan
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string MaNV { get; set; } = string.Empty;
        public string Quyen { get; set; } = string.Empty;
        public bool TrangThai { get; set; } = true;
        public string? HoTenNhanVien { get; set; }
    }

    public class BenhNhan
    {
        public string MaBN { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; } = "Nam";
        public string? SoDienThoai { get; set; }
        public string? DiaChi { get; set; }
        public string? TienSuBenh { get; set; }
    }

    public class PhieuKham
    {
        public string MaPK { get; set; } = string.Empty;
        public int STT { get; set; }
        public string MaBN { get; set; } = string.Empty;
        public string TenBN { get; set; } = string.Empty;
        public string MaNV_BacSi { get; set; } = string.Empty;
        public string TenBacSi { get; set; } = string.Empty;
        public DateTime NgayKham { get; set; } = DateTime.Now;
        public string? TrieuChung { get; set; }
        public string? ChanDoan { get; set; }
        public string? LoiDan { get; set; }
        public string TrangThai { get; set; } = "Chờ khám";
    }

    public class DichVu
    {
        public string MaDV { get; set; } = string.Empty;
        public string TenDV { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public string? MoTa { get; set; }
    }

    public class Thuoc
    {
        public string MaThuoc { get; set; } = string.Empty;
        public string TenThuoc { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal DonGiaBan { get; set; }
        public int SoLuongTon { get; set; }
        public string? CachDungMacDinh { get; set; }
    }

    public class ChiTietChiDinhDV
    {
        public int MaChiDinh { get; set; }
        public string MaPK { get; set; } = string.Empty;
        public string MaDV { get; set; } = string.Empty;
        public string TenDV { get; set; } = string.Empty;
        public decimal DonGiaThoiDiem { get; set; }
        public string? KetQua { get; set; }
        public string TrangThai { get; set; } = "Chờ thực hiện";
    }

    public class ChiTietDonThuoc
    {
        public int MaCTDT { get; set; }
        public string MaDonThuoc { get; set; } = string.Empty;
        public string MaThuoc { get; set; } = string.Empty;
        public string TenThuoc { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGiaThoiDiem { get; set; }
        public decimal ThanhTien => SoLuong * DonGiaThoiDiem;
        public string CachDung { get; set; } = string.Empty;
    }

    public class HoaDon
    {
        public string MaHD { get; set; } = string.Empty;
        public string MaPK { get; set; } = string.Empty;
        public string MaNV_ThuNgan { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; } = DateTime.Now;
        public decimal TienDichVu { get; set; }
        public decimal TienThuoc { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongTien { get; set; }
        public string HinhThucTT { get; set; } = "Tiền mặt";
        public string TrangThai { get; set; } = "Chưa thanh toán";
    }
}
