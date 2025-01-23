using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace webProject
{
    public static class ArtAPI
    {
        private static string FieldsQuery = "fields=id,title,date_display,artist_display,dimensions_detail,artwork_type_title,image_id";

        private static HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://api.artic.edu"),
            DefaultRequestHeaders =
            {
                {
                    "User-Agent", "Mozilla/5.0"
                },
            }

        };

        public static async Task<ApiResponceSingle?> GetPictureJsonById(CancellationToken ctx, int id)
        {
            try
            {
                //Console.WriteLine("Started API request");
                var fullPath = $"/api/v1/artworks/{id}?" + FieldsQuery;
                var result = await _httpClient.GetFromJsonAsync<ApiResponceSingle>(fullPath,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }, cancellationToken: ctx);
                if (result == null)
                {
                    return null;
                }
                return result;
            }
            catch(Exception ex){
                return null;
            }
            //return null;
        }

        public static async Task<ApiResponceMultiple?> GetListOfPicturesBySearch(CancellationToken ctx, string search, int limit) {
            //Console.WriteLine("Started API request");
            var fullPath = $"/api/v1/artworks/search?q={search}&limit={limit}&" + FieldsQuery;
            try
            { 
                var result = await _httpClient.GetFromJsonAsync<ApiResponceMultiple>(fullPath,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }, cancellationToken: ctx);
                if (result == null)
                {
                    //throw new ApplicationException("Not found");
                    return null;
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
                //throw new ApplicationException("Not found");
                return null;
            }
            //return null;
        } 
    }   
}
