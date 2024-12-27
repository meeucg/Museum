using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace webProject
{
    public static class ImgAPI
    {
        private static HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://api.artic.edu/iiif/2"),
            DefaultRequestHeaders =
            {
                {
                    "User-Agent", "Mozilla/5.0"
                },
            }

        };

        public static async Task<HttpResponseMessage> GetPictureById(CancellationToken ctx, string id) {
            var res = await _httpClient.GetAsync($"https://www.artic.edu/iiif/2/{id}/full/4000,/0/default.jpg");
            if (res.StatusCode == HttpStatusCode.OK) {
                return res;
            }
            throw new Exception("Image not found");
        }
    }
}
