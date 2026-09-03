using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Employees;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "OWNER,ADMIN")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    // Nhận IEmployeeService từ Di
    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Lấy danh sách nhân viên theo chi nhánh
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EmployeeListItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EmployeeListItemDTO>>>
        GetEmployeesByBranchAsync(
            [FromQuery] int branchId,
            [FromQuery] string keyword = "",
            [FromQuery] bool? isActive = null)
    {
        // BranchId phải là 1 số nguyên dương
        if (branchId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Chi nhánh không hợp lệ",
                detail: "BranchId phải lớn hơn 0.");
        }

        // Đọc danh tính người dùng hiện tại từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? role = User.FindFirstValue(ClaimTypes.Role);
        bool isValidUserId = int.TryParse(userIdValue, out int currentUserId);

        if (!isValidUserId || string.IsNullOrWhiteSpace(role))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa đầy đủ thông tin người dùng.");
        }

        // Service kiểm tra chi nhánh, quyền truy cập và lấy nhân viên
        EmployeeListResultDTO result =
            await _employeeService.GetEmployeesByBranchAsync(
                currentUserId,
                role,
                branchId,
                keyword,
                isActive);

        // Chi nhánh không tồn tại hoặc bị xóa
        if (!result.IsBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh",
                detail: "Chi nhánh được yêu cầu không tồn tại");
        }

        // Chi nhánh tồn tại nhưng không có quyền truy cập
        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền truy cập",
                detail: "Bạn không được phân công tại chi nhánh này.");
        }

        return Ok(result.Employees);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của 1 nhân viên tại chi nhánh
    /// </summary>
    [HttpGet("{employeeId:int}", Name = "GetEmployeeDetail")]
    [ProducesResponseType(typeof(EmployeeDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailDTO>> GetEmployeeDetailAsync(
        [FromRoute] int employeeId,
        [FromQuery] int branchId)
    {
        // EmployeeId và BranchId phải là số nguyên dương
        if (employeeId <= 0 || branchId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dữ liệu không hợp lệ",
                detail: "EmployeeId và BranchId phải lớn hơn 0.");
        }

        // Đọc thông tin hiện tại người dùng từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? role = User.FindFirstValue(ClaimTypes.Role);
        bool isValidUser = int.TryParse(userIdValue, out int currentUserId);

        if (!isValidUser || string.IsNullOrWhiteSpace(role))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa đầy đủ thông tin người dùng.");
        }

        // Gọi service để kiểm tra quyền và lấy chi tiết nhân viên
        EmployeeDetailResultDTO result =
            await _employeeService.GetEmployeeDetailAsync(
                currentUserId,
                role,
                employeeId,
                branchId);

        // Chi nhánh không tồn tại hoặc đã xóa
        if (!result.IsBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh",
                detail: "Chi nhánh được yêu cầu không tồn tại.");
        }

        // Người dùng không có quyền truy cập chi nhánh
        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền truy cập",
                detail: "Bạn không được phân công tại chi nhánh này.");
        }

        // Không tìm thấy nhân viên hợp lệ tại chi nhánh
        if (!result.IsEmployeeFound || result.Employee == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy nhân viên",
                detail: "Nhân viên không tồn tại hoặc không còn làm việc tại chi nhánh.");
        }

        return Ok(result.Employee);
    }

    /// <summary>
    /// Tạo tài khoản, hồ sơ và phân công chi nhánh cho nhân viên mới
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(EmployeeDetailDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployeeDetailDTO>> CreateEmployeeAsync(
        [FromBody] EmployeeCreateDTO request)
    {
        // DateOnly mặc định không được xem là ngày vào làm hợp lệ
        if (request.HireDate == default)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ngày vào làm không hợp lệ",
                detail: "Ngày vào làm không được để trống.");
        }

        // Lấy Id của ADMIN đang thực hiện request từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isValidUserId = int.TryParse(userIdValue, out int currentAdminId);

        if (!isValidUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa thông tin người dùng.");
        }

        // Service kiểm tra nghiệp vụ và tạo nhân viên
        EmployeeCreateResultDTO result =
            await _employeeService.CreateEmployeeAsync(currentAdminId, request);

        if (!result.IsBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh",
                detail: "Chi nhánh được yêu cầu không tồn tại.");
        }

        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền truy cập",
                detail: "Bạn không được phân công tại chi nhánh này.");
        }

        if (!result.IsEmployeeRoleFound)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Lỗi cấu hình hệ thống",
                detail: "Không tìm thấy Role EMPLOYEE.");
        }

        if (result.IsUsernameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Username đã tồn tại",
                detail: "Vui lòng sử dụng Username khác.");
        }

        if (result.IsEmailDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email đã tồn tại",
                detail: "Vui lòng sử dụng Email khác.");
        }

        if (result.IsEmployeeCodeDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Mã nhân viên đã tồn tại",
                detail: "Vui lòng sử dụng mã nhân viên khác.");
        }

        // Bảo vệ trường hợp kết quả từ Service không nhất quán
        if (result.Employee == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo nhân viên",
                detail: "Hệ thống không nhận được dữ liệu nhân viên vừa tạo.");
        }

        // Trả 201 và đường dẫn để xem nhân viên vừa tạo
        return CreatedAtRoute("GetEmployeeDetail",
            new
            {
                employeeId = result.Employee.Id,
                branchId = request.BranchId
            }, result.Employee);
    }

    /// <summary>
    /// Cập nhật thông tin nhân viên tại chi nhánh ADMIN được phân công
    /// </summary>
    [HttpPut("{employeeId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(EmployeeDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployeeDetailDTO>> UpdateEmployeeAsync(
        [FromRoute] int employeeId,
        [FromQuery] int branchId,
        [FromBody] EmployeeUpdateDTO request)
    {
        // EmployeeId và BranchId phải là số nguyên dương
        if (employeeId <= 0 || branchId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dữ liệu không hợp lệ",
                detail: "EmployeeId và BranchId phải lớn hơn 0.");
        }

        // DateOnly mặc định không được xem là ngày vào làm hợp lệ
        if (request.HireDate == default)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ngày vào làm không hợp lệ",
                detail: "Ngày vào làm không được để trống.");
        }

        // Lấy Id của ADMIN đang thực hiện request từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isValidUserId = int.TryParse(userIdValue, out int currentAdminId);

        if (!isValidUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa thông tin người dùng.");
        }

        // Gọi Service để kiểm tra nghiệp vụ và cập nhật nhân viên
        EmployeeUpdateResultDTO result =
            await _employeeService.UpdateEmployeeAsync(
                currentAdminId,
                employeeId,
                branchId,
                request);

        // Chi nhánh không tồn tại hoặc bị xóa
        if (!result.IsBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh",
                detail: "Chi nhánh được yêu cầu không tồn tại.");
        }

        // ADMIN không còn được phân công tại chi nhánh
        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền truy cập",
                detail: "Bạn không được phân công tại chi nhánh này.");
        }

        // Nhân viên không tồn tại hoặc không thuộc chi nhánh
        if (!result.IsEmployeeFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy nhân viên",
                detail: "Nhân viên không tồn tại hoặc không còn làm việc tại chi nhánh.");
        }

        // Email đang được 1 tài khoản khác sử dụng
        if (result.IsEmailDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email đã tồn tại",
                detail: "Vui lòng sử dụng Email khác.");
        }

        // Mã nhân viên đang được 1 hồ sơ khác sử dụng
        if (result.IsEmployeeCodeDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Mã nhân viên đã tồn tại",
                detail: "Vui lòng sử dụng mã nhân viên khác.");
        }

        // Bảo vệ trường hợp kết quả từ Service không nhất quán
        if (result.Employee == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể cập nhật nhân viên",
                detail: "Hệ thống không nhận được dữ liệu nhân viên sau khi cập nhật.");
        }

        // Trả dữ liệu mới nhất của nhân viên sau khi cập nhật
        return Ok(result.Employee);
    }

    /// <summary>
    /// Chuyển nhân viên từ chi nhánh hiện tại sang chi nhánh mới
    /// </summary>
    [HttpPut("{employeeId:int}/current-branch")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(EmployeeDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployeeDetailDTO>> ChangeEmployeeBranchAsync(
        [FromRoute] int employeeId,
        [FromQuery] int currentBranchId,
        [FromBody] EmployeeChangeBranchDTO request)
    {
        // EmployeeId và BranchId phải là số nguyên dương
        if (employeeId <= 0 || currentBranchId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dữ liệu không hợp lệ",
                detail: "EmployeeId và BranchId phải lớn hơn 0.");
        }

        // DateOnly mặc định không được xem là ngày chuyển hợp lệ
        if (request.ActiveFrom == default)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ngày chuyển không hợp lệ",
                detail: "Ngày bắt đầu tại chi nhánh mới không được để trống.");
        }

        // Lấy Id của ADMIN đang thực hiện request từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isValidUserId = int.TryParse(userIdValue, out int currentAdminId);

        if (!isValidUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa thông tin người dùng.");
        }

        // Gọi Service để kiểm tra nghiệp vụ chuyển chi nhánh
        EmployeeChangeBranchResultDTO result =
            await _employeeService.ChangeEmployeeBranchAsync(
                currentAdminId,
                employeeId,
                currentBranchId,
                request);

        // Chi nhánh hiện tại không tồn tại hoặc đã bị xóa
        if (!result.IsCurrentBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh hiện tại",
                detail: "Chi nhánh hiện tại không tồn tại hoặc đã bị xóa.");
        }

        // Chi nhánh mới không tồn tại hoặc đã bị xóa
        if (!result.IsNewBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh mới",
                detail: "Chi nhánh muốn chuyển đến không tồn tại hoặc đã bị xóa.");
        }

        // ADMIN không có quyền tại 1 trong 2 chi nhánh
        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền chuyển chi nhánh",
                detail: "Bạn phải được phân công tại cả chi nhánh hiện tại và chi nhánh mới.");
        }

        // Nhân viên hoặc phân công hiện tại không tồn tại
        if (!result.IsEmployeeFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy nhân viên",
                detail: "Nhân viên không tồn tại hoặc không làm việc tại chi nhánh hiện tại.");
        }

        // Không cho chuyển đến chi nhánh hiện tại
        if (result.IsSameBranch)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Chi nhánh bị trùng",
                detail: "Chi nhánh mới phải khác chi nhánh hiện tại.");
        }

        // Ngày bắt đầu mới không phù hợp với lịch sử phân công
        if (!result.IsActiveFromValid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ngày chuyển không hợp lệ",
                detail: "Ngày bắt đầu tại chi nhánh mới phải sau ngày bắt đầu của phân công hiện tại.");
        }

        // Bảo vệ trường hợp Service trả kết quả không nhất quán
        if (result.Employee == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể chuyển chi nhánh",
                detail: "Hệ thống không nhận được thông tin nhân viên sau khi chuyển.");
        }

        // Trả thông tin nhân viên cùng lịch sử phân công mới nhất
        return Ok(result.Employee);
    }


}