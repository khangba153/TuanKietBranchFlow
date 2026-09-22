using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class ReportOrderRequestDTO
{
    // Employee phải mô tả lý do báo sai đơn
    [Required(ErrorMessage = "Lý do báo sai không được để trống")]
    [StringLength(500, MinimumLength = 1,
        ErrorMessage = "Lý do báo sai không được vượt quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}