using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Application.DTOs.Inventory;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Inventory;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Nhận repository và UnitOfWork để dùng chung một DbContext trong mỗi request.
    public InventoryService(
        IInventoryRepository inventoryRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    // Danh mục chung chỉ dành cho ADMIN/OWNER; API giới hạn role trước khi gọi.
    public async Task<List<InventoryUnitDTO>> GetUnitsAsync(string? search)
    {
        List<IngredientUnit> units = await _inventoryRepository.GetUnitsAsync(search?.Trim());
        List<int> usedUnitIds = await _inventoryRepository.GetUsedUnitIdsAsync();
        List<InventoryUnitDTO> ingredientUnits = new List<InventoryUnitDTO>();

        foreach (IngredientUnit unit in units)
        {
            InventoryUnitDTO ingredientUnitDTO = new InventoryUnitDTO
            {
                Id = unit.Id,
                Name = unit.Name,
                IsActive = unit.IsActive,
                IsUsed = usedUnitIds.Contains(unit.Id)
            };

            ingredientUnits.Add(ingredientUnitDTO);
        }

        return ingredientUnits;
    }

    public async Task<List<InventoryIngredientDTO>> GetIngredientsAsync(string? search)
    {
        List<Ingredient> ingredients = await _inventoryRepository.GetIngredientsAsync(search?.Trim());
        List<int> usedIngredientIds = await _inventoryRepository.GetUsedIngredientIdsAsync();
        List<InventoryIngredientDTO> ingredientDTOs = new List<InventoryIngredientDTO>();

        foreach (Ingredient ingredient in ingredients)
        {
            InventoryIngredientDTO ingredientDTO = new InventoryIngredientDTO
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                UnitId = ingredient.UnitId,
                UnitName = ingredient.Unit.Name,
                IsActive = ingredient.IsActive,
                IsUsed = usedIngredientIds.Contains(ingredient.Id)
            };

            ingredientDTOs.Add(ingredientDTO);
        }

        return ingredientDTOs;
    }

    // Tạo đơn vị mới sau khi chuẩn hóa tên và kiểm tra trùng.
    public async Task<InventoryResultDTO> CreateUnitAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 50)
        {
            return Error("Invalid", "Tên đơn vị phải có từ 1 đến 50 ký tự.");
        }

        string unitName = name.Trim();

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _inventoryRepository.LockConfigurationAsync();

            bool isNameDuplicated = await _inventoryRepository.UnitNameExistsAsync(unitName, 0);

            if (isNameDuplicated)
            {
                return Error("Conflict", "Tên đơn vị đã tồn tại.");
            }

            IngredientUnit ingredientUnit = new IngredientUnit
            {
                Name = unitName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _inventoryRepository.AddUnitAsync(ingredientUnit);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new InventoryResultDTO
            {
                SavedId = ingredientUnit.Id
            };
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await ResetAsync();

            if (await _inventoryRepository.UnitNameExistsAsync(unitName, 0))
            {
                return Error("Conflict", "Tên đơn vị đã tồn tại. Hãy tải lại danh sách.");
            }

            throw;
        }
        catch (SqlException exception) when (exception.Number == 51001)
        {
            return Error("Conflict", "Danh mục đang được cập nhật. Hãy tải lại và thử sau.");
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }

    // Chỉ sửa tên; không thay trạng thái hoặc đổi ý nghĩa đơn vị đã dùng.
    public async Task<InventoryResultDTO> UpdateUnitAsync(
        int unitId,
        string name)
    {
        if (unitId <= 0 || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 50)
        {
            return Error("Invalid", "Tên đơn vị phải có từ 1 đến 50 ký tự.");
        }

        string unitName = name.Trim();

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _inventoryRepository.LockConfigurationAsync();

            IngredientUnit? ingredientUnit = await _inventoryRepository.GetUnitAsync(unitId);

            if (ingredientUnit == null)
            {
                return Error("NotFound", "Không tìm thấy đơn vị tính.");
            }

            bool isNameDuplicated = await _inventoryRepository.UnitNameExistsAsync(unitName, unitId);

            if (isNameDuplicated)
            {
                return Error("Conflict", "Tên đơn vị đã tồn tại.");
            }

            bool isUnitUsed = await _inventoryRepository.UnitIsUsedAsync(unitId);

            if (ingredientUnit.Name != unitName && isUnitUsed)
            {
                return Error("Conflict", "Đơn vị đã được sử dụng, không đổi tên. Hãy tạo đơn vị mới nếu cần.");
            }

            ingredientUnit.Name = unitName;
            ingredientUnit.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new InventoryResultDTO
            {
                SavedId = ingredientUnit.Id
            };
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await ResetAsync();

            if (await _inventoryRepository.UnitNameExistsAsync(unitName, unitId))
            {
                return Error("Conflict", "Tên đơn vị đã tồn tại. Hãy tải lại danh sách.");
            }

            throw;
        }
        catch (SqlException exception) when (exception.Number == 51001)
        {
            return Error("Conflict", "Danh mục đang được cập nhật. Hãy tải lại và thử sau.");
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }

    // Chỉ đổi trạng thái; đơn vị còn nguyên liệu hoạt động không được ngừng dùng.
    public async Task<InventoryResultDTO> UpdateUnitStatusAsync(
        int unitId,
        bool isActive)
    {
        if (unitId <= 0)
        {
            return Error("Invalid", "Đơn vị tính không hợp lệ.");
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _inventoryRepository.LockConfigurationAsync();

            IngredientUnit? ingredientUnit = await _inventoryRepository.GetUnitAsync(unitId);

            if (ingredientUnit == null)
            {
                return Error("NotFound", "Không tìm thấy đơn vị tính.");
            }

            if (!isActive)
            {
                bool hasActiveIngredients = await _inventoryRepository.UnitHasActiveIngredientsAsync(unitId);

                if (hasActiveIngredients)
                {
                    return Error("Conflict", "Còn nguyên liệu hoạt động sử dụng đơn vị này.");
                }
            }

            ingredientUnit.IsActive = isActive;
            ingredientUnit.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new InventoryResultDTO
            {
                SavedId = ingredientUnit.Id
            };
        }
        catch (SqlException exception) when (exception.Number == 51001)
        {
            return Error("Conflict", "Danh mục đang được cập nhật. Hãy tải lại và thử sau.");
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }

    // Tạo nguyên liệu với một đơn vị đang hoạt động, chưa tạo tồn chi nhánh.
    public async Task<InventoryResultDTO> CreateIngredientAsync(
        string name,
        int unitId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150 || unitId <= 0)
        {
            return Error("Invalid", "Nhập tên nguyên liệu từ 1 đến 150 ký tự và chọn đơn vị tính.");
        }

        string ingredientName = name.Trim();

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _inventoryRepository.LockConfigurationAsync();

            bool isNameDuplicated = await _inventoryRepository.IngredientNameExistsAsync(ingredientName, 0);

            if (isNameDuplicated)
            {
                return Error("Conflict", "Tên nguyên liệu đã tồn tại.");
            }

            IngredientUnit? ingredientUnit = await _inventoryRepository.GetUnitAsync(unitId);

            if (ingredientUnit == null || !ingredientUnit.IsActive)
            {
                return Error("Conflict", "Đơn vị không tồn tại hoặc đã ngừng dùng.");
            }

            Ingredient ingredient = new Ingredient
            {
                Name = ingredientName,
                UnitId = ingredientUnit.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _inventoryRepository.AddIngredientAsync(ingredient);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new InventoryResultDTO
            {
                SavedId = ingredient.Id
            };
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await ResetAsync();

            if (await _inventoryRepository.IngredientNameExistsAsync(ingredientName, 0))
            {
                return Error("Conflict", "Tên nguyên liệu đã tồn tại. Hãy tải lại danh sách.");
            }

            throw;
        }
        catch (SqlException exception) when (exception.Number == 51001)
        {
            return Error("Conflict", "Danh mục đang được cập nhật. Hãy tải lại và thử sau.");
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }

    // Sửa thông tin nguyên liệu, giữ nguyên trạng thái và bảo vệ đơn vị đã dùng.
    public async Task<InventoryResultDTO> UpdateIngredientAsync(
        int ingredientId,
        string name,
        int unitId)
    {
        if (ingredientId <= 0 || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150 || unitId <= 0)
        {
            return Error("Invalid", "Nhập tên nguyên liệu từ 1 đến 150 ký tự và chọn đơn vị tính.");
        }

        string ingredientName = name.Trim();

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _inventoryRepository.LockConfigurationAsync();

            Ingredient? ingredient = await _inventoryRepository.GetIngredientAsync(ingredientId);

            if (ingredient == null)
            {
                return Error("NotFound", "Không tìm thấy nguyên liệu.");
            }

            bool isNameDuplicated = await _inventoryRepository.IngredientNameExistsAsync(ingredientName, ingredientId);

            if (isNameDuplicated)
            {
                return Error("Conflict", "Tên nguyên liệu đã tồn tại.");
            }

            if (ingredient.UnitId != unitId)
            {
                bool isIngredientUsed = await _inventoryRepository.IngredientIsUsedAsync(ingredientId);

                if (isIngredientUsed)
                {
                    return Error("Conflict", "Nguyên liệu đã cấu hình kho hoặc có lịch sử, không đổi đơn vị tính.");
                }
            }

            IngredientUnit? ingredientUnit = await _inventoryRepository.GetUnitAsync(unitId);

            if (ingredientUnit == null || !ingredientUnit.IsActive)
            {
                return Error("Conflict", "Đơn vị không tồn tại hoặc đã ngừng dùng.");
            }

            ingredient.Name = ingredientName;
            ingredient.UnitId = ingredientUnit.Id;
            ingredient.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new InventoryResultDTO
            {
                SavedId = ingredient.Id
            };
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await ResetAsync();

            if (await _inventoryRepository.IngredientNameExistsAsync(ingredientName, ingredientId))
            {
                return Error("Conflict", "Tên nguyên liệu đã tồn tại. Hãy tải lại danh sách.");
            }

            throw;
        }
        catch (SqlException exception) when (exception.Number == 51001)
        {
            return Error("Conflict", "Danh mục đang được cập nhật. Hãy tải lại và thử sau.");
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }

    // Bật lại nguyên liệu chỉ khi đơn vị còn hoạt động; ngừng dùng không xóa tồn.
    public async Task<InventoryResultDTO> UpdateIngredientStatusAsync(
        int ingredientId,
        bool isActive)
    {
        if (ingredientId <= 0)
        {
            return Error("Invalid", "Nguyên liệu không hợp lệ.");
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _inventoryRepository.LockConfigurationAsync();

            Ingredient? ingredient = await _inventoryRepository.GetIngredientAsync(ingredientId);

            if (ingredient == null)
            {
                return Error("NotFound", "Không tìm thấy nguyên liệu.");
            }

            if (isActive)
            {
                IngredientUnit? ingredientUnit = await _inventoryRepository.GetUnitAsync(ingredient.UnitId);

                if (ingredientUnit == null || !ingredientUnit.IsActive)
                {
                    return Error("Conflict", "Cần bật đơn vị tính trước khi bật nguyên liệu.");
                }
            }

            ingredient.IsActive = isActive;
            ingredient.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new InventoryResultDTO
            {
                SavedId = ingredient.Id
            };
        }
        catch (SqlException exception) when (exception.Number == 51001)
        {
            return Error("Conflict", "Danh mục đang được cập nhật. Hãy tải lại và thử sau.");
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }

    public async Task<InventoryResultDTO> GetStockAsync(
        int userId,
        string role,
        InventoryQueryDTO query)
    {
        string? status = string.IsNullOrWhiteSpace(query.StockStatus) ? null : query.StockStatus.Trim().ToUpperInvariant();
        if (query.BranchId <= 0 || query.Page < 1 || query.Page > 10000 || query.Search?.Length > 150 || (status != null && status != "UNCONFIGURED" && status != "OUT" && status != "LOW" && status != "AVAILABLE"))
        {
            return Error("Invalid", "Bộ lọc hoặc trang không hợp lệ.");
        }

        InventoryResultDTO? accessError = await CheckBranchAsync(userId, role, query.BranchId, false);
        if (accessError != null)
        {
            return accessError;
        }

        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(query.BranchId);
        InventoryStockPage stock = await _inventoryRepository.GetStockAsync(query.BranchId, query.Search?.Trim(), query.IsActive, status, query.Page, 20);
        InventoryPageDTO data = new InventoryPageDTO

        {

            CanConfigure = role == "ADMIN" && branch?.IsActive == true,
            ConfiguredCount = stock.ConfiguredCount,
            OutOfStockCount = stock.OutOfStockCount,
            LowStockCount = stock.LowStockCount,
            UnconfiguredCount = stock.UnconfiguredCount,
            TotalCount = stock.TotalCount,
            Page = query.Page

        };
        foreach (InventoryStockRow item in stock.Items)
        {
            data.Items.Add(new InventoryRowDTO
            {
                ConfigurationId = item.ConfigurationId,
                IngredientId = item.IngredientId,
                Name = item.Name,
                UnitName = item.UnitName,
                IsActive = item.IsActive,
                Quantity = item.Quantity,
                WarningThreshold = item.WarningThreshold,
                UpdatedAt = item.UpdatedAt,
                RowVersion = item.RowVersion,
                StockStatus = GetStockStatus(item.Quantity, item.WarningThreshold)
            });
        }

        return new InventoryResultDTO

        {

            Inventory = data

        };
    }

    public async Task<InventoryResultDTO> ConfigureAsync(
        int userId,
        InventoryConfigurationRequestDTO request)
    {
        if (request.BranchId <= 0 || request.IngredientId <= 0 || !IsValidThreshold(request.WarningThreshold))
        {
            return Error("Invalid", "Ngưỡng phải không âm, tối đa 3 số lẻ và không vượt 999999999999999,999.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _inventoryRepository.LockConfigurationAsync();
            InventoryResultDTO? error = await CheckBranchAsync(userId, "ADMIN", request.BranchId, true);
            if (error != null)
            {
                return error;
            }

            Ingredient? ingredient = await _inventoryRepository.GetIngredientAsync(request.IngredientId);
            if (ingredient == null)
            {
                return Error("NotFound", "Không tìm thấy nguyên liệu.");
            }

            IngredientUnit? unit = await _inventoryRepository.GetUnitAsync(ingredient.UnitId);
            if (!ingredient.IsActive || unit == null || !unit.IsActive)
            {
                return Error("Conflict", "Chỉ thêm nguyên liệu và đơn vị đang hoạt động vào kho.");
            }

            if (await _inventoryRepository.ConfigurationExistsAsync(request.BranchId, request.IngredientId))
            {
                return Error("Conflict", "Nguyên liệu đã có trong kho chi nhánh. Hãy tải lại.");
            }

            BranchIngredient configuration = new BranchIngredient

            {

                BranchId = request.BranchId,
                IngredientId = request.IngredientId,
                Quantity = 0,
                WarningThreshold = request.WarningThreshold,
                UpdatedAt = DateTime.UtcNow

            };
            await _inventoryRepository.AddAsync(configuration);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return new InventoryResultDTO

            {

                SavedId = configuration.Id

            };
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await ResetAsync();
            if (await _inventoryRepository.ConfigurationExistsAsync(request.BranchId, request.IngredientId))
            {
                return Error("Conflict", "Nguyên liệu đã có trong kho chi nhánh. Hãy tải lại.");
            }

            throw;
        }
        catch (SqlException exception) when (exception.Number == 51001)
        {
            return Error("Conflict", "Kho đang được cấu hình. Hãy tải lại và thử sau.");
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }

    public async Task<InventoryResultDTO> UpdateThresholdAsync(
        int userId,
        int id,
        InventoryThresholdRequestDTO request)
    {
        if (id <= 0 || !IsValidThreshold(request.WarningThreshold) || request.ExpectedRowVersion?.Length != 8)
        {
            return Error("Invalid", "Ngưỡng hoặc phiên bản dữ liệu không hợp lệ. Ngưỡng tối đa 3 số lẻ.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            BranchIngredient? configuration = await _inventoryRepository.GetConfigurationAsync(id);
            if (configuration == null)
            {
                return Error("NotFound", "Không tìm thấy cấu hình kho.");
            }

            InventoryResultDTO? error = await CheckBranchAsync(userId, "ADMIN", configuration.BranchId, true);
            if (error != null)
            {
                return error;
            }

            if (!configuration.RowVersion.SequenceEqual(request.ExpectedRowVersion!))
            {
                return Error("Conflict", "Tồn kho đã thay đổi. Hãy tải lại trước khi sửa ngưỡng.");
            }

            _inventoryRepository.SetExpectedRowVersion(configuration, request.ExpectedRowVersion!);
            configuration.WarningThreshold = request.WarningThreshold;
            configuration.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return new InventoryResultDTO

            {

                SavedId = configuration.Id

            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error("Conflict", "Tồn kho đã thay đổi. Hãy tải lại trước khi sửa ngưỡng.");
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }

    private async Task<InventoryResultDTO?> CheckBranchAsync(
        int userId,
        string role,
        int branchId,
        bool writing)
    {
        if (userId <= 0 || (role != "ADMIN" && role != "OWNER" && role != "EMPLOYEE"))
        {
            return Error("Forbidden", "Không có quyền truy cập kho.");
        }

        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(branchId);
        if (branch == null)
        {
            return Error("NotFound", "Không tìm thấy chi nhánh.");
        }

        if (role != "OWNER" && !await _branchRepository.HasActiveAssignmentAsync(userId, branchId, DateOnly.FromDateTime(DateTime.Today)))
        {
            return Error("Forbidden", "Bạn không được phân công tại chi nhánh này.");
        }

        if (writing && !branch.IsActive)
        {
            return Error("Conflict", "Chi nhánh ngừng hoạt động chỉ được xem tồn.");
        }

        return null;
    }

    public static bool IsValidThreshold(decimal threshold)
    {
        return threshold >= 0 && threshold <= 999999999999999.999m && decimal.Round(threshold, 3) == threshold;
    }

    public static string GetStockStatus(
        decimal? quantity,
        decimal? threshold)
    {
        if (!quantity.HasValue)
        {
            return "UNCONFIGURED";
        }

        if (quantity == 0)
        {
            return "OUT";
        }

        if (threshold > 0 && quantity <= threshold)
        {
            return "LOW";
        }

        return "AVAILABLE";
    }

    private static InventoryResultDTO Error(
        string code,
        string message)
    {
        return new InventoryResultDTO

        {

            ErrorCode = code,
            ErrorMessage = message

        };
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException && (sqlException.Number == 2601 || sqlException.Number == 2627);
    }

    private async Task ResetAsync()
    {
        await _unitOfWork.RollbackTransactionAsync();
        _unitOfWork.ClearTracking();
    }
}
