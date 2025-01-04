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
        private static SemaphoreSlim semaphore = new SemaphoreSlim(2, 8);
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

        public static async Task<HttpResponseMessage> GetPictureById(string id, CancellationToken ctx) {

            await semaphore.WaitAsync(ctx);

            var res = await _httpClient.GetAsync($"https://www.artic.edu/iiif/2/{id}/full/900,/0/default.jpg", ctx);
            if (res.StatusCode == HttpStatusCode.OK) {
                semaphore.Release();
                return res;
            }

            semaphore.Release();
            throw new Exception("Image not found");
        }
    }
}
