using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Minio.Helper;

namespace webProject;

public static class MinioMuseum
{
    private static string endpoint = "localhost:9000";
    private static string user = "museum";
    private static string password = "museumpassword";

    private static IMinioClient minio = new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(user, password)
        .WithSSL(false)
        .Build();

    public static async Task ListBuckets() {
        var buckets = await minio.ListBucketsAsync();
        foreach (var bucket in buckets.Buckets)
        {
            Console.WriteLine(bucket.Name);
        }
    }

    public static async Task<bool> GetBanner(HttpListenerContext context, long userid, CancellationToken ctx = default) { 
        var response = context.Response;
        try {
            var obj = await minio.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket("userdata")
                    .WithObject($"{userid}/banner")
                    .WithCallbackStream((stream) =>
                    {
                        stream.CopyTo(response.OutputStream);
                    }
                )
            , ctx);

            response.ContentType = obj.ContentType;
            response.StatusCode = 200;
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine(ex.Message);
            response.StatusCode = 404;
            return false;
        }
    }

    public static async Task<bool> SetBanner(long userid, string contentType, Stream content, CancellationToken ctx)
    {
        string filename = $"{userid}/banner";

        try {
            await minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket("userdata")
                    .WithContentType(contentType)
                    .WithObject(filename)
                    .WithStreamData(content)
                    .WithObjectSize(-1),
                ctx);
            return true;
        }
        catch(Exception ex) {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}
