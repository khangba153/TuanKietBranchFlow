using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISizeRepository _sizeRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISizeRepository sizeRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _sizeRepository = sizeRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    // Chuyển các món chưa xóa thành DTO dành cho màn quản lý
    public async Task<List<MenuProductDTO>> GetAllProductsAsync()
    {
        List<Product> products =
            await _productRepository.GetAllNotDeletedAsync();

        List<MenuProductDTO> result = products
            .Select(product => new MenuProductDTO
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                ProductSizes = product.ProductSizes
                    .Select(productSize => new MenuProductSizeDTO
                    {
                        Id = productSize.Id,
                        SizeId = productSize.SizeId,
                        SizeName = productSize.Size.Name,
                        Price = productSize.Price,
                        IsProductSizeActive = productSize.IsActive,
                        IsSizeAvailable =
                            productSize.Size.IsActive
                            && !productSize.Size.Deleted
                    })
                    .ToList()
            })
            .ToList();

        return result;
    }

    // Kiểm tra dữ liệu, tạo Product cùng các size–giá và lưu thay đổi
    public async Task<CreateProductResultDTO> CreateProductAsync(
        CreateProductRequestDTO request)
    {
        CreateProductResultDTO result = new CreateProductResultDTO();

        if (request.CategoryId <= 0
            || string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 150
            || (!string.IsNullOrWhiteSpace(request.ImageUrl)
                && request.ImageUrl.Trim().Length > 1000)
            || request.ProductSizes == null
            || request.ProductSizes.Count < 2)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        bool hasInvalidProductSize = request.ProductSizes.Any(productSize =>
            productSize.SizeId <= 0
            || productSize.Price <= 0);

        if (hasInvalidProductSize)
        {
            result.IsProductSizesInvalid = true;
            return result;
        }

        List<int> requestedSizeIds = request.ProductSizes
            .Select(productSize => productSize.SizeId)
            .ToList();

        List<int> distinctSizeIds = requestedSizeIds
            .Distinct()
            .ToList();

        if (distinctSizeIds.Count != requestedSizeIds.Count)
        {
            result.IsProductSizesInvalid = true;
            return result;
        }

        string productName = request.Name.Trim();

        string? imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
            ? null
            : request.ImageUrl.Trim();

        Category? category =
            await _categoryRepository.GetNotDeletedByIdAsync(request.CategoryId);

        if (category == null)
        {
            result.IsCategoryNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _productRepository.ExistsByNameAsync(productName);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        List<Size> activeSizes =
            await _sizeRepository.GetActiveNotDeletedByIdsAsync(distinctSizeIds);

        if (activeSizes.Count != distinctSizeIds.Count)
        {
            result.IsProductSizesInvalid = true;
            return result;
        }

        DateTime createdAt = DateTime.UtcNow;

        Product product = new Product
        {
            CategoryId = category.Id,
            Name = productName,
            ImageUrl = imageUrl,
            IsActive = true,
            CreatedAt = createdAt,
            Deleted = false
        };

        foreach (CreateProductSizeRequestDTO requestProductSize
            in request.ProductSizes)
        {
            ProductSize productSize = new ProductSize
            {
                SizeId = requestProductSize.SizeId,
                Price = requestProductSize.Price,
                IsActive = true,
                CreatedAt = createdAt,
                Deleted = false
            };

            product.ProductSizes.Add(productSize);
        }

        // Món mới mặc định được bán tại các chi nhánh đang hoạt động
        List<Branch> activeBranches =
            await _branchRepository.GetActiveNotDeletedAsync();

        foreach (Branch branch in activeBranches)
        {
            BranchProduct branchProduct = new BranchProduct
            {
                BranchId = branch.Id,
                IsAvailable = true,
                UpdatedAt = createdAt
            };

            product.BranchProducts.Add(branchProduct);
        }

        // Lưu Product, ProductSize và BranchProduct cùng một lần
        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        List<MenuProductSizeDTO> createdProductSizes = new();

        foreach (ProductSize productSize in product.ProductSizes)
        {
            Size size = activeSizes.First(
                activeSize => activeSize.Id == productSize.SizeId);

            MenuProductSizeDTO productSizeDTO = new MenuProductSizeDTO
            {
                Id = productSize.Id,
                SizeId = productSize.SizeId,
                SizeName = size.Name,
                Price = productSize.Price,
                IsProductSizeActive = productSize.IsActive,
                IsSizeAvailable = size.IsActive && !size.Deleted
            };

            createdProductSizes.Add(productSizeDTO);
        }

        result.Product = new MenuProductDTO
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            Name = product.Name,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            ProductSizes = createdProductSizes
        };

        return result;
    }

    // Cập nhật Product và toàn bộ danh sách size–giá trong một lần lưu
    public async Task<UpdateProductResultDTO> UpdateProductAsync(
        int productId,
        UpdateProductRequestDTO request)
    {
        UpdateProductResultDTO result = new UpdateProductResultDTO();

        // Kiểm tra các trường cơ bản trước khi truy vấn dữ liệu
        if (productId <= 0
            || request.CategoryId <= 0
            || string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 150
            || (!string.IsNullOrWhiteSpace(request.ImageUrl)
                && request.ImageUrl.Trim().Length > 1000)
            || request.IsActive == null
            || request.ProductSizes == null
            || request.ProductSizes.Count < 2)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        // Kiểm tra từng dòng size–giá trong request
        bool hasInvalidProductSize = request.ProductSizes.Any(productSize =>
            productSize == null
            || productSize.SizeId <= 0
            || productSize.Price <= 0
            || productSize.IsActive == null);

        if (hasInvalidProductSize)
        {
            result.IsProductSizesInvalid = true;
            return result;
        }

        List<int> requestedSizeIds = request.ProductSizes
            .Select(productSize => productSize.SizeId)
            .ToList();

        List<int> distinctSizeIds = requestedSizeIds
            .Distinct()
            .ToList();

        if (distinctSizeIds.Count != requestedSizeIds.Count)
        {
            result.IsProductSizesInvalid = true;
            return result;
        }

        // Lấy Product đang được theo dõi để EF Core ghi nhận thay đổi
        Product? product =
            await _productRepository.GetNotDeletedByIdAsync(productId);

        if (product == null)
        {
            result.IsProductNotFound = true;
            return result;
        }

        Category? category =
            await _categoryRepository.GetNotDeletedByIdAsync(request.CategoryId);

        if (category == null)
        {
            result.IsCategoryNotFound = true;
            return result;
        }

        string productName = request.Name.Trim();

        bool isNameDuplicated =
            await _productRepository.ExistsByNameExceptIdAsync(
                productName,
                productId);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        // Size mới phải đang hoạt động; cặp hiện có vẫn có thể được giữ lại
        List<Size> activeSizes =
            await _sizeRepository.GetActiveNotDeletedByIdsAsync(distinctSizeIds);

        foreach (UpdateProductSizeRequestDTO requestProductSize
            in request.ProductSizes)
        {
            bool isExistingProductSize = product.ProductSizes.Any(productSize =>
                productSize.SizeId == requestProductSize.SizeId);

            bool isActiveSize = activeSizes.Any(size =>
                size.Id == requestProductSize.SizeId);

            if (!isExistingProductSize && !isActiveSize)
            {
                result.IsProductSizesInvalid = true;
                return result;
            }
        }

        DateTime updatedAt = DateTime.UtcNow;

        // Cập nhật thông tin chính của Product
        product.CategoryId = category.Id;
        product.Category = category;
        product.Name = productName;
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
            ? null
            : request.ImageUrl.Trim();
        product.IsActive = request.IsActive.Value;
        product.UpdatedAt = updatedAt;

        // Cập nhật cặp ProductSize đã có hoặc thêm cặp mới
        foreach (UpdateProductSizeRequestDTO requestProductSize
            in request.ProductSizes)
        {
            ProductSize? existingProductSize = product.ProductSizes
                .FirstOrDefault(productSize =>
                    productSize.SizeId == requestProductSize.SizeId);

            if (existingProductSize != null)
            {
                existingProductSize.Price = requestProductSize.Price;
                existingProductSize.IsActive = requestProductSize.IsActive.GetValueOrDefault();
                existingProductSize.UpdatedAt = updatedAt;
            }
            else
            {
                Size size = activeSizes.First(activeSize =>
                    activeSize.Id == requestProductSize.SizeId);

                ProductSize newProductSize = new ProductSize
                {
                    ProductId = product.Id,
                    SizeId = requestProductSize.SizeId,
                    Size = size,
                    Price = requestProductSize.Price,
                    IsActive = requestProductSize.IsActive.GetValueOrDefault(),
                    CreatedAt = updatedAt,
                    Deleted = false
                };

                product.ProductSizes.Add(newProductSize);
            }
        }

        // Size bị bỏ khỏi request được xóa mềm để giữ liên kết đơn hàng cũ
        foreach (ProductSize productSize in product.ProductSizes)
        {
            if (!distinctSizeIds.Contains(productSize.SizeId))
            {
                productSize.Deleted = true;
                productSize.UpdatedAt = updatedAt;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Chỉ trả các cặp size–giá chưa xóa lên màn quản lý
        result.Product = new MenuProductDTO
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            Name = product.Name,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            ProductSizes = product.ProductSizes
                .Where(productSize => !productSize.Deleted)
                .Select(productSize => new MenuProductSizeDTO
                {
                    Id = productSize.Id,
                    SizeId = productSize.SizeId,
                    SizeName = productSize.Size.Name,
                    Price = productSize.Price,
                    IsProductSizeActive = productSize.IsActive,
                    IsSizeAvailable =
                        productSize.Size.IsActive && !productSize.Size.Deleted
                })
                .ToList()
        };

        return result;
    }

    // Đánh dấu Product đã xóa mềm và lưu thay đổi
    public async Task<DeleteProductResultDTO> DeleteProductAsync(int productId)
    {
        DeleteProductResultDTO result = new DeleteProductResultDTO();

        Product? product =
            await _productRepository.GetNotDeletedByIdAsync(productId);

        if (product == null)
        {
            result.IsProductNotFound = true;
            return result;
        }

        product.Deleted = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        result.IsDeleted = true;
        return result;
    }
}
