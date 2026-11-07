using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace QlyPhongKham.Helpers
{
    public static class EmailHelper
    {
        // ── Cấu hình SMTP ─────────────────────────────────────────────
        public static string SmtpHost     { get; set; } = "smtp.gmail.com";
        public static int    SmtpPort     { get; set; } = 587;
        public static string SmtpUser     { get; set; } = "doanhquang039@gmail.com";
        public static string SmtpPassword { get; set; } = "sjrptfjlpsiwazuq";
        public static bool   EnableSsl    { get; set; } = true;
        public static string ClinicName   { get; set; } = "Phòng khám An Tâm";
        public static string? LastError   { get; set; }

        // ── Gửi email (đồng bộ, trả về true nếu thành công) ──────────
        public static bool Send(string to, string subject, string body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(SmtpUser))
                    return false;

                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    EnableSsl   = EnableSsl,
                    Credentials = new NetworkCredential(SmtpUser, SmtpPassword),
                    Timeout     = 15_000
                };
                using var msg = new MailMessage(SmtpUser, to, subject, body) { IsBodyHtml = true };
                client.Send(msg);
                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>Gửi bất đồng bộ (fire-and-forget) – không block UI.</summary>
        public static void SendAsync(string to, string subject, string body)
            => Task.Run(() => Send(to, subject, body));

        // ── Template: Xác nhận lịch hẹn ──────────────────────────────
        /// <summary>Trả về HTML body email xác nhận lịch hẹn.</summary>
        public static string BuildXacNhanLichHenBody(
            string tenBenhNhan,
            string tenBacSi,
            DateTime ngayHen,
            string lyDoKham)
        {
            return $@"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden'>
  <div style='background:#0fb4a4;padding:24px;text-align:center'>
    <h1 style='color:#fff;margin:0;font-size:22px'>✅ Lịch hẹn được xác nhận</h1>
    <p style='color:#d1faf5;margin:4px 0 0'>{ClinicName}</p>
  </div>
  <div style='padding:28px'>
    <p style='font-size:16px'>Xin chào <strong>{tenBenhNhan}</strong>,</p>
    <p>Lịch hẹn của bạn đã được xác nhận thành công. Vui lòng đến đúng giờ.</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0'>
      <tr style='background:#f8fafc'>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold;width:38%'>📅 Ngày &amp; Giờ hẹn</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb'>{ngayHen:dddd, dd/MM/yyyy HH:mm}</td>
      </tr>
      <tr>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold'>👨‍⚕️ Bác sĩ phụ trách</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb'>{tenBacSi}</td>
      </tr>
      <tr style='background:#f8fafc'>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold'>📋 Lý do khám</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb'>{(string.IsNullOrWhiteSpace(lyDoKham) ? "—" : lyDoKham)}</td>
      </tr>
    </table>
    <div style='background:#fffbeb;border-left:4px solid #f59e0b;padding:12px 16px;border-radius:4px;margin:16px 0'>
      <strong>Lưu ý:</strong> Vui lòng đến trước 15 phút để làm thủ tục và đo sinh hiệu.
      Nếu có thay đổi, hãy liên hệ phòng khám để hủy hoặc đổi lịch.
    </div>
    <p style='color:#6b7280;font-size:13px;margin-top:24px'>
      Email này được gửi tự động từ hệ thống {ClinicName}. Vui lòng không trả lời email này.
    </p>
  </div>
  <div style='background:#f1f5f9;padding:14px;text-align:center;color:#94a3b8;font-size:12px'>
    © {DateTime.Now.Year} {ClinicName}
  </div>
</div>";
        }

        public static bool GuiXacNhanLichHen(
            string emailBenhNhan,
            string tenBenhNhan,
            string tenBacSi,
            DateTime ngayHen,
            string lyDoKham)
        {
            if (string.IsNullOrWhiteSpace(emailBenhNhan)) return false;
            string subject = $"[{ClinicName}] Lịch hẹn của bạn đã được xác nhận";
            string body    = BuildXacNhanLichHenBody(tenBenhNhan, tenBacSi, ngayHen, lyDoKham);
            return Send(emailBenhNhan, subject, body);
        }

        // ── Template: Xác nhận thanh toán ─────────────────────────────
        /// <summary>Trả về HTML body email xác nhận thanh toán.</summary>
        public static string BuildThanhToanBody(
            string tenBenhNhan,
            string maHoaDon,
            decimal soTien,
            string hinhThucTT,
            DateTime ngayThanhToan)
        {
            return $@"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden'>
  <div style='background:#22c55e;padding:24px;text-align:center'>
    <h1 style='color:#fff;margin:0;font-size:22px'>💳 Thanh toán thành công</h1>
    <p style='color:#dcfce7;margin:4px 0 0'>{ClinicName}</p>
  </div>
  <div style='padding:28px'>
    <p style='font-size:16px'>Xin chào <strong>{tenBenhNhan}</strong>,</p>
    <p>Chúng tôi xác nhận bạn đã thanh toán viện phí thành công.</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0'>
      <tr style='background:#f8fafc'>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold;width:38%'>🧾 Mã hóa đơn</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold;color:#0ea5e9'>{maHoaDon}</td>
      </tr>
      <tr>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold'>💰 Số tiền</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-size:18px;font-weight:bold;color:#22c55e'>{soTien:N0} đ</td>
      </tr>
      <tr style='background:#f8fafc'>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold'>💳 Hình thức</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb'>{hinhThucTT}</td>
      </tr>
      <tr>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold'>🕐 Thời gian</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb'>{ngayThanhToan:dd/MM/yyyy HH:mm}</td>
      </tr>
    </table>
    <p style='color:#6b7280;font-size:13px;margin-top:24px'>
      Vui lòng lưu giữ email này làm biên lai. Cảm ơn bạn đã sử dụng dịch vụ của {ClinicName}.
    </p>
  </div>
  <div style='background:#f1f5f9;padding:14px;text-align:center;color:#94a3b8;font-size:12px'>
    © {DateTime.Now.Year} {ClinicName}
  </div>
</div>";
        }

        public static bool GuiXacNhanThanhToan(
            string emailBenhNhan,
            string tenBenhNhan,
            string maHoaDon,
            decimal tongTien,
            string hinhThucTT,
            DateTime ngayThanhToan)
        {
            if (string.IsNullOrWhiteSpace(emailBenhNhan)) return false;

            string subject = $"[{ClinicName}] Hóa đơn #{maHoaDon} đã thanh toán thành công";
            string body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden'>
  <div style='background:#22c55e;padding:24px;text-align:center'>
    <h1 style='color:#fff;margin:0;font-size:22px'>💳 Thanh toán thành công</h1>
    <p style='color:#dcfce7;margin:4px 0 0'>{ClinicName}</p>
  </div>
  <div style='padding:28px'>
    <p style='font-size:16px'>Xin chào <strong>{tenBenhNhan}</strong>,</p>
    <p>Chúng tôi xác nhận bạn đã thanh toán viện phí thành công.</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0'>
      <tr style='background:#f8fafc'>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold;width:38%'>🧾 Mã hóa đơn</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold;color:#0ea5e9'>{maHoaDon}</td>
      </tr>
      <tr>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold'>💰 Số tiền</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-size:18px;font-weight:bold;color:#22c55e'>{tongTien:N0} đ</td>
      </tr>
      <tr style='background:#f8fafc'>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold'>💳 Hình thức</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb'>{hinhThucTT}</td>
      </tr>
      <tr>
        <td style='padding:10px 14px;border:1px solid #e5e7eb;font-weight:bold'>🕐 Thời gian</td>
        <td style='padding:10px 14px;border:1px solid #e5e7eb'>{ngayThanhToan:dd/MM/yyyy HH:mm}</td>
      </tr>
    </table>
    <p style='color:#6b7280;font-size:13px;margin-top:24px'>
      Vui lòng lưu giữ email này làm biên lai. Cảm ơn bạn đã sử dụng dịch vụ của {ClinicName}.
    </p>
  </div>
  <div style='background:#f1f5f9;padding:14px;text-align:center;color:#94a3b8;font-size:12px'>
    © {DateTime.Now.Year} {ClinicName}
  </div>
</div>";
            return Send(emailBenhNhan, subject, body);
        }
    }
}
