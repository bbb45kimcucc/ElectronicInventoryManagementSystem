using System;
using System.ComponentModel.DataAnnotations;

namespace ElectronicInventoryManagementSystem.Models
{
    public class ActionRequest
    {
        [Key]
        public int Id { get; set; }

        public string ActionType { get; set; }

        public int TargetId { get; set; }

        // Tên của mục đó cho Admin dễ đọc (VD: "Khách hàng Kim Anh")
        public string Content { get; set; }

        // Lý do xóa (Nhân viên nhập)
        public string? Reason { get; set; }

        // Trạng thái: "Pending" (Chờ duyệt), "Approved" (Đã duyệt), "Rejected" (Từ chối)
        public string Status { get; set; } = "Pending";

        public string? CreatedBy { get; set; } // Email người yêu cầu
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}