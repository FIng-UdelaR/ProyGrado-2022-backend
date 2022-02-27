using System.Net.Http;
using System.Threading.Tasks;

namespace CloudManufacturingAPI.Repositories.SystemManagement
{
    public class MachineHttpClient : IMachineHttpClient
    {
        static readonly HttpClient httpClient = new HttpClient();

        public Task<HttpResponseMessage> GetAsync(string uri)
        {
            return httpClient.GetAsync(uri);
        }

        public Task<HttpResponseMessage> PostAsync(string uri, StringContent stringContent)
        {
            return httpClient.PostAsync(uri, stringContent);
        }

        public Task<HttpResponseMessage> PutAsync(string uri, StringContent stringContent)
        {
            return httpClient.PutAsync(uri, stringContent);
        }
    }
}
