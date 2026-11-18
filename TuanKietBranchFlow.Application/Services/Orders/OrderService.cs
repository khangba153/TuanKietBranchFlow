using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using TuanKietBranchFlow.Application.DTOs.Orders;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;
using System.Text.Json;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;

namespace TuanKietBranchFlow.Application.Services.Orders;

public class OrderService : IOrderService
{
    private const string UpSizeGroupName = "Up size";
    private const string CompletedStatus = "COMPLETED";
    private const string AdjustedStatus = "ADJUSTED";
    private const string NeedsReviewStatus = "NEEDS_REVIEW";
    private readonly IBranchRepository _branchRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    public OrderService(
        IBranchRepository branchRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    // Kiểm tra request, tính lại giá và lưu toàn bộ trong 1 transaction
    public async Task<CreateOrderResultDTO> CreateOrderAsync(
        int currentUserId,
        CreateOrderRequestDTO request)
    {
        DateOnly businessDate = DateOnly.FromDateTime(DateTime.Today);
        // Bước 1: Kiểm tra chi nhánh có còn hoạt động không
        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(request.BranchId);
        if (branch is null || !branch.IsActive)
        {
            return new CreateOrderResultDTO

            {

                IsBranchFound = false,
                HasAccess = false,
                IsOrderValid = false,
                ErrorMessage = "Chi nhánh không tồn tại hoặc không hoạt động."

            };
        }

        // Bước 2: Kiểm tra employee có được phân công tại chi nhánh
        bool hasAccess = await _branchRepository.HasActiveAssignmentAsync(currentUserId, request.BranchId, businessDate);
        if (!hasAccess)
        {
            return new CreateOrderResultDTO

            {

                IsBranchFound = true,
                HasAccess = false,
                IsOrderValid = false,
                ErrorMessage = "Bạn không được phân công tại chi nhánh này."

            };
        }

        // Service tự bảo vệ dù Controller đã kiểm tra DataAnnotations
        if (request.Items.Count == 0)
        {
            return CreateInvalidResult("Đơn hàng phải có ít nhất một món");
        }

        foreach (CreateOrderItemRequestDTO requestedItem in request.Items)
        {
            if (requestedItem.Quantity <= 0)
            {
                return CreateInvalidResult("Số lượng món phải lớn hơn 0.");
            }
        }

        // Bước 3: Lấy các ID duy nhất để truy vấn db
        List<int> productSizeIds = request.Items.Select(item => item.ProductSizeId).Distinct().ToList();
        List<int> requestedToppingIds = new List<int>();
        List<int> requestedNoteOptionIds = new List<int>();
        foreach (CreateOrderItemRequestDTO requestedItem in request.Items)
        {
            foreach (CreateOrderItemToppingRequestDTO requestedTopping in requestedItem.Toppings)
            {
                requestedToppingIds.Add(requestedTopping.ToppingId);
            }

            foreach (int noteOptionId in requestedItem.NoteOptionIds)
            {
                requestedNoteOptionIds.Add(noteOptionId);
            }
        }

        List<int> toppingIds = requestedToppingIds.Distinct().ToList();
        List<int> noteOptionIds = requestedNoteOptionIds.Distinct().ToList();
        // Bước 4: Backen đọc lại dữ liệu và giá thật từ db
        List<ProductSize> productSizes = await _orderRepository.GetAvailableProductSizesAsync(request.BranchId, productSizeIds);
        List<Topping> toppings = new List<Topping>();
        if (toppingIds.Count > 0)
        {
            toppings = await _orderRepository.GetAvailableToppingsAsync(request.BranchId, toppingIds);
        }

        List<NoteOption> noteOptions = new List<NoteOption>();
        if (noteOptionIds.Count > 0)
        {
            noteOptions = await _orderRepository.GetActiveNoteOptionsAsync(noteOptionIds);
        }

        // Repository bỏ qua ID không hợp lệ nên Service đối chiếu ID duy nhất
        if (productSizes.Count != productSizeIds.Count)
        {
            return CreateInvalidResult("Có món hoặc size không hợp lệ tại chi nhánh.");
        }

        if (toppings.Count != toppingIds.Count)
        {
            return CreateInvalidResult("Có topping không hợp lệ tại chi nhánh.");
        }

        if (noteOptions.Count != noteOptionIds.Count)
        {
            return CreateInvalidResult("Có lựa chọn ghi chú không hợp lệ.");
        }

        // Bước 5: kiểm tra quy tắc riêng trên từng dòng món trong giỏ hàng
        foreach (CreateOrderItemRequestDTO requestedItem in request.Items)
        {
            List<int> selectedToppingIds = new List<int>();
            foreach (CreateOrderItemToppingRequestDTO requestedTopping in requestedItem.Toppings)
            {
                if (selectedToppingIds.Contains(requestedTopping.ToppingId))
                {
                    return CreateInvalidResult("Một topping không được chọn lặp lại trong cùng một món.");
                }

                selectedToppingIds.Add(requestedTopping.ToppingId);
                Topping topping = toppings.First(currentTopping => currentTopping.Id == requestedTopping.ToppingId);
                bool isUpSize = string.Equals(topping.ToppingGroup.Name, UpSizeGroupName, StringComparison.OrdinalIgnoreCase);
                int maximumQuantity = isUpSize ? 1 : 3;
                if (requestedTopping.Quantity <= 0 || requestedTopping.Quantity > maximumQuantity)
                {
                    return CreateInvalidResult($"Số lượng topping {topping.Name} không hợp lệ.");
                }
            }

            List<int> selectedNoteGroupIds = new List<int>();
            foreach (int noteOptionId in requestedItem.NoteOptionIds)
            {
                NoteOption noteOption = noteOptions.First(currentOption => currentOption.Id == noteOptionId);
                // Mỗi nhóm như Đá hoặc Đường chỉ chọn 1 option
                if (selectedNoteGroupIds.Contains(noteOption.NoteGroupId))
                {
                    return CreateInvalidResult("Mỗi nhóm ghi chú chỉ được chọn một lựa chọn.");
                }

                selectedNoteGroupIds.Add(noteOption.NoteGroupId);
            }
        }

        // Bước 6: chỉ mở transaction sau khi request đã hợp lệ
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Cùng khóa tài khoản với điều chuyển: không ghi đơn vào nguồn sau khi chuyển đã commit.
            if (!await _orderRepository.LockAndCheckEmployeeBranchAsync(currentUserId, request.BranchId, businessDate))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new CreateOrderResultDTO

                {

                    IsBranchFound = true,
                    HasAccess = false

                };
            }

            OrderCodeResult orderCodeResult = await _orderRepository.GetNextOrderCodeAsync(request.BranchId, businessDate);
            DateTime createdAt = DateTime.UtcNow;
            SalesOrder order = new SalesOrder

            {

                Code = orderCodeResult.Code,
                BranchId = request.BranchId,
                CreatedByUserId = currentUserId,
                BusinessDate = businessDate,
                DailySequence = orderCodeResult.SequenceNumber,
                TotalAmount = 0m,
                Status = CompletedStatus,
                CreatedAt = createdAt

            };
            // Bước 7: tạo graph đơn và lưu snapshot
            foreach (CreateOrderItemRequestDTO requestedItem in request.Items)
            {
                ProductSize productSize = productSizes.First(currentProductSize => currentProductSize.Id == requestedItem.ProductSizeId);
                OrderItem orderItem = new OrderItem

                {

                    ProductSizeId = productSize.Id,
                    ProductNameSnapshot = productSize.Product.Name,
                    SizeNameSnapshot = productSize.Size.Name,
                    Quantity = requestedItem.Quantity,
                    UnitPriceSnapshot = productSize.Price

                };
                decimal configureUnitPrice = productSize.Price;
                foreach (CreateOrderItemToppingRequestDTO requestedTopping in requestedItem.Toppings)
                {
                    Topping topping = toppings.First(currnentTopping => currnentTopping.Id == requestedTopping.ToppingId);
                    configureUnitPrice += topping.Price * requestedTopping.Quantity;
                    orderItem.OrderItemToppings.Add(new OrderItemTopping
                    {
                        ToppingId = topping.Id,
                        ToppingNameSnapshot = topping.Name,
                        Quantity = requestedTopping.Quantity,
                        UnitPriceSnapshot = topping.Price
                    });
                }

                foreach (int noteOptionId in requestedItem.NoteOptionIds)
                {
                    NoteOption noteOption = noteOptions.First(currentOption => currentOption.Id == noteOptionId);
                    orderItem.OrderItemNotes.Add(new OrderItemNote
                    {
                        NoteOptionId = noteOption.Id,
                        NoteNameSnapshot = noteOption.Name
                    });
                }

                orderItem.SubtotalAmount = configureUnitPrice * requestedItem.Quantity;
                order.TotalAmount += orderItem.SubtotalAmount;
                order.OrderItems.Add(orderItem);
            }

            await _orderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return new CreateOrderResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsOrderValid = true,
                Order = new CreateOrderResponseDTO

                {

                    Id = order.Id,
                    Code = order.Code,
                    BranchId = order.BranchId,
                    BusinessDate = order.BusinessDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    CreatedAt = order.CreatedAt

                }

            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    // Tạo kết quả lỗi nghiệp vụ sau khi xác nhận branch và quyền
    private static CreateOrderResultDTO CreateInvalidResult(string errorMessage)
    {
        return new CreateOrderResultDTO

        {

            IsBranchFound = true,
            HasAccess = true,
            IsOrderValid = false,
            ErrorMessage = errorMessage

        };
    }

    // Lấy các đơn của employee và chuyển entity thành DTO cho màn danh sách
    public async Task<List<MyOrderListItemDTO>> GetMyOrdersAsync(
        int currentUserId,
        string keyword,
        DateOnly? businessDate)
    {
        string normalizedKeyword = keyword.Trim();
        List<SalesOrder> orders = await _orderRepository.GetOrdersCreatedByUserAsync(currentUserId, normalizedKeyword, businessDate);
        List<MyOrderListItemDTO> result = new List<MyOrderListItemDTO>();
        foreach (SalesOrder order in orders)
        {
            int totalQuantity = order.OrderItems.Sum(orderItem => orderItem.Quantity);
            result.Add(new MyOrderListItemDTO
            {
                Id = order.Id,
                Code = order.Code,
                TotalAmount = order.TotalAmount,
                TotalQuantity = totalQuantity,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            });
        }

        return result;
    }

    // Lấy và ánh xạ chi tiết 1 đơn hàng do employee hiện tại tạo
    public async Task<MyOrderDetailDTO?> GetMyOrderDetailAsync(
        int currentUserId,
        int orderId)
    {
        SalesOrder? order = await _orderRepository.GetOrderDetailCreatedByUserAsync(orderId, currentUserId);
        if (order is null)
        {
            return null;
        }

        MyOrderDetailDTO result = new MyOrderDetailDTO

        {

            Id = order.Id,
            Code = order.Code,
            BranchId = order.BranchId,
            BranchCode = order.Branch.Code,
            BranchName = order.Branch.Name,
            BusinessDate = order.BusinessDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            ReportReason = order.ReportReason,
            ReportedAt = order.ReportedAt,
            CreatedAt = order.CreatedAt

        };
        foreach (OrderItem orderItem in order.OrderItems)
        {
            MyOrderDetailItemDTO itemDTO = new MyOrderDetailItemDTO

            {

                ProductName = orderItem.ProductNameSnapshot,
                SizeName = orderItem.SizeNameSnapshot,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPriceSnapshot,
                SubtotalAmount = orderItem.SubtotalAmount

            };
            foreach (OrderItemTopping orderItemTopping in orderItem.OrderItemToppings)
            {
                itemDTO.Toppings.Add(new MyOrderDetailToppingDTO
                {
                    Name = orderItemTopping.ToppingNameSnapshot,
                    Quantity = orderItemTopping.Quantity,
                    UnitPrice = orderItemTopping.UnitPriceSnapshot
                });
            }

            foreach (OrderItemNote orderItemNote in orderItem.OrderItemNotes)
            {
                itemDTO.Notes.Add(new MyOrderDetailNoteDTO
                {
                    Name = orderItemNote.NoteNameSnapshot
                });
            }

            result.Items.Add(itemDTO);
        }

        // Chỉ đưa dữ liệu hiển thị ra employee, không trả JSON snapshot nội bộ.
        foreach (OrderAdjustment history in order.OrderAdjustments.OrderByDescending(history => history.Id))
        {
            OrderHistorySnapshotDTO? before = ReadResolutionSnapshot(history.BeforeData);
            OrderHistorySnapshotDTO? after = ReadResolutionSnapshot(history.AfterData);
            result.History.Add(new MyOrderResolutionDTO
            {
                Id = history.Id,
                ProcessedByFullName = history.AdjustedByUser?.FullName ?? "Quản lý",
                Reason = history.Reason,
                CreatedAt = history.CreatedAt,
                AfterStatus = after?.Status,
                BeforeTotalAmount = before?.TotalAmount,
                AfterTotalAmount = after?.TotalAmount,
                BeforeItems = MapResolutionItems(before),
                AfterItems = MapResolutionItems(after)
            });
        }

        result.LatestHistoryId = order.OrderAdjustments.Select(history => history.Id).DefaultIfEmpty(0).Max();
        return result;
    }

    // Lịch sử cũ không đọc được vẫn hiển thị lý do và thời gian xử lý.
    private static OrderHistorySnapshotDTO? ReadResolutionSnapshot(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OrderHistorySnapshotDTO>(data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Employee chỉ cần nội dung món, không cần ID menu hoặc thông tin tài khoản nội bộ.
    private static List<MyOrderDetailItemDTO> MapResolutionItems(OrderHistorySnapshotDTO? snapshot)
    {
        List<MyOrderDetailItemDTO> items = new List<MyOrderDetailItemDTO>();
        if (snapshot is null || snapshot.Items is null)
        {
            return items;
        }

        foreach (OrderManagementItemDTO original in snapshot.Items)
        {
            MyOrderDetailItemDTO item = new MyOrderDetailItemDTO

            {

                ProductName = original.ProductName,
                SizeName = original.SizeName,
                Quantity = original.Quantity,
                UnitPrice = original.UnitPrice,
                SubtotalAmount = original.SubtotalAmount

            };
            foreach (OrderManagementToppingDTO topping in original.Toppings)
            {
                item.Toppings.Add(new MyOrderDetailToppingDTO
                {
                    Name = topping.Name,
                    Quantity = topping.Quantity,
                    UnitPrice = topping.UnitPrice
                });
            }

            foreach (string note in original.Notes)
            {
                item.Notes.Add(new MyOrderDetailNoteDTO
                {
                    Name = note
                });
            }

            items.Add(item);
        }

        return items;
    }

    // Kiểm tra quyền sở hữu, trạng thái và cập nhật thông tin báo sai
    public async Task<ReportOrderResult> ReportOrderAsync(
        int currentUserId,
        int orderId,
        ReportOrderRequestDTO request)
    {
        string normalizedReason = request.Reason?.Trim() ?? string.Empty;
        // Service tự bảo vệ nếu được gọi ngoài Controller
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            return new ReportOrderResult

            {

                IsReportValid = false,
                ErrorMessage = "Lý do báo sai không được để trống."

            };
        }

        if (normalizedReason.Length > 500)
        {
            return new ReportOrderResult

            {

                IsReportValid = false,
                ErrorMessage = "Lý do báo sai không được vượt quá 500 ký tự."

            };
        }

        SalesOrder? order = await _orderRepository.GetOrderForReportAsync(orderId, currentUserId);
        if (order is null)
        {
            return new ReportOrderResult

            {

                IsReportValid = true,
                IsOrderFound = false,
                ErrorMessage = "Không tìm thấy đơn hàng."

            };
        }

        // Cả đơn hoàn tất và đã điều chỉnh đều có thể cần kiểm tra lại.
        if (order.Status != CompletedStatus && order.Status != AdjustedStatus)
        {
            return new ReportOrderResult

            {

                IsReportValid = true,
                IsOrderFound = true,
                CanReport = false,
                ErrorMessage = "Chỉ đơn hoàn tất hoặc đã điều chỉnh mới có thể báo sai. Hãy tải lại đơn."

            };
        }

        int latestHistoryId = order.OrderAdjustments.Select(history => history.Id).DefaultIfEmpty(0).Max();
        if (request.ExpectedHistoryId != latestHistoryId)
        {
            return new ReportOrderResult

            {

                IsReportValid = true,
                IsOrderFound = true,
                ErrorMessage = "Quản lý đã có kết quả xử lý mới. Hãy tải lại và đọc kết quả trước khi báo sai lại."

            };
        }

        if (latestHistoryId > 0 && !request.HasReviewedResolution)
        {
            return new ReportOrderResult

            {

                IsOrderFound = true,
                ErrorMessage = "Vui lòng xác nhận đã xem kết quả xử lý trước khi báo sai lại."

            };
        }

        DateTime reportedAt = DateTime.UtcNow;
        // UPDATE có điều kiện ngăn hai lần gửi hoặc xử lý admin chen giữa bước đọc và ghi.
        int changedRows = await _orderRepository.TryReportOrderAsync(orderId, currentUserId, latestHistoryId, normalizedReason, reportedAt);
        if (changedRows != 1)
        {
            return new ReportOrderResult

            {

                IsReportValid = true,
                IsOrderFound = true,
                ErrorMessage = "Đơn đã thay đổi hoặc đã được báo sai. Hãy tải lại trước khi tiếp tục."

            };
        }

        return new ReportOrderResult

        {

            IsReportValid = true,
            IsOrderFound = true,
            CanReport = true,
            Order = new ReportOrderResponseDTO

            {

                Id = order.Id,
                Code = order.Code,
                Status = NeedsReviewStatus,
                ReportReason = normalizedReason,
                ReportedAt = reportedAt

            }

        };
    }
}
