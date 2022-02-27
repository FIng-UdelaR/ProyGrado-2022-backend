using System.Net.Http;
using System.Threading.Tasks;

namespace CloudManufacturingAPI.Repositories.SystemManagement
{
    public interface IMachineHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string uri);
        Task<HttpResponseMessage> PostAsync(string uri, StringContent stringContent);
        Task<HttpResponseMessage> PutAsync(string uri, StringContent stringContent);
    }
}
