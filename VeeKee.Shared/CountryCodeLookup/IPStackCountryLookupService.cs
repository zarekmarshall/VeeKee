using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VeeKee.Shared.CountryCodeLookup
{
    public class IPStackCountryLookupService
    {
        private const string APIKey = "4cc179684e2eb7c7a5e6e0326a437455";
        private const string UrlFormat = "http://api.ipstack.com/{0}?access_key={1}";

        public static async Task<string> LookupAsync(string host, CancellationToken cancellationToken)
        {
            string requestUrl = string.Format(UrlFormat, host, APIKey);

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
                {
                    using (var response = await client.SendAsync(request, cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync();
                        var countryCodeResponse = JsonConvert.DeserializeObject<IPStackCountryCodeResponse>(content);
                        return countryCodeResponse.country_code;
                    }
                }
            }
        }
    }
}
