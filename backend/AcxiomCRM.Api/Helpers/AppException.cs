using AcxiomCRM.Api.Dtos;

namespace AcxiomCRM.Api.Helpers;

// Throw from services. ExceptionMiddleware converts it into { message, errors } JSON.
public class AppException : Exception
{
    public int StatusCode { get; }
    public List<FieldError>? Errors { get; }

    public AppException(int statusCode, string message, List<FieldError>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public static AppException BadRequest(string message, List<FieldError>? errors = null) => new(400, message, errors);

    // Single field validation error, e.g. Field("amount", "Opportunity Amount must be greater than 0.")
    public static AppException Field(string field, string message) =>
        new(400, "Please correct the highlighted fields.", new List<FieldError> { new(field, message) });

    public static AppException Fields(List<FieldError> errors) => new(400, "Please correct the highlighted fields.", errors);

    public static AppException Unauthorized(string message = "Please login to continue.") => new(401, message);

    public static AppException Forbidden(string message = "You do not have permission to perform this action.") => new(403, message);

    public static AppException NotFound(string entity = "Record") => new(404, $"{entity} not found.");

    public static AppException Conflict(string message, List<FieldError>? errors = null) => new(409, message, errors);
}
