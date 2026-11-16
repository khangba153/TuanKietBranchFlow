using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services.Orders;

public class OrderAdjustmentService : IOrderAdjustmentService
{
    private readonly IOrderManagementService _orderManagementService;
    private readonly IOrderMenuRepository _orderMenuRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderAdjustmentService(
        IOrderManagementService orderManagementService,
        IOrderMenuRepository orderMenuRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _orderManagementService = orderManagementService;
        _orderMenuRepository = orderMenuRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    // Chỉ ADMIN trong phạm vi và đúng trạng thái mới được lấy lựa chọn chỉnh đơn.
    public async Task<OrderAdjustmentResultDTO> GetOptionsAsync(int userId, string role, int orderId)
    {
        if (userId <= 0 || role != "ADMIN")
        {
            return new()
            {
                IsForbidden = true
            };
        }

        OrderManagementDetailDTO? order = await _orderManagementService.GetDetailAsync(userId, role, orderId);
        if (order == null)
        {
            return new()
            {
                IsNotFound = true
            };
        }

        if (order.Status != "NEEDS_REVIEW")
        {
            return new()
            {
                IsConflict = true
            };
        }

        return new()
        {
            Before = order,
            Options = await LoadOptionsAsync(order)
        };
    }

    // Tính giá ở Backend; Preview không mở transaction hoặc ghi database.
    public async Task<OrderAdjustmentResultDTO> PreviewAsync(int userId, string role, int orderId, AdjustOrderRequestDTO request)
    {
        if (userId <= 0 || role != "ADMIN")
        {
            return new()
            {
                IsForbidden = true
            };
        }

        if (request.Items == null || request.Items.Count < 1 || request.Items.Count > 100 || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500 || request.ExpectedReportedAt == null || request.ExpectedHistoryId < 0)
        {
            return Invalid("Đơn phải còn ít nhất một dòng món và có lý do 1–500 ký tự.");
        }

        OrderManagementDetailDTO? before = await _orderManagementService.GetDetailAsync(userId, role, orderId);
        if (before == null)
        {
            return new()
            {
                IsNotFound = true
            };
        }

        if (before.Status != "NEEDS_REVIEW" || before.ReportedAt != request.ExpectedReportedAt || before.LatestHistoryId != request.ExpectedHistoryId)
        {
            return new()
            {
                IsConflict = true
            };
        }

        if (request.Items.Any(item => item == null || item.Quantity < 1 || item.Quantity > 999 || item.Toppings == null || item.NoteOptionIds == null || item.Toppings.Count > 100 || item.NoteOptionIds.Count > 100) || request.Items.Select(item => item.OrderItemId).Distinct().Count() != request.Items.Count)
        {
            return Invalid("Dòng món bị trùng hoặc số lượng/lựa chọn không hợp lệ.");
        }

        OrderAdjustmentOptionsDTO options = await LoadOptionsAsync(before);
        OrderManagementDetailDTO after = JsonSerializer.Deserialize<OrderManagementDetailDTO>(OrderSnapshot.Serialize(before))!;
        after.Items.Clear();
        after.TotalAmount = 0;
        after.TotalQuantity = 0;
        after.Status = "ADJUSTED";
        foreach (AdjustOrderItemRequestDTO row in request.Items.OrderBy(item => item.OrderItemId))
        {
            OrderManagementItemDTO? original = before.Items.FirstOrDefault(item => item.Id == row.OrderItemId);
            if (original == null)
            {
                return Invalid("Không được thêm món mới hoặc dùng dòng món của đơn khác.");
            }

            bool changedSize = row.ProductSizeId != original.ProductSizeId;
            OrderAdjustmentSizeDTO? newSize = options.Sizes.FirstOrDefault(size => size.Id == row.ProductSizeId && size.ProductId == original.ProductId);
            if (row.ProductSizeId <= 0 || (changedSize && newSize == null))
            {
                return Invalid($"Size mới của {original.ProductName} không còn được bán hoặc thuộc món khác.");
            }

            OrderManagementItemDTO item = new()
            {
                Id = original.Id,
                ProductId = original.ProductId,
                ProductSizeId = row.ProductSizeId,
                ProductName = original.ProductName,
                SizeName = changedSize ? newSize!.Name : original.SizeName,
                Quantity = row.Quantity,
                UnitPrice = changedSize ? newSize!.Price : original.UnitPrice
            };
            if (row.Toppings.Any(top => top == null) || row.Toppings.Select(top => top.ToppingId).Distinct().Count() != row.Toppings.Count)
            {
                return Invalid("Topping không được trùng trong một dòng món.");
            }

            decimal configuredPrice = item.UnitPrice;
            foreach (var selected in row.Toppings.OrderBy(top => top.ToppingId))
            {
                OrderAdjustmentToppingDTO? current = options.Toppings.FirstOrDefault(top => top.Id == selected.ToppingId);
                if (current == null || selected.Quantity < 1 || selected.Quantity > current.MaxQuantity)
                {
                    return Invalid("Topping không còn hợp lệ hoặc vượt giới hạn số lượng. Hãy bỏ/chọn lại.");
                }

                OrderManagementToppingDTO? old = original.Toppings.FirstOrDefault(top => top.ToppingId == selected.ToppingId);
                decimal price = old?.UnitPrice ?? current.Price;
                item.Toppings.Add(new()
                {
                    ToppingId = current.Id,
                    Name = old?.Name ?? current.Name,
                    Quantity = selected.Quantity,
                    UnitPrice = price
                });
                configuredPrice += price * selected.Quantity;
            }

            List<int> noteGroups = new();
            foreach (int selected in row.NoteOptionIds.OrderBy(id => id))
            {
                OrderAdjustmentNoteDTO? note = options.Notes.FirstOrDefault(note => note.Id == selected);
                if (note == null || noteGroups.Contains(note.GroupId))
                {
                    return Invalid("Ghi chú không hợp lệ; mỗi nhóm chỉ chọn một lựa chọn.");
                }

                noteGroups.Add(note.GroupId);
                int oldIndex = original.NoteOptionIds.IndexOf(selected);
                item.NoteOptionIds.Add(note.Id);
                item.Notes.Add(oldIndex >= 0 ? original.Notes[oldIndex] : note.Name);
            }

            item.SubtotalAmount = configuredPrice * item.Quantity;
            if (item.UnitPrice < 0 || configuredPrice < 0 || item.SubtotalAmount > 9999999999999999.99m)
            {
                return Invalid("Giá trị đơn vượt phạm vi cho phép.");
            }

            after.TotalAmount += item.SubtotalAmount;
            after.TotalQuantity += item.Quantity;
            after.Items.Add(item);
        }

        if (after.TotalAmount > 9999999999999999.99m)
        {
            return Invalid("Tổng giá trị đơn vượt phạm vi cho phép.");
        }

        if (JsonSerializer.Serialize(before.Items) == JsonSerializer.Serialize(after.Items))
        {
            return Invalid("Chưa có thay đổi. Dùng Giữ nguyên đơn nếu không cần điều chỉnh.");
        }

        // Hash chỉ phát hiện thay đổi giá/dữ liệu giữa preview và submit, không thay thế quyền truy cập.
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId + "|" + before.LatestHistoryId + "|" + request.Reason.Trim() + "|" + OrderSnapshot.Serialize(before) + "|" + OrderSnapshot.Serialize(after))));
        return new()
        {
            Before = before,
            After = after,
            PreviewHash = hash
        };
    }

    // Tính lại giá rồi giành quyền xử lý; dữ liệu món, tổng tiền và history commit cùng nhau.
    public async Task<OrderAdjustmentResultDTO> AdjustAsync(int userId, string role, int orderId, AdjustOrderRequestDTO request)
    {
        OrderAdjustmentResultDTO result = await PreviewAsync(userId, role, orderId, request);
        if (result.After == null)
        {
            return result;
        }

        if (string.IsNullOrEmpty(request.PreviewHash) || request.PreviewHash != result.PreviewHash)
        {
            return new()
            {
                IsConflict = true,
                Error = "Giá hoặc nội dung đã thay đổi. Hãy xem trước lại."
            };
        }

        DateTime now = DateTime.UtcNow;
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            int changed = await _orderRepository.TryResolveReviewAsync(orderId, userId, DateOnly.FromDateTime(DateTime.Today), request.ExpectedReportedAt!.Value, request.ExpectedHistoryId, "ADJUSTED", now);
            if (changed != 1)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new()
                {
                    IsConflict = true
                };
            }

            List<OrderItem> items = new();
            foreach (OrderManagementItemDTO row in result.After.Items)
            {
                OrderItem item = new()
                {
                    Id = row.Id,
                    ProductSizeId = row.ProductSizeId,
                    ProductNameSnapshot = row.ProductName,
                    SizeNameSnapshot = row.SizeName,
                    Quantity = row.Quantity,
                    UnitPriceSnapshot = row.UnitPrice,
                    SubtotalAmount = row.SubtotalAmount
                };
                foreach (var top in row.Toppings)
                {
                    item.OrderItemToppings.Add(new()
                    {
                        ToppingId = top.ToppingId,
                        ToppingNameSnapshot = top.Name,
                        Quantity = top.Quantity,
                        UnitPriceSnapshot = top.UnitPrice
                    });
                }

                for (int index = 0; index < row.NoteOptionIds.Count; index++)
                {
                    item.OrderItemNotes.Add(new()
                    {
                        NoteOptionId = row.NoteOptionIds[index],
                        NoteNameSnapshot = row.Notes[index]
                    });
                }

                items.Add(item);
            }

            await _orderRepository.ApplyAdjustedItemsAsync(orderId, items, result.After.TotalAmount);
            await _orderRepository.AddReviewHistoryAsync(new OrderAdjustment
            {
                SalesOrderId = orderId,
                AdjustedByUserId = userId,
                Reason = request.Reason.Trim(),
                BeforeData = OrderSnapshot.Serialize(result.Before!),
                AfterData = OrderSnapshot.Serialize(result.After),
                CreatedAt = now
            });
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            _unitOfWork.ClearTracking();
            throw;
        }
    }

    // Menu hiện tại chỉ dùng để xác thực lựa chọn mới/đang giữ; không ghi đè giá snapshot cũ.
    private async Task<OrderAdjustmentOptionsDTO> LoadOptionsAsync(OrderManagementDetailDTO order)
    {
        OrderAdjustmentOptionsDTO result = new();
        List<int> productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        foreach (Category category in await _orderMenuRepository.GetAvailableCategoriesAsync(order.BranchId))
        {
            foreach (Product product in category.Products.Where(product => productIds.Contains(product.Id)))
            {
                foreach (ProductSize size in product.ProductSizes)
                {
                    result.Sizes.Add(new()
                    {
                        Id = size.Id,
                        ProductId = product.Id,
                        Name = size.Size.Name,
                        Price = size.Price
                    });
                }
            }
        }

        foreach (ToppingGroup group in await _orderMenuRepository.GetAvailableToppingGroupsAsync(order.BranchId))
        {
            foreach (Topping top in group.Toppings)
            {
                result.Toppings.Add(new()
                {
                    Id = top.Id,
                    Name = top.Name,
                    Price = top.Price,
                    MaxQuantity = string.Equals(group.Name, "Up size", StringComparison.OrdinalIgnoreCase) ? 1 : 3
                });
            }
        }

        foreach (NoteGroup group in await _orderMenuRepository.GetActiveNoteGroupsAsync())
        {
            foreach (NoteOption note in group.NoteOptions)
            {
                result.Notes.Add(new()
                {
                    Id = note.Id,
                    GroupId = group.Id,
                    GroupName = group.Name,
                    Name = note.Name
                });
            }
        }

        return result;
    }

    private static OrderAdjustmentResultDTO Invalid(string message) => new()
    {
        Error = message
    };
}
