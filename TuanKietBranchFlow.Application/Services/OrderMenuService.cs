using TuanKietBranchFlow.Application.DTOs.OrderMenus;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;

namespace TuanKietBranchFlow.Application.Services;

public class OrderMenuService : IOrderMenuService
{
    private const string UpSizeGroupName = "Up size";

    private readonly IBranchRepository _branchRepository;
    private readonly IOrderMenuRepository _orderMenuRepository;

    public OrderMenuService(
        IBranchRepository branchRepository,
        IOrderMenuRepository orderMenuRepository)
    {
        _branchRepository = branchRepository;
        _orderMenuRepository = orderMenuRepository;
    }

    // Lấy menu sau khi kiểm tra chi nhánh và phân công employee
    public async Task<OrderMenuResultDTO> GetOrderMenuAsync(
        int currentUserId,
        int branchId)
    {
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        // Bước 1: kiểm tra chi nhánh tồn tại và chưa bị xóa
        Branch? branch =
            await _branchRepository.GetNotDeletedByIdAsync(branchId);

        if (branch == null)
        {
            return new OrderMenuResultDTO
            {
                IsBranchFound = false,
                HasAccess = false,
                Menu = null
            };
        }

        // Bước 2: kiểm tra employee có phân công còn hiệu lực
        bool hasAccess =
            await _branchRepository.HasActiveAssignmentAsync(
                currentUserId,
                branchId,
                currentDate);

        if (!hasAccess)
        {
            return new OrderMenuResultDTO
            {
                IsBranchFound = true,
                HasAccess = false,
                Menu = null
            };
        }

        // Bước 3: chỉ lấy menu sau khi employee có quyền truy cập
        List<Category> categories =
            await _orderMenuRepository.GetAvailableCategoriesAsync(branchId);

        List<ToppingGroup> toppingGroups =
            await _orderMenuRepository.GetAvailableToppingGroupsAsync(branchId);

        List<NoteGroup> noteGroups =
            await _orderMenuRepository.GetActiveNoteGroupsAsync();

        // Chuyển danh mục và món thành DTO
        List<OrderMenuCategoryDTO> categoryDTOs =
            new List<OrderMenuCategoryDTO>();

        foreach (Category category in categories)
        {
            OrderMenuCategoryDTO categoryDTO =
                new OrderMenuCategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name
                };

            foreach (Product product in category.Products)
            {
                OrderMenuProductDTO productDTO =
                    new OrderMenuProductDTO
                    {
                        Id = product.Id,
                        Name = product.Name,
                        ImageUrl = product.ImageUrl,
                        MinPrice = product.ProductSizes.Min(
                            productSize => productSize.Price)
                    };

                foreach (ProductSize productSize in product.ProductSizes)
                {
                    OrderMenuProductSizeDTO productSizeDTO =
                        new OrderMenuProductSizeDTO
                        {
                            Id = productSize.Id,
                            SizeId = productSize.SizeId,
                            SizeName = productSize.Size.Name,
                            Price = productSize.Price
                        };

                    productDTO.ProductSizes.Add(productSizeDTO);
                }

                categoryDTO.Products.Add(productDTO);
            }

            categoryDTOs.Add(categoryDTO);
        }

        // Chuyển topping thành DTO và đặt giới hạn số lượng
        List<OrderMenuToppingGroupDTO> toppingGroupDTOs =
            new List<OrderMenuToppingGroupDTO>();

        foreach (ToppingGroup toppingGroup in toppingGroups)
        {
            OrderMenuToppingGroupDTO toppingGroupDTO =
                new OrderMenuToppingGroupDTO
                {
                    Id = toppingGroup.Id,
                    Name = toppingGroup.Name
                };

            bool isUpSizeGroup = string.Equals(
                toppingGroup.Name,
                UpSizeGroupName,
                StringComparison.OrdinalIgnoreCase);

            foreach (Topping topping in toppingGroup.Toppings)
            {
                OrderMenuToppingDTO toppingDTO =
                    new OrderMenuToppingDTO
                    {
                        Id = topping.Id,
                        Name = topping.Name,
                        UnitPrice = topping.Price,
                        MaxQuantity = isUpSizeGroup ? 1 : 3
                    };

                toppingGroupDTO.Toppings.Add(toppingDTO);
            }

            toppingGroupDTOs.Add(toppingGroupDTO);
        }

        // Chuyển nhóm ghi chú thành DTO
        List<OrderMenuNoteGroupDTO> noteGroupDTOs =
            new List<OrderMenuNoteGroupDTO>();

        foreach (NoteGroup noteGroup in noteGroups)
        {
            OrderMenuNoteGroupDTO noteGroupDTO =
                new OrderMenuNoteGroupDTO
                {
                    Id = noteGroup.Id,
                    Name = noteGroup.Name,
                    MaxSelections = 1
                };

            foreach (NoteOption option in noteGroup.NoteOptions)
            {
                OrderMenuNoteOptionDTO optionDTO =
                    new OrderMenuNoteOptionDTO
                    {
                        Id = option.Id,
                        Name = option.Name
                    };

                noteGroupDTO.Options.Add(optionDTO);
            }

            noteGroupDTOs.Add(noteGroupDTO);
        }

        // Bước 4: kết hợp toàn bộ dữ liệu thành response
        OrderMenuResponseDTO menu = new OrderMenuResponseDTO
        {
            Branch = new OrderMenuBranchDTO
            {
                Id = branch.Id,
                Code = branch.Code,
                Name = branch.Name
            },
            Categories = categoryDTOs,
            ToppingGroups = toppingGroupDTOs,
            NoteGroups = noteGroupDTOs
        };

        return new OrderMenuResultDTO
        {
            IsBranchFound = true,
            HasAccess = true,
            Menu = menu
        };
    }
}