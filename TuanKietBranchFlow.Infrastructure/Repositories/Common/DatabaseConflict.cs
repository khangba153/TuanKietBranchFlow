using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Common;

public static class DatabaseConflict
{
    // Chỉ phân loại vi phạm unique của SQL Server; không che lỗi database khác.
    public static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException && (sqlException.Number == 2601 || sqlException.Number == 2627);
    }
}
