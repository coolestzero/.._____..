
// 用途：在线配置操作的业务结果封装

namespace EOLTest.Models
{
    public class OnlineConfigResult
    {
        public bool Success { get; set; }
        public Vehicle Vehicle { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetail { get; set; }
        public HttpErrorType? ErrorType { get; set; }

        public static OnlineConfigResult Ok(Vehicle vehicle) => new() { Success = true, Vehicle = vehicle };

        public static OnlineConfigResult Fail(string message, string detail = null) =>
            new() { Success = false, ErrorMessage = message, ErrorDetail = detail };

        public static OnlineConfigResult Fail(HttpResult<string> httpResult) =>
            new()
            {
                Success = false,
                ErrorMessage = httpResult.ErrorMessage,
                ErrorDetail = httpResult.ExceptionDetail,
                ErrorType = httpResult.ErrorType
            };
    }
}
