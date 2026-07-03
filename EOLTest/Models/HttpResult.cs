
// 用途：统一封装 HTTP 操作结果，区分成功/失败与错误类型

namespace EOLTest.Models
{
    public class HttpResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public HttpErrorType ErrorType { get; set; } = HttpErrorType.None;
        public string ErrorMessage { get; set; }
        public string ExceptionDetail { get; set; }

        public static HttpResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static HttpResult<T> Fail(HttpErrorType type, string message, string detail = null) =>
            new() { Success = false, ErrorType = type, ErrorMessage = message, ExceptionDetail = detail };
    }

    /// <summary>
    /// HTTP 错误分类
    /// </summary>
    public enum HttpErrorType
    {
        None,
        Timeout,            // 连接超时或总超时
        NetworkError,       // DNS/连接拒绝/断开
        HttpError,          // 4xx/5xx
        ParseError,         // 响应解析失败
        Cancelled,          // 用户取消
        Unknown
    }
}
