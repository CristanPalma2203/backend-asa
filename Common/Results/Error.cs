namespace backend_asa.Common.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    BusinessRule,
    Unauthorized,
    Forbidden
}

public record Error(string Code, string Message, ErrorType Type);
