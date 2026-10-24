namespace production_api_skeleton.Domain.Exceptions;

public class ConcurrencyConflictException: Exception
{
    public ConcurrencyConflictException(string message) 
        : base(message) 
    { 
    } 
 
    public ConcurrencyConflictException(string message, Exception innerException) 
        : base(message, innerException) 
    { 
    }
}