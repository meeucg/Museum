using System.Net;
using System.Text;
using System.Text.Json;
using webProject;
using webProject.Models;
using webProject.Entities;
using HandlebarsDotNet;
using System.Net.Mime;
using Microsoft.Win32;
using System.Threading;
using webProject.models;

async Task ShowResourseFile(HttpListenerContext context, CancellationToken token)
{
    if (context.Request.Url is null)
    {
        context.Response.StatusCode = 404;
        return;
    }

    var path = context.Request.Url?.LocalPath.Split('/')[^1];

    context.Response.StatusCode = 200;
    var type = path!.Split('.')[^1];
    context.Response.ContentType = type switch
    {
        "html" => "text/html",
        "css" => "text/css",
        "js" => "text/javascript",
        "png" => "image/png",
        "svg" => "image/svg+xml",
        _ => throw new ArgumentOutOfRangeException()
    };

    var file = await File.ReadAllBytesAsync($"./resources/{type}/{path}", token);
    await context.Response.OutputStream.WriteAsync(file, token);
}

var homeEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/home"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            Console.WriteLine("HTML Request");
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html";
            var file = await File.ReadAllBytesAsync("./resources/html/index.html", token);
            await context.Response.OutputStream.WriteAsync(file, token);
        }
    );

var searchEndpoint =
    new Endpoint(
        (context) => {
            var query = context.Request.Url?.Query.Split("?")[^1];
            return context.Request.Url?.LocalPath == "/search"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            var response = context.Response;
            var query = context.Request.Url?.Query.Split("?")[^1];
            if (query is null || query == "")
            {
                response.StatusCode = 400;
                return;
            }
            var search = await ArtAPI.GetListOfPicturesBySearch(token, query!, 100) ?? throw new Exception("Not found");
            var result = new ApiResponceMultiple();
            result.Data = new List<Art>();

            var typesFilter = new string[] { "Painting", "Print", "Drawing and Watercolor",
                    "Architectural Drawing", "Photograph" };

            foreach (Art art in search.Data)
            {
                if (typesFilter.Contains(art.ArtworkTypeTitle))
                {
                    result.Data.Add(art);
                }
            }

            result.Info = search.Info;
            result.Config = search.Config;

            response.StatusCode = 200;
            response.ContentType = "application/json";
            await response.OutputStream.WriteAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })), token);
        }
    );

var iiifEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/iiif"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            var response = context.Response;
            var request = context.Request;
            string id = request.Url?.Query.Split("?")[^1] ?? throw new Exception();
            using Stream requestStream = (await ImgAPI.GetPictureById(id!, token)).Content.ReadAsStream(token);
            using Stream responseStream = response.OutputStream;
            await requestStream.CopyToAsync(responseStream, token);
            response.StatusCode = 200;
            response.ContentType = "image/jpg";
        }
    );

var registerEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/register"
                    && context.Request.HttpMethod == "POST";
        },
        async (context, token) => {
            var response = context.Response;
            var register = await UserMethods.Register(context, token);

            if (register != null)
            {
                response.StatusCode = 200;
                response.ContentType = "application/json";
                response.Headers["Set-Cookie"] = $"token={register.Token};path=/; expires={DateTime.Now.AddMinutes(30)}";

                Console.WriteLine(register.Token);
                await response.OutputStream.WriteAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(register, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })), token);
            }
        }
    );

var loginEndpoint =
    new Endpoint(
        (context) =>
        {
            return context.Request.Url?.LocalPath == "/login"
                    && context.Request.HttpMethod == "POST";
        },
        async (context, token) =>
        {
            var response = context.Response;
            var login = await UserMethods.Login(context, token);

            if (login != null)
            {
                response.StatusCode = 200;
                response.ContentType = "application/json";
                response.Headers["Set-Cookie"] = $"token={login.Token};path=/; expires={DateTime.Now.AddMinutes(30)}";

                await response.OutputStream.WriteAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(login, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })), token);
            }
        }
    );

var profileEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/profile"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            var response = context.Response;
            var request = context.Request;

            int id;
            if (!Int32.TryParse(request.QueryString["id"], out id))
            {
                throw new Exception("Not valid url");
            }

            var user = await DbContext.GetUserById(id);
            if (user != null)
            {
                bool edit = false;
                User? auth = null;
                try
                {
                    auth = await UserMethods.Authorize(context, token);
                }
                catch { }
                
                if (auth != null && auth.Id == id)
                {
                    edit = true;
                }

                string profileTemplatePath = "./resources/hbs/profileTemplate.hbs";
                string source = File.ReadAllText(profileTemplatePath);
                var template = Handlebars.Compile(source);

                List<Collection> collections = new();
                var collectionIds = await DbContext.GetAllUserCollectionIds(id, token) ?? [];
                foreach (var collectionId in collectionIds) {
                    var newCollection = await DbContext.GetCollectionById(collectionId, token);
                    if (newCollection == null) {
                        continue;
                    }
                    List<string?> previews = [null, null, null];
                    var pictureIds = await DbContext.GetAllPictureIdsFromCollection(newCollection.Id, token);
                    if (pictureIds == null) {
                        continue;
                    }
                    for (int i = pictureIds.Count - 1; i >= 0 && i >= pictureIds.Count - 3; i--) {
                        var picture = await DbContext.GetPictureById(pictureIds[i], token);
                        if (picture == null) {
                            continue;
                        }
                        previews[pictureIds.Count - i - 1] = picture.ImageUrl;
                    }
                    newCollection.PreviewImages = previews;
                    collections.Add(newCollection);
                }

                var data = new
                {
                    username = user.Username,
                    email = user.Login,
                    edit = edit,
                    avatarLetter = char.ToUpper(user.Username[0]),
                    collections = collections,
                    banner = $"/banner?id={user.Id}",
                };
                var result = template(data);

                response.ContentType = "text/html";
                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(result), token);
            }
            else
            {
                throw new Exception("Such user doesn't exist");
            }
        }
    );

var userInfoEndpoint =
    new Endpoint(
        (context) =>
        {
            return context.Request.Url?.LocalPath == "/userinfo"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) =>
        {
            var response = context.Response;
            var auth = await UserMethods.Authorize(context, token);

            if (auth != null)
            {
                var user = await DbContext.GetUserById(auth.Id, token);

                if (user != null)
                {
                    response.StatusCode = 200;
                    response.ContentType = "application/json";

                    await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new UserInfoModel(user), new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })), token);
                }
            }
        }
    );

var postUserPictureEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/userpicture"
                    && context.Request.HttpMethod == "POST";
        },
        async (context, token) => {
            var auth = await UserMethods.Authorize(context, token);
            if (auth == null) {
                throw new Exception("Unauthorized request");
            }

            var response = context.Response;
            var request = context.Request;

            using var sr = new StreamReader(request.InputStream);
            var picture = JsonSerializer.Deserialize<UserPictureModel>(
                await sr.ReadToEndAsync(token).ConfigureAwait(false),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Invalid request body");

            var pictureStream = new MemoryStream(Convert.FromBase64String(picture.ImageBase64));
            var pictureId = Guid.NewGuid().ToString();
            if(!await MinioMuseum.SetPicture(pictureId, picture.ContentType, pictureStream, token)) throw new Exception("Sorry, something went wrong");
            var pictureResult = await DbContext.CreateUserPicture(picture, $"/minioimage?id={pictureId}", auth.Id, token) ?? throw new Exception("Sorry, something went wrong");
            
            response.StatusCode = 200;
        }
    );

var getUserPictureImageEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/minioimage"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            var response = context.Response;
            var request = context.Request;
            var id = request.QueryString["id"] ?? throw new Exception("No id passed");

            if (!await MinioMuseum.GetPicture(context, id, token))
            {
                throw new Exception("Invalid id or broken file");
            }
        }
    );

var getUserPictureEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/userpicture"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            var response = context.Response;
            var request = context.Request;
            var idString = request.QueryString["id"] ?? throw new Exception("No id passed");
            long id;
            if (!long.TryParse(idString, out id)) {
                throw new Exception("Invalid id");
            }

            var picture = await DbContext.GetPictureById(id, token) ?? throw new Exception("Picture doesn't exist");

            response.StatusCode = 200;
            await response.OutputStream.WriteAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(picture, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })), token);
        }
    );

var postUserCollectionEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/addcollection"
                    && context.Request.HttpMethod == "POST";
        },
        async (context, token) => {
            var auth = await UserMethods.Authorize(context, token);
            if (auth == null)
            {
                throw new Exception("Unauthorized request");
            }

            var response = context.Response;
            var request = context.Request;

            using var sr = new StreamReader(request.InputStream);
            var collection = JsonSerializer.Deserialize<CollectionModel>(
                await sr.ReadToEndAsync(token).ConfigureAwait(false),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Invalid request body");

            await DbContext.CreateCollection(collection, auth.Id, token) ;
            response.StatusCode = 200;
        }
    );

var likeArticPictureEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/likeartic"
                    && context.Request.HttpMethod == "POST";
        },
        async (context, token) => {
            var auth = await UserMethods.Authorize(context, token);
            if (auth == null)
            {
                throw new Exception("Unauthorized request");
            }

            var response = context.Response;
            var request = context.Request;

            using var sr = new StreamReader(request.InputStream);
            var pictureModel = JsonSerializer.Deserialize<ArtistPictureModel>(
                await sr.ReadToEndAsync(token).ConfigureAwait(false),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Invalid request body");


            var pictureDbId = await DbContext.GetPictureIdByApiId(pictureModel.Id, token);

            if (pictureDbId == null)
            {
                var getPicture = await ArtAPI.GetPictureJsonById(token, pictureModel.Id) ?? throw new Exception("Picture not found");
                Console.WriteLine(getPicture.Data.ArtistDisplay);

                var newPicture = new UserPictureModel
                {
                    Name = getPicture.Data.Title ?? "Unnamed",
                    Description = getPicture.Data.ArtistDisplay + " | " + getPicture.Data.DateDisplay,
                    ImageBase64 = "",
                    ContentType = "image/jpg"
                };

                var pictureResult = await DbContext.CreateApiPicture(newPicture, $"/iiif?{getPicture.Data.ImageId}", token)
                    ?? throw new Exception("Sorry, something went wrong");
                await DbContext.SetApiIdToPictureId(pictureModel.Id, pictureResult.Id);
                await DbContext.Like(pictureResult.Id, auth.Id);
            }
            else 
            {
                await DbContext.Like(pictureDbId ?? 0, auth.Id); // ?? 0 просто для преобразования long? в long
            }

            response.StatusCode = 200;
        }
    );

var dislikeArticPictureEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/dislikeartic"
                    && context.Request.HttpMethod == "POST";
        },
        async (context, token) => {
            var auth = await UserMethods.Authorize(context, token);
            if (auth == null)
            {
                throw new Exception("Unauthorized request");
            }

            var response = context.Response;
            var request = context.Request;

            using var sr = new StreamReader(request.InputStream);
            var pictureModel = JsonSerializer.Deserialize<ArtistPictureModel>(
                await sr.ReadToEndAsync(token).ConfigureAwait(false),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Invalid request body");


            var pictureDbId = await DbContext.GetPictureIdByApiId(pictureModel.Id, token);

            if (pictureDbId == null)
            {
                throw new Exception("Picture is not in user likes already");
            }
            else
            {
                await DbContext.Disike(pictureDbId ?? 0, auth.Id); // ?? 0 просто для преобразования long? в long
            }

            response.StatusCode = 200;
        }
    );

var getUserApiLikes =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/userarticlikes"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            var auth = await UserMethods.Authorize(context, token);
            if (auth == null)
            {
                throw new Exception("Unauthorized request");
            }

            var response = context.Response;
            var request = context.Request;

            var likes = await DbContext.GetAllUserApiLikes(auth.Id, token);
            //foreach (var like in likes) {
            //    Console.WriteLine(like);
            //}
            response.StatusCode = 200;
            await response.OutputStream.WriteAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(likes, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })), token);
        }
    );

var checkAuthEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/checkauth"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            var auth = await UserMethods.Authorize(context, token);
            if (auth == null)
            {
                throw new Exception("Unauthorized request");
            }
            context.Response.StatusCode = 200;
        }
    );

var getUserBannerEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/banner"
                    && context.Request.HttpMethod == "GET";
        },
        async (context, token) => {
            var response = context.Response;
            var request = context.Request;

            int id;
            if (!Int32.TryParse(request.QueryString["id"], out id))
            {
                throw new Exception("Not valid url");
            }

            if (!await MinioMuseum.GetBanner(context, id, token)) {
                throw new Exception("User doesn't have a banner");
            }

            response.StatusCode = 200;
        }
    );

var postUserBannerEndpoint =
    new Endpoint(
        (context) => {
            return context.Request.Url?.LocalPath == "/banner"
                    && context.Request.HttpMethod == "POST";
        },
        async (context, token) => {
            var response = context.Response;
            var request = context.Request;

            var auth = await UserMethods.Authorize(context, token);
            if (auth == null)
            {
                throw new Exception("Unauthorized request");
            }

            using var sr = new StreamReader(request.InputStream);
            var banner = JsonSerializer.Deserialize<UserBannerModel>(
                await sr.ReadToEndAsync(token).ConfigureAwait(false),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Invalid request body");

            var bannerStream = new MemoryStream(Convert.FromBase64String(banner.ImageBase64));
            if (!await MinioMuseum.SetBanner(auth.Id, banner.ContentType, bannerStream, token)) throw new Exception("Sorry, something went wrong");

            response.StatusCode = 200;
        }
    );

var Framework = new Framework()
    .WithEndpoints(
        [
            homeEndpoint,
            searchEndpoint,
            iiifEndpoint,
            registerEndpoint,
            loginEndpoint,
            profileEndpoint,
            userInfoEndpoint,
            postUserPictureEndpoint,
            getUserPictureEndpoint,
            getUserPictureImageEndpoint,
            postUserCollectionEndpoint,
            likeArticPictureEndpoint,
            dislikeArticPictureEndpoint,
            getUserApiLikes,
            checkAuthEndpoint,
            getUserBannerEndpoint,
            postUserBannerEndpoint
        ]
    )
    .WithDefault(ShowResourseFile);

await Framework.Run();