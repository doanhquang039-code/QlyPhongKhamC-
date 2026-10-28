-- Du lieu test them cho QlyPhongKham.
-- An toan de chay lai: chi them cac dong chua co, khong xoa hay sua du lieu hien tai.
-- Tai khoan test: test_admin, test_bs, test_bs2, test_letan / mat khau 123456.
-- Tai khoan benh nhan test: test_bn01 / test_bn04 / test_bn05 (mat khau 123456).

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NULL
   OR OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BenhNhan', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PhieuKham', N'U') IS NULL
BEGIN
    THROW 50001, N'Chua co schema QL_PhongKham. Hay khoi tao CSDL truoc khi nap du lieu test.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.PhieuKham', N'KhamLamSang') IS NULL
        ALTER TABLE dbo.PhieuKham ADD KhamLamSang NVARCHAR(MAX) NULL;
    IF COL_LENGTH(N'dbo.PhieuKham', N'SinhHieu') IS NULL
        ALTER TABLE dbo.PhieuKham ADD SinhHieu NVARCHAR(200) NULL;

    IF OBJECT_ID(N'dbo.Khoa', N'U') IS NULL
        CREATE TABLE dbo.Khoa (
            MaKhoa VARCHAR(10) NOT NULL PRIMARY KEY,
            TenKhoa NVARCHAR(100) NOT NULL UNIQUE
        );

    IF OBJECT_ID(N'dbo.PhongKham', N'U') IS NULL
        CREATE TABLE dbo.PhongKham (
            MaPhong VARCHAR(10) NOT NULL PRIMARY KEY,
            MaKhoa VARCHAR(10) NOT NULL,
            TenPhong NVARCHAR(100) NOT NULL,
            CONSTRAINT FK_PhongKham_Khoa FOREIGN KEY (MaKhoa) REFERENCES dbo.Khoa(MaKhoa)
        );

    IF OBJECT_ID(N'dbo.PhanCongPhong', N'U') IS NULL
        CREATE TABLE dbo.PhanCongPhong (
            MaNV VARCHAR(10) NOT NULL,
            MaPhong VARCHAR(10) NOT NULL,
            VaiTro NVARCHAR(20) NOT NULL,
            CONSTRAINT PK_PhanCongPhong PRIMARY KEY (MaNV, MaPhong),
            CONSTRAINT CK_PhanCongPhong_VaiTro CHECK (VaiTro IN (N'BacSi', N'LeTan')),
            CONSTRAINT FK_PhanCongPhong_NhanVien FOREIGN KEY (MaNV) REFERENCES dbo.NhanVien(MaNV) ON DELETE CASCADE,
            CONSTRAINT FK_PhanCongPhong_Phong FOREIGN KEY (MaPhong) REFERENCES dbo.PhongKham(MaPhong) ON DELETE CASCADE
        );

    IF OBJECT_ID(N'dbo.TaiKhoanBenhNhan', N'U') IS NULL
        CREATE TABLE dbo.TaiKhoanBenhNhan (
            TenDangNhap VARCHAR(50) NOT NULL PRIMARY KEY,
            MatKhau VARCHAR(255) NOT NULL,
            MaBN VARCHAR(10) NOT NULL UNIQUE,
            TrangThai BIT NOT NULL CONSTRAINT DF_TaiKhoanBenhNhan_TrangThai DEFAULT 1,
            CONSTRAINT FK_TaiKhoanBenhNhan_BenhNhan FOREIGN KEY (MaBN) REFERENCES dbo.BenhNhan(MaBN) ON DELETE CASCADE
        );

    IF COL_LENGTH(N'dbo.PhieuKham', N'MaPhong') IS NULL
        ALTER TABLE dbo.PhieuKham ADD MaPhong VARCHAR(10) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhieuKham_Phong')
        ALTER TABLE dbo.PhieuKham ADD CONSTRAINT FK_PhieuKham_Phong
            FOREIGN KEY (MaPhong) REFERENCES dbo.PhongKham(MaPhong);

    INSERT INTO dbo.NhanVien (MaNV, HoTen, ChucVu, ChuyenKhoa, SoDienThoai, Email, TrangThai)
    SELECT v.MaNV, v.HoTen, v.ChucVu, v.ChuyenKhoa, v.SoDienThoai, v.Email, 1
    FROM (VALUES
        ('NV901', N'Test Admin', N'Admin', NULL, '0900001001', 'test.admin@example.test'),
        ('NV902', N'Test Bác sĩ Nội', N'BacSi', N'Nội tổng quát', '0900001002', 'test.bs@example.test'),
        ('NV903', N'Test Bác sĩ Tai Mũi Họng', N'BacSi', N'Tai Mũi Họng', '0900001003', 'test.bs2@example.test'),
        ('NV904', N'Test Lễ tân Thu ngân', N'LeTan', NULL, '0900001004', 'test.letan@example.test')
    ) v(MaNV, HoTen, ChucVu, ChuyenKhoa, SoDienThoai, Email)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.NhanVien n WHERE n.MaNV = v.MaNV);

    INSERT INTO dbo.TaiKhoan (TenDangNhap, MatKhau, MaNV, Quyen, TrangThai)
    SELECT v.TenDangNhap, '123456', v.MaNV, v.Quyen, 1
    FROM (VALUES
        ('test_admin', 'NV901', N'Admin'),
        ('test_bs', 'NV902', N'BacSi'),
        ('test_bs2', 'NV903', N'BacSi'),
        ('test_letan', 'NV904', N'LeTan')
    ) v(TenDangNhap, MaNV, Quyen)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.TaiKhoan t WHERE t.TenDangNhap = v.TenDangNhap)
      AND EXISTS (SELECT 1 FROM dbo.NhanVien n WHERE n.MaNV = v.MaNV AND n.ChucVu = v.Quyen);

    INSERT INTO dbo.Khoa (MaKhoa, TenKhoa)
    SELECT v.MaKhoa, v.TenKhoa
    FROM (VALUES
        ('KNOI', N'Nội tổng quát'),
        ('KTMH', N'Tai Mũi Họng'),
        ('KNHI', N'Nhi khoa')
    ) v(MaKhoa, TenKhoa)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Khoa k WHERE k.MaKhoa = v.MaKhoa);

    INSERT INTO dbo.PhongKham (MaPhong, MaKhoa, TenPhong)
    SELECT v.MaPhong, v.MaKhoa, v.TenPhong
    FROM (VALUES
        ('P101', 'KNOI', N'Phòng Nội 101'),
        ('P102', 'KNOI', N'Phòng Nội 102'),
        ('P201', 'KTMH', N'Phòng Tai Mũi Họng 201'),
        ('P301', 'KNHI', N'Phòng Nhi 301')
    ) v(MaPhong, MaKhoa, TenPhong)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PhongKham p WHERE p.MaPhong = v.MaPhong);

    INSERT INTO dbo.PhanCongPhong (MaNV, MaPhong, VaiTro)
    SELECT v.MaNV, v.MaPhong, v.VaiTro
    FROM (VALUES
        ('NV002', 'P101', N'BacSi'), ('NV002', 'P102', N'BacSi'),
        ('NV003', 'P201', N'BacSi'),
        ('NV004', 'P101', N'LeTan'), ('NV005', 'P201', N'LeTan'),
        ('NV902', 'P101', N'BacSi'), ('NV902', 'P102', N'BacSi'),
        ('NV903', 'P201', N'BacSi'),
        ('NV904', 'P101', N'LeTan'), ('NV904', 'P201', N'LeTan')
    ) v(MaNV, MaPhong, VaiTro)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.PhanCongPhong p WHERE p.MaNV = v.MaNV AND p.MaPhong = v.MaPhong
    )
      AND EXISTS (SELECT 1 FROM dbo.NhanVien n WHERE n.MaNV = v.MaNV AND n.ChucVu = v.VaiTro);

    INSERT INTO dbo.BenhNhan (MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh)
    SELECT v.MaBN, v.HoTen, v.NgaySinh, v.GioiTinh, v.SoDienThoai, v.DiaChi, v.TienSuBenh
    FROM (VALUES
        ('BN9001', N'Nguyễn Minh Anh', CONVERT(date, '19920412'), N'Nữ', '0901000001', N'Hà Nội', N'Dị ứng phấn hoa'),
        ('BN9002', N'Trần Quốc Bảo', CONVERT(date, '19870623'), N'Nam', '0901000002', N'Hà Nội', N'Tiền sử viêm dạ dày'),
        ('BN9003', N'Lê Thu Hà', CONVERT(date, '19981105'), N'Nữ', '0901000003', N'Bắc Ninh', N'Không ghi nhận'),
        ('BN9004', N'Phạm Đức Long', CONVERT(date, '19750317'), N'Nam', '0901000004', N'Hà Nội', N'Tăng huyết áp'),
        ('BN9005', N'Hoàng Ngọc Lan', CONVERT(date, '20010629'), N'Nữ', '0901000005', N'Hưng Yên', N'Không ghi nhận'),
        ('BN9006', N'Đỗ Thành Công', CONVERT(date, '19931112'), N'Nam', '0901000006', N'Hà Nội', N'Dị ứng hải sản'),
        ('BN9007', N'Vũ Khánh Linh', CONVERT(date, '20140309'), N'Nữ', '0901000007', N'Hà Nội', N'Viêm mũi dị ứng'),
        ('BN9008', N'Bùi Văn Nam', CONVERT(date, '19681221'), N'Nam', '0901000008', N'Nam Định', N'Tiểu đường type 2'),
        ('BN9009', N'Ngô Hải Yến', CONVERT(date, '19970602'), N'Nữ', '0901000009', N'Hà Nội', N'Không ghi nhận'),
        ('BN9010', N'Đặng Quang Huy', CONVERT(date, '19840215'), N'Nam', '0901000010', N'Hải Dương', N'Rối loạn mỡ máu'),
        ('BN9011', N'Phan Thị Mai', CONVERT(date, '19900930'), N'Nữ', '0901000011', N'Hà Nội', N'Dị ứng thời tiết'),
        ('BN9012', N'Đinh Gia Hưng', CONVERT(date, '20051207'), N'Nam', '0901000012', N'Hà Nội', N'Không ghi nhận'),
        ('BN9013', N'Nguyễn Thị Hương', CONVERT(date, '19790918'), N'Nữ', '0901000013', N'Bắc Giang', N'Tiền sử đau nửa đầu'),
        ('BN9014', N'Đào Minh Tú', CONVERT(date, '19960126'), N'Nam', '0901000014', N'Hà Nội', N'Không ghi nhận'),
        ('BN9015', N'Võ Ngọc Trâm', CONVERT(date, '20030411'), N'Nữ', '0901000015', N'Vĩnh Phúc', N'Viêm xoang tái phát'),
        ('BN9016', N'Phùng Văn Hải', CONVERT(date, '19811008'), N'Nam', '0901000016', N'Hà Nội', N'Không ghi nhận'),
        ('BN9017', N'Tạ Bảo Châu', CONVERT(date, '19950514'), N'Nữ', '0901000017', N'Hà Nội', N'Tiền sử viêm họng'),
        ('BN9018', N'Chu Anh Dũng', CONVERT(date, '19721103'), N'Nam', '0901000018', N'Phú Thọ', N'Tăng huyết áp'),
        ('BN9019', N'Mai Phương Thảo', CONVERT(date, '20000719'), N'Nữ', '0901000019', N'Hà Nội', N'Không ghi nhận'),
        ('BN9020', N'Đoàn Nhật Minh', CONVERT(date, '19991024'), N'Nam', '0901000020', N'Hà Nam', N'Không ghi nhận')
    ) v(MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.BenhNhan b WHERE b.MaBN = v.MaBN);

    INSERT INTO dbo.TaiKhoanBenhNhan (TenDangNhap, MatKhau, MaBN, TrangThai)
    SELECT v.TenDangNhap, '123456', v.MaBN, 1
    FROM (VALUES
        ('test_bn01', 'BN9001'),
        ('test_bn04', 'BN9004'),
        ('test_bn05', 'BN9005')
    ) v(TenDangNhap, MaBN)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.TaiKhoanBenhNhan t WHERE t.TenDangNhap = v.TenDangNhap)
      AND EXISTS (SELECT 1 FROM dbo.BenhNhan b WHERE b.MaBN = v.MaBN);

    INSERT INTO dbo.DichVu (MaDV, TenDV, DonGia, MoTa)
    SELECT v.MaDV, v.TenDV, v.DonGia, v.MoTa
    FROM (VALUES
        ('DV91', N'Khám tổng quát (test)', CAST(100000 AS DECIMAL(18,2)), N'Dịch vụ test'),
        ('DV92', N'Xét nghiệm máu (test)', CAST(250000 AS DECIMAL(18,2)), N'Dịch vụ test'),
        ('DV93', N'Điện tim (test)', CAST(180000 AS DECIMAL(18,2)), N'Dịch vụ test'),
        ('DV94', N'Nội soi Tai Mũi Họng (test)', CAST(300000 AS DECIMAL(18,2)), N'Dịch vụ test')
    ) v(MaDV, TenDV, DonGia, MoTa)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DichVu d WHERE d.MaDV = v.MaDV);

    INSERT INTO dbo.Thuoc (MaThuoc, TenThuoc, DonViTinh, DonGiaBan, SoLuongTon, CachDungMacDinh)
    SELECT v.MaThuoc, v.TenThuoc, v.DonViTinh, v.DonGiaBan, v.SoLuongTon, v.CachDungMacDinh
    FROM (VALUES
        ('TH91', N'Paracetamol test 500mg', N'Viên', CAST(2000 AS DECIMAL(18,2)), 180, N'Uống khi sốt theo hướng dẫn bác sĩ'),
        ('TH92', N'Vitamin C test 500mg', N'Viên', CAST(3000 AS DECIMAL(18,2)), 90, N'Uống sau ăn'),
        ('TH93', N'Cetirizine test 10mg', N'Viên', CAST(4000 AS DECIMAL(18,2)), 55, N'Uống buổi tối'),
        ('TH94', N'Omeprazole test 20mg', N'Viên', CAST(5000 AS DECIMAL(18,2)), 35, N'Uống trước ăn sáng'),
        ('TH95', N'Nước muối sinh lý test', N'Lọ', CAST(12000 AS DECIMAL(18,2)), 18, N'Dùng vệ sinh theo hướng dẫn'),
        ('TH96', N'Siro ho test', N'Lọ', CAST(45000 AS DECIMAL(18,2)), 8, N'Dùng theo hướng dẫn bác sĩ'),
        ('TH97', N'Gạc y tế test', N'Gói', CAST(10000 AS DECIMAL(18,2)), 0, N'Dùng ngoài da'),
        ('TH98', N'Dung dịch sát khuẩn test', N'Lọ', CAST(25000 AS DECIMAL(18,2)), 120, N'Dùng ngoài da')
    ) v(MaThuoc, TenThuoc, DonViTinh, DonGiaBan, SoLuongTon, CachDungMacDinh)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Thuoc t WHERE t.MaThuoc = v.MaThuoc);

    INSERT INTO dbo.LichHen (MaBN, MaNV_BacSi, NgayHen, LyDoKham, TrangThai)
    SELECT v.MaBN, v.MaNV_BacSi, v.NgayHen, v.LyDoKham, v.TrangThai
    FROM (VALUES
        ('BN9009', 'NV902', DATEADD(HOUR, 2, GETDATE()), N'Tái khám tổng quát', N'Đã xác nhận'),
        ('BN9010', 'NV902', DATEADD(HOUR, 4, GETDATE()), N'Đau đầu kéo dài', N'Chờ xác nhận'),
        ('BN9011', 'NV903', DATEADD(DAY, 1, GETDATE()), N'Khám viêm mũi dị ứng', N'Đã xác nhận'),
        ('BN9012', 'NV903', DATEADD(DAY, 2, GETDATE()), N'Khám sức khỏe', N'Chờ xác nhận'),
        ('BN9013', 'NV902', DATEADD(DAY, 3, GETDATE()), N'Tái khám huyết áp', N'Đã xác nhận'),
        ('BN9014', 'NV902', DATEADD(DAY, -1, GETDATE()), N'Khám đau họng', N'Đã đến'),
        ('BN9015', 'NV903', DATEADD(DAY, 4, GETDATE()), N'Nội soi Tai Mũi Họng', N'Đã hủy'),
        ('BN9016', 'NV903', DATEADD(HOUR, 6, GETDATE()), N'Khám viêm họng', N'Chờ xác nhận')
    ) v(MaBN, MaNV_BacSi, NgayHen, LyDoKham, TrangThai)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.LichHen l
        WHERE l.MaBN = v.MaBN
          AND l.MaNV_BacSi = v.MaNV_BacSi
          AND CONVERT(date, l.NgayHen) = CONVERT(date, v.NgayHen)
    );

    INSERT INTO dbo.PhieuKham
        (MaPK, STT, MaBN, MaNV_BacSi, NgayKham, TrieuChung, KhamLamSang, SinhHieu, ChanDoan, LoiDan, TrangThai)
    SELECT v.MaPK, v.STT, v.MaBN, v.MaNV_BacSi, v.NgayKham, v.TrieuChung, v.KhamLamSang,
           v.SinhHieu, v.ChanDoan, v.LoiDan, v.TrangThai
    FROM (VALUES
        ('PK9001', 1, 'BN9001', 'NV902', DATEADD(MINUTE, -80, GETDATE()), N'Sốt nhẹ, đau đầu', NULL, NULL, NULL, NULL, N'Chờ khám'),
        ('PK9002', 2, 'BN9002', 'NV902', DATEADD(MINUTE, -60, GETDATE()), N'Đau bụng vùng thượng vị', N'Ấn đau nhẹ vùng thượng vị', N'36.8°C; HA 125/80; Mạch 78', NULL, NULL, N'Đang khám'),
        ('PK9003', 3, 'BN9003', 'NV902', DATEADD(MINUTE, -45, GETDATE()), N'Mệt mỏi, chóng mặt', N'Tỉnh táo, niêm mạc hồng', N'36.7°C; HA 118/76; Mạch 80', N'Theo dõi thiếu máu', N'Chờ kết quả xét nghiệm', N'Chờ kết quả CLS'),
        ('PK9004', 4, 'BN9004', 'NV902', DATEADD(MINUTE, -30, GETDATE()), N'Đau đầu, huyết áp cao', N'Tỉnh táo, không yếu liệt', N'36.6°C; HA 150/95; Mạch 88', N'Tăng huyết áp', N'Tái khám sau 7 ngày', N'Đã khám xong'),
        ('PK9005', 5, 'BN9005', 'NV903', DATEADD(MINUTE, -25, GETDATE()), N'Đau họng, nghẹt mũi', N'Họng đỏ nhẹ, niêm mạc mũi phù nề', N'36.9°C; HA 110/70; Mạch 76', N'Viêm mũi họng cấp', N'Uống thuốc đúng hướng dẫn', N'Đã khám xong'),
        ('PK9006', 6, 'BN9006', 'NV903', DATEADD(MINUTE, -20, GETDATE()), N'Ngứa da sau ăn', NULL, NULL, NULL, NULL, N'Đã hủy'),
        ('PK9007', 7, 'BN9007', 'NV903', DATEADD(MINUTE, -15, GETDATE()), N'Hắt hơi, chảy nước mũi', N'Niêm mạc mũi phù nề', N'36.5°C; HA 108/68; Mạch 74', NULL, NULL, N'Chờ khám'),
        ('PK9008', 8, 'BN9008', 'NV902', DATEADD(MINUTE, -10, GETDATE()), N'Tái khám đường huyết', N'Tỉnh táo, thể trạng ổn định', N'36.6°C; HA 132/84; Mạch 82', N'Tiểu đường type 2 theo dõi', N'Tái khám theo lịch', N'Đã khám xong'),
        ('PK9009', 1, 'BN9009', 'NV903', DATEADD(DAY, -1, GETDATE()), N'Đau tai nhẹ', N'Màng nhĩ sáng, không xung huyết', N'36.7°C; HA 112/72; Mạch 75', N'Viêm tai ngoài nhẹ', N'Tái khám nếu triệu chứng tăng', N'Đã khám xong'),
        ('PK9010', 1, 'BN9010', 'NV902', DATEADD(DAY, -2, GETDATE()), N'Đau đầu', N'Tỉnh táo, không dấu hiệu thần kinh khu trú', N'36.6°C; HA 128/82; Mạch 79', N'Đau đầu căng cơ', N'Nghỉ ngơi, tái khám khi cần', N'Đã khám xong'),
        ('PK9011', 1, 'BN9011', 'NV902', DATEADD(DAY, -4, GETDATE()), N'Ợ nóng, đau thượng vị', N'Bụng mềm, ấn đau nhẹ', N'36.8°C; HA 120/78; Mạch 80', N'Viêm dạ dày', N'Tái khám sau 2 tuần', N'Đã khám xong'),
        ('PK9012', 1, 'BN9012', 'NV903', DATEADD(DAY, -6, GETDATE()), N'Khám sức khỏe định kỳ', N'Thể trạng ổn định', N'36.5°C; HA 115/75; Mạch 72', N'Sức khỏe ổn định', N'Duy trì sinh hoạt lành mạnh', N'Đã khám xong'),
        ('PK9013', 1, 'BN9001', 'NV902', DATEADD(DAY, -3, GETDATE()), N'Sốt siêu vi, đau đầu', N'Tỉnh táo, họng đỏ nhẹ', N'37.8°C; HA 118/76; Mạch 84', N'Nhiễm siêu vi đường hô hấp trên', N'Nghỉ ngơi, uống đủ nước, tái khám nếu sốt cao', N'Đã khám xong')
    ) v(MaPK, STT, MaBN, MaNV_BacSi, NgayKham, TrieuChung, KhamLamSang, SinhHieu, ChanDoan, LoiDan, TrangThai)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PhieuKham p WHERE p.MaPK = v.MaPK);

    UPDATE pk
    SET MaPhong = (
        SELECT TOP 1 pc.MaPhong
        FROM dbo.PhanCongPhong pc
        WHERE pc.MaNV = pk.MaNV_BacSi AND pc.VaiTro = N'BacSi'
        ORDER BY pc.MaPhong
    )
    FROM dbo.PhieuKham pk
    WHERE pk.MaPhong IS NULL
      AND EXISTS (
          SELECT 1 FROM dbo.PhanCongPhong pc
          WHERE pc.MaNV = pk.MaNV_BacSi AND pc.VaiTro = N'BacSi'
      );

    INSERT INTO dbo.ChiTietChiDinhDV (MaPK, MaDV, DonGiaThoiDiem, KetQua, TrangThai)
    SELECT v.MaPK, v.MaDV, v.DonGiaThoiDiem, v.KetQua, v.TrangThai
    FROM (VALUES
        ('PK9003', 'DV92', CAST(250000 AS DECIMAL(18,2)), NULL, N'Chờ thực hiện'),
        ('PK9004', 'DV93', CAST(180000 AS DECIMAL(18,2)), N'Điện tim trong giới hạn tham chiếu', N'Đã có kết quả'),
        ('PK9005', 'DV94', CAST(300000 AS DECIMAL(18,2)), N'Nội soi: niêm mạc họng sung huyết nhẹ', N'Đã có kết quả'),
        ('PK9009', 'DV91', CAST(100000 AS DECIMAL(18,2)), N'Đã khám và tư vấn', N'Đã có kết quả'),
        ('PK9010', 'DV93', CAST(180000 AS DECIMAL(18,2)), N'Không ghi nhận bất thường', N'Đã có kết quả'),
        ('PK9011', 'DV91', CAST(100000 AS DECIMAL(18,2)), N'Đã khám tổng quát', N'Đã có kết quả'),
        ('PK9012', 'DV92', CAST(250000 AS DECIMAL(18,2)), N'Kết quả trong giới hạn tham chiếu', N'Đã có kết quả'),
        ('PK9013', 'DV91', CAST(100000 AS DECIMAL(18,2)), N'Đã khám và tư vấn', N'Đã có kết quả')
    ) v(MaPK, MaDV, DonGiaThoiDiem, KetQua, TrangThai)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.ChiTietChiDinhDV c WHERE c.MaPK = v.MaPK AND c.MaDV = v.MaDV
    );

    INSERT INTO dbo.DonThuoc (MaDonThuoc, MaPK, NgayKe, GhiChu)
    SELECT v.MaDonThuoc, v.MaPK, v.NgayKe, v.GhiChu
    FROM (VALUES
        ('DT9001', 'PK9005', DATEADD(MINUTE, -24, GETDATE()), N'Đơn thuốc test - viêm mũi họng'),
        ('DT9002', 'PK9009', DATEADD(DAY, -1, GETDATE()), N'Đơn thuốc test - viêm tai'),
        ('DT9003', 'PK9010', DATEADD(DAY, -2, GETDATE()), N'Đơn thuốc test - đau đầu'),
        ('DT9004', 'PK9011', DATEADD(DAY, -4, GETDATE()), N'Đơn thuốc test - viêm dạ dày'),
        ('DT9005', 'PK9012', DATEADD(DAY, -6, GETDATE()), N'Đơn thuốc test - khám định kỳ'),
        ('DT9006', 'PK9013', DATEADD(DAY, -3, GETDATE()), N'Đơn thuốc test - sốt siêu vi')
    ) v(MaDonThuoc, MaPK, NgayKe, GhiChu)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DonThuoc d WHERE d.MaDonThuoc = v.MaDonThuoc OR d.MaPK = v.MaPK);

    INSERT INTO dbo.ChiTietDonThuoc (MaDonThuoc, MaThuoc, SoLuong, DonGiaThoiDiem, CachDung)
    SELECT v.MaDonThuoc, v.MaThuoc, v.SoLuong, v.DonGiaThoiDiem, v.CachDung
    FROM (VALUES
        ('DT9001', 'TH91', 6, CAST(2000 AS DECIMAL(18,2)), N'Uống khi sốt theo hướng dẫn bác sĩ'),
        ('DT9001', 'TH93', 5, CAST(4000 AS DECIMAL(18,2)), N'Uống buổi tối'),
        ('DT9002', 'TH95', 1, CAST(12000 AS DECIMAL(18,2)), N'Dùng vệ sinh theo hướng dẫn'),
        ('DT9003', 'TH91', 4, CAST(2000 AS DECIMAL(18,2)), N'Uống khi đau theo hướng dẫn bác sĩ'),
        ('DT9004', 'TH94', 14, CAST(5000 AS DECIMAL(18,2)), N'Uống trước ăn sáng'),
        ('DT9005', 'TH92', 10, CAST(3000 AS DECIMAL(18,2)), N'Uống sau ăn'),
        ('DT9006', 'TH91', 6, CAST(2000 AS DECIMAL(18,2)), N'Uống khi sốt theo hướng dẫn bác sĩ')
    ) v(MaDonThuoc, MaThuoc, SoLuong, DonGiaThoiDiem, CachDung)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.ChiTietDonThuoc c
        WHERE c.MaDonThuoc = v.MaDonThuoc AND c.MaThuoc = v.MaThuoc
    );

    INSERT INTO dbo.HoaDon
        (MaHD, MaPK, MaNV_ThuNgan, NgayLap, TienDichVu, TienThuoc, GiamGia, TongTien, HinhThucTT, TrangThai)
    SELECT v.MaHD, v.MaPK, v.MaNV_ThuNgan, v.NgayLap, v.TienDichVu, v.TienThuoc, v.GiamGia,
           v.TongTien, v.HinhThucTT, v.TrangThai
    FROM (VALUES
        ('HD9001', 'PK9005', 'NV904', GETDATE(), CAST(300000 AS DECIMAL(18,2)), CAST(32000 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)), CAST(332000 AS DECIMAL(18,2)), N'Tiền mặt', N'Đã thanh toán'),
        ('HD9002', 'PK9009', 'NV904', DATEADD(DAY, -1, GETDATE()), CAST(100000 AS DECIMAL(18,2)), CAST(12000 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)), CAST(112000 AS DECIMAL(18,2)), N'Chuyển khoản QR', N'Đã thanh toán'),
        ('HD9003', 'PK9010', 'NV904', DATEADD(DAY, -2, GETDATE()), CAST(180000 AS DECIMAL(18,2)), CAST(8000 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)), CAST(188000 AS DECIMAL(18,2)), N'Thẻ', N'Đã thanh toán'),
        ('HD9004', 'PK9011', 'NV904', DATEADD(DAY, -4, GETDATE()), CAST(100000 AS DECIMAL(18,2)), CAST(70000 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)), CAST(170000 AS DECIMAL(18,2)), N'Tiền mặt', N'Đã thanh toán'),
        ('HD9005', 'PK9012', 'NV904', DATEADD(DAY, -6, GETDATE()), CAST(250000 AS DECIMAL(18,2)), CAST(30000 AS DECIMAL(18,2)), CAST(10000 AS DECIMAL(18,2)), CAST(270000 AS DECIMAL(18,2)), N'Chuyển khoản QR', N'Đã thanh toán'),
        ('HD9006', 'PK9013', 'NV904', DATEADD(DAY, -3, GETDATE()), CAST(100000 AS DECIMAL(18,2)), CAST(12000 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)), CAST(112000 AS DECIMAL(18,2)), N'Tiền mặt', N'Đã thanh toán')
    ) v(MaHD, MaPK, MaNV_ThuNgan, NgayLap, TienDichVu, TienThuoc, GiamGia, TongTien, HinhThucTT, TrangThai)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.HoaDon h WHERE h.MaHD = v.MaHD OR h.MaPK = v.MaPK
    );

    COMMIT TRANSACTION;
    PRINT N'Nap du lieu test thanh cong. Tai khoan nhan vien: test_admin / test_bs / test_bs2 / test_letan; tai khoan benh nhan: test_bn01 / test_bn04 / test_bn05; mat khau 123456.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
