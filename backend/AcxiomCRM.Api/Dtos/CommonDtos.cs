namespace AcxiomCRM.Api.Dtos;

public record FieldError(string Field, string Message);

// Every error response has this shape: { "message": "...", "errors": [ { "field": "email", "message": "..." } ] }
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<FieldError>? Errors { get; set; }

    public ErrorResponse(string message, List<FieldError>? errors = null)
    {
        Message = message;
        Errors = errors;
    }
}

public record MessageResponse(string Message);
