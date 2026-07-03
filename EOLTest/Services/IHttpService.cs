
// 用途：定义 HTTP 通信能力，支持 XML/JSON

using System.Threading;
using System.Threading.Tasks;
using EOLTest.Models;

namespace EOLTest.Services
{
    public interface IHttpService
    {
        Task<HttpResult<string>> PostXmlAsync(string url, string xmlBody, CancellationToken ct = default);
        Task<HttpResult<string>> PostJsonAsync(string url, string jsonBody, CancellationToken ct = default);
        Task<HttpResult<string>> GetStringAsync(string url, CancellationToken ct = default);
    }
}
