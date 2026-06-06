namespace JobEngine.Shared.Common;

// Typed exceptions let the API layer return correct HTTP status codes
// without any business logic in controllers
// NotFoundException → 404   
// ConflictException → 409
// QuotaExceededException → 429  
// UnauthorizedException → 401

public class NotFoundException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);
public class QuotaExceededException(string message) : Exception(message);
public class UnauthorizedException(string msg = "Unauthorized") : Exception(msg);
public class JobExecutionException(string message) : Exception(message);