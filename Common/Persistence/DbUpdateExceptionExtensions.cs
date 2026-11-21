using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Common.Persistence;

public static class DbUpdateExceptionExtensions
{
    private const int FilaDuplicadaEnIndiceUnico = 2601;
    private const int ViolacionDeRestriccionUnique = 2627;

    public static bool EsViolacionDeIndiceUnico(this DbUpdateException exception) =>
        exception.InnerException is SqlException sql
        && sql.Number is FilaDuplicadaEnIndiceUnico or ViolacionDeRestriccionUnique;
}
