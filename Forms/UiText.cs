using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace QlyPhongKham.Forms
{
    internal static class UiText
    {
        private static readonly KeyValuePair<string, string>[] Translations =
        {
            Pair("Quản trị viên", "Administrator"),
            Pair("Bác sĩ khám bệnh", "Clinician"),
            Pair("Bác sĩ", "Doctor"),
            Pair("Y tá", "Nurse"),
            Pair("Thu ngân", "Cashier"),
            Pair("MENU CHÍNH", "MAIN MENU"),
            Pair("CỔNG BỆNH NHÂN", "PATIENT PORTAL"),
            Pair("Cổng bệnh nhân", "Patient portal"),
            Pair("An Tâm - Cổng bệnh nhân", "An Tam - Patient portal"),
            Pair("An Tâm – Hệ thống Quản lý Phòng Khám", "An Tam - Clinic Management System"),
            Pair("Tổng quan tiếp đón & điều phối", "Reception & coordination overview"),
            Pair("Tổng quan", "Overview"),
            Pair("Trang chủ", "Home"),
            Pair("Trang chủ bệnh nhân", "Patient home"),
            Pair("Lịch hẹn khám", "Appointment bookings"),
            Pair("Lịch hẹn", "Appointments"),
            Pair("Lịch khám", "Examination schedule"),
            Pair("Tiếp nhận bệnh nhân", "Patient reception"),
            Pair("Bệnh nhân", "Patients"),
            Pair("👤 Bệnh nhân của tôi", "👤 My patients"),
            Pair("🏢 Cả phòng", "🏢 Entire room"),
            Pair("Phòng được phân công:", "Assigned rooms:"),
            Pair("Chỉ nhận được ca đang chờ khám. Ca đã bắt đầu do bác sĩ đang phụ trách tiếp tục.", "Only waiting cases can be claimed. The assigned doctor must continue cases already in progress."),
            Pair("Ca khám này đang do ", "This visit is currently assigned to "),
            Pair(". Nếu nhận, người phụ trách sẽ chuyển sang bạn. Bạn có muốn tiếp tục không?", ". If you accept, responsibility will transfer to you. Continue?"),
            Pair("Nhận ca khám trong phòng", "Take a room case"),
            Pair("Ca đã được nhận", "Case already claimed"),
            Pair("Không thể nhận ca khám", "Unable to claim case"),
            Pair("Bệnh nhân   ›", "Patient   ›"),
            Pair("Phiếu khám", "Examination records"),
            Pair("Khám & Kê đơn", "Examination & prescriptions"),
            Pair("Kho thuốc", "Medicine inventory"),
            Pair("Danh mục thuốc", "Medicine catalog"),
            Pair("Bảng giá dịch vụ", "Service pricing"),
            Pair("Dịch vụ cận lâm sàng", "Diagnostic services"),
            Pair("Hóa đơn & Thu ngân", "Invoices & cashier"),
            Pair("Thanh toán viện phí", "Hospital payments"),
            Pair("Thanh toán", "Payment"),
            Pair("Nhân sự", "Staff"),
            Pair("Khoa & Phòng khám", "Departments & rooms"),
            Pair("Báo cáo thống kê", "Reports"),
            Pair("Nhật ký hệ thống", "System logs"),
            Pair("Hồ sơ cá nhân", "My profile"),
            Pair("Xem thông tin của bạn", "View your information"),
            Pair("Xem hồ sơ của bạn", "View your profile"),
            Pair("Hồ sơ khám bệnh", "Medical records"),
            Pair("LỊCH HẸN SẮP TỚI", "UPCOMING APPOINTMENTS"),
            Pair("Bạn chưa có lịch hẹn sắp tới.", "You have no upcoming appointments."),
            Pair("Xem lịch hẹn", "View appointments"),
            Pair("HỒ SƠ VÀ TÀI LIỆU MỚI", "RECENT RECORDS AND DOCUMENTS"),
            Pair("Số lượt khám đã ghi nhận", "Recorded visits"),
            Pair("Điện thoại:", "Phone:"),
            Pair("Mở hồ sơ khám bệnh", "Open medical records"),
            Pair("HÓA ĐƠN / VIỆN PHÍ", "INVOICES / PAYMENTS"),
            Pair("Hóa đơn", "Invoice"),
            Pair("Xem chi tiết các hóa đơn và trạng thái thanh toán của bạn.", "View your invoices and payment status."),
            Pair("Hóa đơn của tôi", "My invoices"),
            Pair("Chi tiết viện phí", "Payment details"),
            Pair("Chọn một hóa đơn để xem thông tin.", "Select an invoice to view its details."),
            Pair("Nếu hóa đơn còn chưa thanh toán, bạn có thể quét mã VietQR hoặc đến quầy thu ngân phòng khám để được hỗ trợ.", "For unpaid invoices, scan the VietQR code or contact the clinic cashier for assistance."),
            Pair("Xem mã QR thanh toán", "View payment QR code"),
            Pair("Hóa đơn chưa thanh toán. Bạn có thể bấm nút bên dưới để mở mã VietQR chuyển khoản nhanh.", "This invoice is unpaid. Use the button below to open a VietQR code for quick bank transfer."),
            Pair("Hóa đơn này đã được thanh toán thành công. Cảm ơn bạn đã tin tưởng An Tâm.", "This invoice has been paid. Thank you for choosing An Tam."),
            Pair("ĐĂNG KÝ LỊCH KHÁM", "BOOK AN APPOINTMENT"),
            Pair("CẬP NHẬT LỊCH HẸN", "UPDATE APPOINTMENT"),
            Pair("Tra cứu hướng dẫn sử dụng cổng bệnh nhân. Trợ lý không thay thế tư vấn hoặc chẩn đoán của bác sĩ.", "Get help using the patient portal. The assistant does not replace medical advice or a doctor's diagnosis."),
            Pair("Hỏi đáp sức khỏe và hồ sơ", "Health and records Q&A"),
            Pair("Thông tin cá nhân", "Personal information"),
            Pair("Trợ lý An Tâm", "An Tam Assistant"),
            Pair("Trợ lý An Tâm:", "An Tam Assistant:"),
            Pair("Bạn:", "You:"),
            Pair("GIỜ HOẠT ĐỘNG PHÒNG KHÁM", "CLINIC HOURS"),
            Pair("Thứ 2 - Thứ 7", "Monday - Saturday"),
            Pair("Khám trong ngày", "Same-day visits"),
            Pair("Ca tối / Khám hẹn", "Evening / Appointment"),
            Pair("Chủ Nhật", "Sunday"),
            Pair("Theo lịch hẹn trước", "By appointment only"),
            Pair("Xin chào,", "Hello,"),
            Pair("Chúc bạn một ngày khỏe mạnh! Theo dõi lịch trực bác sĩ và lịch hẹn của bạn tại đây.", "Have a healthy day! Check doctors on duty and your appointments here."),
            Pair("Quản lý và theo dõi lịch hẹn khám bệnh của bạn", "Manage and track your appointments"),
            Pair("Lịch hẹn của tôi (Click vào một dòng để xem chi tiết bên dưới)", "My appointments (select a row to view details below)"),
            Pair("CHI TIẾT LỊCH HẸN ĐANG CHỌN", "SELECTED APPOINTMENT DETAILS"),
            Pair("Chọn một lịch hẹn ở bảng trên để xem chi tiết.", "Select an appointment above to view details."),
            Pair("Lưu ý: Chỉ lịch ở trạng thái 'Chờ xác nhận' mới được đổi giờ hoặc sửa lý do. Lịch đã duyệt nếu cần đổi vui lòng liên hệ phòng khám.", "Note: Only appointments awaiting confirmation can be rescheduled or edited. Contact the clinic to change an approved appointment."),
            Pair("Đổi giờ / Sửa lịch", "Reschedule / Edit"),
            Pair("Hủy lịch hẹn này", "Cancel this appointment"),
            Pair("Đã xác nhận", "Confirmed"),
            Pair("Đã hủy", "Cancelled"),
            Pair("Chưa thanh toán", "Unpaid"),
            Pair("Đã thanh toán", "Paid"),
            Pair("Tiền mặt", "Cash"),
            Pair("Chuyển khoản", "Bank transfer"),
            Pair("Đang xử lý", "Processing"),
            Pair("Hoàn thành", "Completed"),
            Pair("Xem viện phí", "View payments"),
            Pair("Địa điểm khám", "Appointment location"),
            Pair("Mã lịch", "Schedule ID"),
            Pair("Khám định kỳ", "Routine checkup"),
            Pair("Bác sĩ khám", "Examining doctor"),
            Pair("Bác sĩ:", "Doctor:"),
            Pair("Chuyên khoa:", "Specialty:"),
            Pair("Phòng:", "Room:"),
            Pair("Lịch trực & Phòng", "Shift & room"),
            Pair("Ngày & Giờ *", "Date & time *"),
            Pair("Email thông báo", "Notification alerts"),
            Pair("Lịch hẹn sẽ ở trạng thái chờ xác nhận sau khi gửi yêu cầu.", "The appointment will be pending confirmation after you submit the request."),
            Pair("Chọn bác sĩ để xem phòng khám.", "Select a doctor to view their clinic room."),
            Pair("Thông tin bác sĩ & Lịch trực", "Doctor & shift information"),
            Pair("Hiện chưa có bác sĩ được phân công phòng khám.", "No doctors are currently assigned to the clinic."),
            Pair("Chọn bác sĩ để xem phòng khám và ca trực.", "Select a doctor to view their clinic room and shift."),
            Pair("Ca trực hôm nay", "Today's shift"),
            Pair("Lịch hẹn này đã qua giờ hẹn, đã khám hoặc không còn hiệu lực.", "This appointment has passed, has been completed, or is no longer active."),
            Pair("✓ Đơn đang chờ phòng khám duyệt: Bạn CÓ THỂ bấm 'Đổi giờ / Sửa' ở góc phải để đổi ngày giờ hoặc cập nhật lý do khám.", "✓ This appointment is awaiting clinic approval. You can use 'Reschedule / Edit' to change its time or reason."),
            Pair("🔒 ĐƠN ĐÃ ĐƯỢC DUYỆT: Lịch hẹn và bác sĩ đã được chốt, KHÔNG THỂ thay đổi giờ hay lý do khám. Nếu cần đổi giờ, vui lòng liên hệ phòng khám hoặc hủy lịch để đặt lại.", "🔒 APPROVED: The appointment and doctor are confirmed. To make changes, contact the clinic or cancel and book again."),
            Pair("Đơn khám này ĐÃ ĐƯỢC DUYỆT nên không thể thay đổi giờ hoặc lý do khám.\r\n\r\nNếu có nhu cầu thay đổi, bạn vui lòng liên hệ trực tiếp phòng khám hoặc hủy lịch hẹn này để đăng ký lịch mới.", "This appointment has been APPROVED and its time or reason cannot be changed.\r\n\r\nTo make changes, contact the clinic or cancel this appointment and submit a new request."),
            Pair("Không thể sửa lịch", "Unable to edit appointment"),
            Pair("Lịch hẹn này không còn ở trạng thái có thể hủy.", "This appointment can no longer be cancelled."),
            Pair("Không còn giờ khám", "No appointment times available"),
            Pair("Đã duyệt", "Approved"),
            Pair("Chờ duyệt", "Awaiting approval"),
            Pair("Bị từ chối", "Rejected"),
            Pair("Đã đến", "Arrived"),
            Pair("Mã lịch hẹn", "Appointment ID"),
            Pair("Trạng thái duyệt", "Approval status"),
            Pair("Ngày hẹn", "Appointment date"),
            Pair("Giờ hẹn", "Appointment time"),
            Pair("Thời gian hẹn", "Scheduled time"),
            Pair("Ngày lập", "Created date"),
            Pair("Ngày lập hóa đơn", "Invoice date"),
            Pair("Ngày khám", "Examination date"),
            Pair("Mã phiếu khám", "Examination ID"),
            Pair("Mã phiếu", "Visit ID"),
            Pair("Mã hóa đơn", "Invoice ID"),
            Pair("Mã đơn thuốc", "Prescription ID"),
            Pair("Mã dịch vụ", "Service ID"),
            Pair("Mã chỉ định", "Order ID"),
            Pair("Tên dịch vụ kỹ thuật", "Diagnostic service"),
            Pair("Kết quả cận lâm sàng", "Diagnostic result"),
            Pair("Chỉ định", "Order"),
            Pair("Triệu chứng", "Symptoms"),
            Pair("Chẩn đoán", "Diagnosis"),
            Pair("Lời dặn", "Instructions"),
            Pair("Tiền dịch vụ", "Service amount"),
            Pair("Tiền thuốc", "Medicine amount"),
            Pair("Tổng tiền", "Total"),
            Pair("Tổng chi phí (đ)", "Total cost (VND)"),
            Pair("Tổng viện phí", "Total charges"),
            Pair("Bảo hiểm", "Insurance"),
            Pair("Tiền khám (đ)", "Examination fee (VND)"),
            Pair("Tiền dịch vụ (đ)", "Service fee (VND)"),
            Pair("Tiền thuốc (đ)", "Medicine fee (VND)"),
            Pair("Giảm trừ (đ)", "Discount (VND)"),
            Pair("Phương thức thanh toán", "Payment method"),
            Pair("Hình thức thanh toán", "Payment type"),
            Pair("Còn nợ (đ)", "Outstanding balance (VND)"),
            Pair("Đã thu (đ)", "Amount paid (VND)"),
            Pair("Còn nợ", "Balance due"),
            Pair("Đã thu", "Paid"),
            Pair("Số tiền (đ)", "Amount (VND)"),
            Pair("Doanh thu (đ)", "Revenue (VND)"),
            Pair("Tên thuốc", "Medicine"),
            Pair("Tên thuốc / Biệt dược", "Medicine / Brand"),
            Pair("Đơn vị tính", "Unit"),
            Pair("Số lượng", "Quantity"),
            Pair("Đơn giá", "Unit price"),
            Pair("Đơn giá (đ)", "Unit price (VND)"),
            Pair("Thành tiền", "Amount"),
            Pair("Thành tiền (đ)", "Line total (VND)"),
            Pair("Tên dịch vụ", "Service"),
            Pair("Cách dùng / Liều lượng", "Directions / Dosage"),
            Pair("Ngày thanh toán", "Payment date"),
            Pair("Chưa cập nhật", "Not updated"),
            Pair("Mã BN", "Patient code"),
            Pair("Họ tên bệnh nhân", "Patient name"),
            Pair("Phái", "Sex"),
            Pair("Tuổi", "Age"),
            Pair("Năm sinh", "Birth year"),
            Pair("CCCD / CMND", "National ID"),
            Pair("Số BHYT", "Health insurance number"),
            Pair("Nghề nghiệp", "Occupation"),
            Pair("Ghi chú", "Notes"),
            Pair("Trạng thái khám", "Visit status"),
            Pair("STT", "No."),
            Pair("Bác sĩ phụ trách", "Attending doctor"),
            Pair("Phòng khám", "Clinic room"),
            Pair("Khoa / Chuyên khoa", "Department / Specialty"),
            Pair("Chẩn đoán chính", "Primary diagnosis"),
            Pair("Chẩn đoán phụ", "Secondary diagnosis"),
            Pair("Lời dặn bác sĩ", "Physician's note"),
            Pair("Khám lâm sàng", "Clinical examination"),
            Pair("Mạch", "Pulse"),
            Pair("Huyết áp", "Blood pressure"),
            Pair("Nhiệt độ", "Temperature"),
            Pair("Nhịp thở", "Respiratory rate"),
            Pair("Cân nặng (kg)", "Weight (kg)"),
            Pair("Chiều cao (cm)", "Height (cm)"),
            Pair("SpO2 (%)", "SpO2 (%)"),
            Pair("Ngày", "Date"),
            Pair("Cách dùng", "Directions"),
            Pair("Kết quả", "Result"),
            Pair("Giảm giá", "Discount"),
            Pair("NHẮC NHỞ SỨC KHỎE", "HEALTH REMINDERS"),
            Pair("Theo dõi lịch tái khám theo hướng dẫn của bác sĩ. Nếu triệu chứng trở nặng, hãy liên hệ cơ sở y tế.", "Follow your doctor's advice for follow-up visits. Contact a healthcare provider if symptoms worsen."),
            Pair("Mở trợ lý An Tâm", "Open An Tam assistant"),
            Pair("Đặt lịch hẹn mới", "Book a new appointment"),
            Pair("Lịch hẹn của tôi", "My appointments"),
            Pair("Trạng thái", "Status"),
            Pair("Lưu ý", "Note"),
            Pair("Hành trình chăm sóc", "Care journey"),
            Pair("Thông tin lịch khám", "Visit information"),
            Pair("Quay lại danh sách", "Back to list"),
            Pair("Chuyên khoa", "Specialty"),
            Pair("Giờ khám", "Visit time"),
            Pair("Giờ:", "Time:"),
            Pair("Lý do khám", "Reason for visit"),
            Pair("Chọn bác sĩ để xem lịch trực và phòng làm việc.", "Select a doctor to view their schedule and room."),
            Pair("Lịch tuần", "Weekly schedule"),
            Pair("Lịch trực hôm nay", "Today's duty schedule"),
            Pair("Nhập email nhận xác nhận lịch & biên lai...", "Enter an email for appointment confirmations and receipts..."),
            Pair("Gửi yêu cầu đặt lịch", "Submit appointment request"),
            Pair("Lưu thay đổi", "Save changes"),
            Pair("Đóng", "Close"),
            Pair("Chọn chuyên khoa, bác sĩ và khung giờ phù hợp.", "Choose a specialty, doctor, and available time."),
            Pair("Hiện chưa có bác sĩ được phân công chuyên khoa để đặt lịch. Vui lòng liên hệ lễ tân.", "No doctors are currently assigned to this specialty. Please contact reception."),
            Pair("Yêu cầu đặt lịch cần được phòng khám xác nhận. Nếu cần hỗ trợ khẩn cấp, vui lòng gọi cấp cứu hoặc đến cơ sở y tế gần nhất.", "Appointment requests require clinic confirmation. For emergencies, call emergency services or visit the nearest healthcare facility."),
            Pair("Đặt lịch khám", "Schedule a visit"),
            Pair("Sửa lịch hẹn", "Edit appointment"),
            Pair("Sửa yêu cầu đặt lịch", "Edit appointment request"),
            Pair("Đã gửi yêu cầu đặt lịch. Vui lòng theo dõi trạng thái trong mục Lịch hẹn.", "Your appointment request was submitted. Check its status under Appointments."),
            Pair("Đã cập nhật lịch hẹn của bạn.", "Your appointment has been updated."),
            Pair("Đặt lịch thành công", "Appointment booked"),
            Pair("Cập nhật thành công", "Update successful"),
            Pair("Chi tiết lịch hẹn", "Appointment details"),
            Pair("Bạn có thể xem hoặc gửi yêu cầu đặt lịch trong mục Lịch hẹn. Lịch mới cần phòng khám xác nhận.", "View or request appointments under Appointments. New requests require clinic confirmation."),
            Pair("Hóa đơn và trạng thái thanh toán có trong mục Thanh toán viện phí. Với hóa đơn chưa thanh toán, vui lòng liên hệ quầy thu ngân.", "Invoices and payment status are under Payments. For unpaid invoices, contact the cashier."),
            Pair("Lịch sử khám, kết quả dịch vụ và đơn thuốc được hiển thị trong mục Hồ sơ khám bệnh.", "Visit history, diagnostic results, and prescriptions are under Medical records."),
            Pair("Tôi chỉ hỗ trợ hướng dẫn sử dụng cổng bệnh nhân. Với tư vấn triệu chứng, chẩn đoán hoặc điều trị, vui lòng liên hệ bác sĩ.", "I can only help you use the patient portal. Contact a doctor for symptom, diagnosis, or treatment advice."),
            Pair("Bạn có thể hỏi", "You can ask"),
            Pair("Làm sao để xem lịch hẹn?", "How do I view my appointments?"),
            Pair("Tôi xem hóa đơn ở đâu?", "Where can I view my invoices?"),
            Pair("Tôi tìm đơn thuốc ở đâu?", "Where can I find my prescriptions?"),
            Pair("Thông tin y tế", "Medical information"),
            Pair("Không dùng trợ lý để tự chẩn đoán hay thay đổi thuốc. Nếu có dấu hiệu khẩn cấp, hãy gọi cấp cứu hoặc đến cơ sở y tế gần nhất.", "Do not use the assistant to self-diagnose or change medication. For emergencies, call emergency services or visit the nearest healthcare facility."),
            Pair("Nhập câu hỏi...", "Type your question..."),
            Pair("Gửi", "Send"),
            Pair("Trợ lý An Tâm: Xin chào! Tôi có thể hướng dẫn bạn sử dụng các mục lịch hẹn, hồ sơ sức khỏe và viện phí.",
                "An Tam Assistant: Hello! I can help you use appointments, health records, and payment features."),
            Pair("Không tải được dữ liệu bệnh nhân", "Could not load patient data"),
            Pair("Xác nhận hủy", "Confirm cancellation"),
            Pair("Bạn chắc chắn muốn hủy lịch hẹn này?", "Are you sure you want to cancel this appointment?"),
            Pair("Không thể hủy lịch", "Unable to cancel appointment"),
            Pair("Vui lòng chọn bác sĩ.", "Please select a doctor."),
            Pair("Thiếu thông tin", "Missing information"),
            Pair("Ngày đã chọn không còn khung giờ khám. Vui lòng chọn ngày khác.", "No appointment times are available on the selected date. Choose another date."),
            Pair("Không thể lưu lịch hẹn", "Unable to save appointment"),
            Pair("Không thể mở biểu mẫu đặt lịch", "Unable to open appointment form"),
            Pair("Bác sĩ không có slot 15 phút khả dụng. Vui lòng chọn ngày khác.", "The doctor has no available 15-minute slots. Choose another date."),
            Pair("Hiện chưa có bác sĩ phù hợp để đặt lịch.", "No suitable doctors are currently available."),
            Pair("Chưa có bác sĩ", "No doctors available"),
            Pair("Chuyên khoa này hiện chưa có bác sĩ được phân công để đặt lịch.", "No doctor is currently assigned to this specialty for booking."),
            Pair("Không thể đặt lịch", "Unable to book appointment"),
            Pair("Lỗi tải dữ liệu", "Data loading error"),
            Pair("Đơn khám này ĐÃ ĐƯỢC DUYỆT", "This appointment has been APPROVED"),
            Pair("Đặt lịch khám trước", "Pre-book a visit"),
            Pair("Làm mới", "Refresh"),
            Pair("Mở ca", "Open shift"),
            Pair("Đóng ca", "Close shift"),
            Pair("Ca thu ngân đang mở:", "Cashier shift open:"),
            Pair("Cần mở ca trước khi ghi nhận thanh toán.", "Open a cash shift before recording payments."),
            Pair("Lập hóa đơn và thu tiền bệnh nhân tại phòng được phân công.", "Create invoices and collect payments for patients in your assigned rooms."),
            Pair("Phòng quản lý:", "Managed room:"),
            Pair("Tài khoản này chưa được phân công phòng khám.", "This account is not assigned to a clinic room."),
            Pair("Bạn không được phân công quản lý thanh toán của phòng khám này.", "You are not assigned to manage payments for this clinic room."),
            Pair("PHIẾU KHÁM ĐÃ HOÀN TẤT — CHỜ LẬP HÓA ĐƠN", "COMPLETED VISITS — AWAITING INVOICE"),
            Pair("HÓA ĐƠN CHƯA THANH TOÁN", "UNPAID INVOICES"),
            Pair("Lập hóa đơn khám", "Create visit invoice"),
            Pair("Thanh toán hóa đơn", "Pay invoice"),
            Pair("Y tá / thu ngân nhận bảng kê:", "Nurse / cashier assigned to the bill:"),
            Pair("Y tá nhận bảng kê", "Assigned nurse"),
            Pair("Phòng này chưa được phân công y tá / thu ngân đang hoạt động.", "No active nurse / cashier is assigned to this room."),
            Pair("Bảng kê sẽ được giao cho nhân viên này.", "The bill will be assigned to this staff member."),
            Pair("Chọn một nhân viên được phân công cùng phòng khám.", "Choose a staff member assigned to this clinic room."),
            Pair("Bảng kê chỉ hiển thị trong hàng chờ của y tá được giao.", "The bill will appear only in the assigned nurse's queue."),
            Pair("Ca khám ", "Visit "),
            Pair(" đã hoàn tất", " is complete"),
            Pair("Bệnh nhân: ", "Patient: "),
            Pair(" đã khám xong. Bảng kê được bác sĩ giao cho bạn để lập hóa đơn / thanh toán.", " has completed the visit. The doctor assigned the bill to you for invoicing / payment."),
            Pair("🚀 GỬI BẢNG KÊ CHO Y TÁ / THU NGÂN", "🚀 SEND BILL TO NURSE / CASHIER"),
            Pair("Chọn y tá / thu ngân nhận bảng kê trước khi hoàn tất ca khám.", "Select a nurse / cashier for the bill before completing the visit."),
            Pair("Chưa chọn người nhận", "No recipient selected"),
            Pair("Không thể hoàn tất phiếu khám. Vui lòng kiểm tra phiếu và phân công của y tá / thu ngân.", "Unable to complete the visit. Check the visit and the nurse / cashier assignment."),
            Pair("Đã chuyển bảng kê cho ", "The bill was assigned to "),
            Pair("Đã hoàn tất ca khám và chuyển bảng kê cho ", "The visit is complete and the bill was assigned to "),
            Pair("Bạn có muốn tạo lịch tái khám nháp cho bệnh nhân không?", "Would you like to create a draft follow-up appointment for the patient?"),
            Pair("Tạo lịch tái khám", "Create follow-up appointment"),
            Pair("Hoàn tất thành công", "Completed successfully"),
            Pair("Hôm nay", "Today"),
            Pair("Đăng xuất", "Sign out"),
            Pair("Thoát tài khoản", "Exit account"),
            Pair("Cấu hình máy chủ", "Server settings"),
            Pair("Mã hồ sơ", "Patient ID"),
            Pair("Họ và tên", "Full name"),
            Pair("Ngày sinh", "Date of birth"),
            Pair("Giới tính", "Gender"),
            Pair("Số điện thoại", "Phone number"),
            Pair("Email nhận thông báo", "Notification email"),
            Pair("Địa chỉ", "Address"),
            Pair("Tiền sử bệnh / dị ứng", "Medical history / allergies"),
            Pair("Lịch sử khám", "Visit history"),
            Pair("Đơn thuốc", "Prescriptions"),
            Pair("Hồ sơ khám bệnh của tôi", "My medical records"),
            Pair("Cập nhật thông tin và xem lịch sử khám", "Update your information and view your visit history"),
            Pair("Cập nhật thông tin và xem lịch sử khám của", "Update your information and view the visit history of"),
            Pair("CHI TIẾT PHIẾU KHÁM ĐANG CHỌN", "SELECTED EXAMINATION DETAILS"),
            Pair("Chọn một ca khám ở bảng trên để xem chi tiết.", "Select an examination above to view details."),
            Pair("Sinh hiệu", "Vitals"),
            Pair("Chưa ghi nhận", "Not recorded"),
            Pair("Triệu chứng ban đầu", "Initial symptoms"),
            Pair("Lời dặn của bác sĩ", "Doctor's instructions"),
            Pair("Tuân thủ theo chỉ dẫn đơn thuốc", "Follow the prescription instructions"),
            Pair("Bác sĩ trực hôm nay tại phòng khám", "Clinic doctors currently on duty"),
            Pair("Bác sĩ trực hôm nay", "On-duty doctors today"),
            Pair("Nếu bác sĩ đang trong ca trực", "If a doctor is currently on duty"),
            Pair("Hôm nay phòng khám chưa có ca trực của bác sĩ hoặc đang ngoài giờ tiếp nhận.", "No doctors are on duty now, or the clinic is closed."),
            Pair("Vui lòng đặt lịch hẹn trước.", "Please book an appointment in advance."),
            Pair("Đặt lịch hẹn", "Book appointment"),
            Pair("Khám hôm nay", "Visit today"),
            Pair("Đang có mặt", "On duty"),
            Pair("Chờ vào ca", "Waiting for shift"),
            Pair("Ca trực", "Shift"),
            Pair("BỆNH NHÂN ĐANG KHÁM", "PATIENT UNDER EXAMINATION"),
            Pair("Đang khám", "In progress"),
            Pair("Chưa có ca khám", "No examination"),
            Pair("Không có", "None"),
            Pair("Tổng ca hôm nay", "Today's total cases"),
            Pair("Đã hoàn tất", "Finished"),
            Pair("Đang chờ khám", "Waiting for examination"),
            Pair("Cận lâm sàng", "Diagnostics"),
            Pair("Chờ xác nhận", "Pending confirmation"),
            Pair("Không đến", "Did not attend"),
            Pair("HÀNG CHỜ KHÁM", "EXAMINATION QUEUE"),
            Pair("LỊCH HẸN HÔM NAY", "TODAY'S APPOINTMENTS"),
            Pair("CHỜ DUYỆT", "PENDING APPROVAL"),
            Pair("DANH SÁCH TIẾP NHẬN HÔM NAY", "TODAY'S RECEPTION LIST"),
            Pair("TIẾP NHẬN BỆNH NHÂN", "PATIENT RECEPTION"),
            Pair("Tiếp nhận", "Receive patient"),
            Pair("Thêm lịch hẹn", "Add appointment"),
            Pair("Phiếu trong phạm vi phòng được giao", "Cases in assigned rooms"),
            Pair("Lịch hẹn trong ngày hôm nay", "Appointments today"),
            Pair("Lịch đặt chờ duyệt toàn viện", "Clinic-wide appointments pending approval"),
            Pair("Lịch vắng trong ngày", "Today's absences"),
            Pair("Quầy tiếp đón trung tâm", "Central reception desk"),
            Pair("Đang trực tại", "On duty at"),
            Pair("Tổ phụ trách", "Assigned team"),
            Pair("Bác sĩ & Y tá / Điều dưỡng", "Doctors & nurses"),
            Pair("Tổng số ca khám được phân công", "Total assigned examination cases"),
            Pair("Bệnh nhân đã kết thúc khám", "Patients who completed their visit"),
            Pair("bệnh nhân chờ vào phòng", "patients waiting to enter"),
            Pair("Chờ / Đã có kết quả CLS", "Pending / completed diagnostic results"),
            Pair("Bàn làm việc", "Workspace"),
            Pair("Chào Bác sĩ", "Welcome, Doctor"),
            Pair("Ca làm việc", "Work shift"),
            Pair("Thông báo", "Notifications"),
            Pair("Thông báo hệ thống", "System notifications"),
            Pair("Đánh dấu tất cả đã đọc", "Mark all as read"),
            Pair("Không có thông báo nào.", "No notifications."),
            Pair("Không tìm thấy hồ sơ bệnh nhân.", "Patient record not found."),
            Pair("Bạn muốn thoát tài khoản và quay lại màn hình đăng nhập?", "Do you want to sign out and return to the login screen?"),
            Pair("Không thể mở hồ sơ", "Unable to open profile"),
            Pair("Không xác định được mã nhân viên đang đăng nhập.", "Could not identify the signed-in employee."),
            Pair("Lỗi tải hồ sơ", "Profile loading error"),
            Pair("Không thể tải hồ sơ cá nhân.", "Unable to load your profile.")
        };

        private sealed class TextSnapshot
        {
            public string Original { get; set; } = "";
            public string LastTranslated { get; set; } = "";
            public string OriginalPlaceholder { get; set; } = "";
            public string LastTranslatedPlaceholder { get; set; } = "";
            public bool IsApplied { get; set; }
            public bool IsWatching { get; set; }
        }

        private static readonly ConditionalWeakTable<Control, TextSnapshot> ControlTexts = new();
        private static readonly ConditionalWeakTable<DataGridViewColumn, TextSnapshot> ColumnTexts = new();
        private static bool _isEnglish;
        private static bool _applying;

        public static bool IsEnglish
        {
            get => _isEnglish;
            set => _isEnglish = value;
        }

        public static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            IEnumerable<KeyValuePair<string, string>> replacements = _isEnglish
                ? Translations.OrderByDescending(pair => pair.Key.Length)
                : Translations.Select(pair => Pair(pair.Value, pair.Key))
                    .OrderByDescending(pair => pair.Key.Length);

            foreach (var pair in replacements)
                text = text.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
            return text;
        }

        public static void Apply(Control control)
        {
            TextSnapshot snapshot = ControlTexts.GetValue(control, key => new TextSnapshot { Original = key.Text });
            if (control is not TextBox { ReadOnly: false })
                ApplyText(control, snapshot);

            if (control is TextBox textBox && !string.IsNullOrEmpty(textBox.PlaceholderText))
            {
                if (snapshot.IsApplied && textBox.PlaceholderText != snapshot.LastTranslatedPlaceholder)
                    snapshot.OriginalPlaceholder = textBox.PlaceholderText;
                else if (string.IsNullOrEmpty(snapshot.OriginalPlaceholder))
                    snapshot.OriginalPlaceholder = textBox.PlaceholderText;

                snapshot.LastTranslatedPlaceholder = Translate(snapshot.OriginalPlaceholder);
                textBox.PlaceholderText = snapshot.LastTranslatedPlaceholder;
            }

            if (control is DataGridView grid)
            {
                foreach (DataGridViewColumn column in grid.Columns)
                    ApplyColumnText(column);
                grid.Invalidate();
            }

            foreach (Control child in control.Controls)
                Apply(child);
        }

        public static void SetText(Control control, string text)
        {
            TextSnapshot snapshot = ControlTexts.GetValue(control, key => new TextSnapshot { Original = key.Text });
            snapshot.Original = text;
            ApplyText(control, snapshot);
        }

        public static void Watch(Control control)
        {
            control.ControlAdded += (sender, args) =>
            {
                if (args.Control == null)
                    return;
                Watch(args.Control);
                Apply(args.Control);
            };

            TextSnapshot snapshot = ControlTexts.GetValue(control, key => new TextSnapshot { Original = key.Text });
            if (!snapshot.IsWatching &&
                (control is Label or Button or TabPage || control is TextBox { ReadOnly: true }))
            {
                snapshot.IsWatching = true;
                control.TextChanged += (sender, args) =>
                {
                    if (_applying)
                        return;

                    snapshot.Original = control.Text;
                    ApplyText(control, snapshot);
                };
            }

            if (control is DataGridView grid)
            {
                grid.ColumnAdded += (sender, args) =>
                {
                    ColumnTexts.GetValue(args.Column, column => new TextSnapshot { Original = column.HeaderText });
                    ApplyColumnText(args.Column);
                };
                grid.DataBindingComplete += (sender, args) =>
                {
                    foreach (DataGridViewColumn column in grid.Columns)
                        ApplyColumnText(column);
                };
                grid.CellFormatting += (sender, args) =>
                {
                    if (args.Value is not string value || args.ColumnIndex < 0)
                        return;

                    string columnName = grid.Columns[args.ColumnIndex].Name;
                    if (columnName.Equals("TrangThai", StringComparison.OrdinalIgnoreCase) ||
                        columnName.Equals("TrangThaiDuyet", StringComparison.OrdinalIgnoreCase) ||
                        columnName.Equals("TrangThaiTT", StringComparison.OrdinalIgnoreCase) ||
                        columnName.Equals("HinhThucTT", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Value = Translate(value);
                        args.FormattingApplied = true;
                    }
                };
                foreach (DataGridViewColumn column in grid.Columns)
                    ColumnTexts.GetValue(column, key => new TextSnapshot { Original = key.HeaderText });
            }

            foreach (Control child in control.Controls)
                Watch(child);
        }

        private static void ApplyText(Control control, TextSnapshot snapshot)
        {
            if (snapshot.IsApplied && control.Text != snapshot.LastTranslated)
                snapshot.Original = control.Text;

            string translated = Translate(snapshot.Original);
            if (!string.Equals(control.Text, translated, StringComparison.Ordinal))
            {
                _applying = true;
                try
                {
                    control.Text = translated;
                }
                finally
                {
                    _applying = false;
                }
            }
            snapshot.LastTranslated = translated;
            snapshot.IsApplied = true;
        }

        private static void ApplyColumnText(DataGridViewColumn column)
        {
            TextSnapshot snapshot = ColumnTexts.GetValue(column, key => new TextSnapshot { Original = key.HeaderText });
            if (snapshot.IsApplied && column.HeaderText != snapshot.LastTranslated)
                snapshot.Original = column.HeaderText;
            snapshot.LastTranslated = Translate(snapshot.Original);
            snapshot.IsApplied = true;
            column.HeaderText = snapshot.LastTranslated;
        }

        private static KeyValuePair<string, string> Pair(string vietnamese, string english)
            => new(vietnamese, english);
    }
}
