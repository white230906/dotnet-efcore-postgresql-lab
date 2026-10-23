namespace production_api_skeleton.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
 
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
 
    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
 
    public async Task InvokeAsync(HttpContext context)
    {
        var providedCorrelationId =
            context.Request.Headers[HeaderName].FirstOrDefault();
 
        var correlationId = IsValid(providedCorrelationId)
            ? providedCorrelationId!
            : Guid.NewGuid().ToString("N");
 
        context.TraceIdentifier = correlationId;
        //TraceIdentifier: mã truy vết chuẩn của ASP.NET Core.
        context.Items[HeaderName] = correlationId;
        //Items: vùng lưu dữ liệu dùng chung trong phạm vi request hiện tại.
        
        //Middleware hoặc endpoint phía sau có thể lấy ID này ra sử dụng
        
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        //ngay trước khi response được gửi, thì thêm cái correlationId
        //Client có thể cung cấp ID này khi báo lỗi để backend tìm log
 
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId
               }))
            //gắn id vào toàn bộ log
        {
            await _next(context);
        }
    }
 
    private static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
        {
            return false;
        }
 
        return value.All(character =>
            char.IsLetterOrDigit(character) ||
            character is '-' or '_' or '.');
    }
}