namespace JobEngine.Shared.Common;

// Domain-specific exceptions map to HTTP status codes in the API layer.
// NotFoundException        = 404
// ConflictException        = 409
// ValidationException      = 422
// QuotaExceededException   = 429
// UnauthorizedException    = 401

public class NotFoundException(string message)
    : Exception(message);

public class ConflictException(string message)
    : Exception(message);

public class QuotaExceededException(string message)
    : Exception(message);

public class UnauthorizedException(string message = "Unauthorized")
    : Exception(message);

public class JobExecutionException(string message)
    : Exception(message);