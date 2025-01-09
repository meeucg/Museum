using System.Net;
using System.Text;
using System.Text.Json;
using webProject;
using webProject.Models;
using webProject.Entities;
using Microsoft.Win32;
using System.Threading;

var httpListener = new HttpListener();
var dbContext = new DbContext();
httpListener.Prefixes.Add("http://localhost:5001/");
httpListener.Start();
Console.WriteLine("Started listening...");

while (httpListener.IsListening)
{
    var context = await httpListener.GetContextAsync();
    var response = context.Response;
    var request = context.Request;
    var localPath = request.Url?.LocalPath;
    var ctx = new CancellationTokenSource();
    byte[]? file;

    _ = Task.Run(async () =>
    {
        switch (localPath)
        {
            case "/home" when request.HttpMethod == "GET":
                Console.WriteLine("HTML Request");
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";
                file = await File.ReadAllBytesAsync("./resources/html/index.html", ctx.Token);
                await context.Response.OutputStream.WriteAsync(file, ctx.Token);
                break;
            case "/search" when request.HttpMethod == "GET":
                var query = request.Url?.Query.Split("?")[^1];
                if (query is null || query == "")
                {
                    response.StatusCode = 400;
                }
                else
                {
                    try
                    {
                        var search = await ArtAPI.GetListOfPicturesBySearch(ctx.Token, query!, 100);
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
                            })), ctx.Token);
                    }
                    catch
                    {
                        response.StatusCode = 404;
                    }
                }
                break;
            case "/register" when request.HttpMethod == "POST":
                try
                {
                    var register = await Register(context, ctx.Token);

                    if (register != null)
                    {
                        response.StatusCode = 200;
                        response.ContentType = "application/json";

                        Console.WriteLine(register.Token);
                        await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(register, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })), ctx.Token);
                        }
                }
                catch (Exception ex)
                {
                    response.StatusCode = 403;
                    response.ContentType = "application/json";
                    
                    await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                            new ErrorMessageModel 
                            { 
                                Error = $"Bad request, {ex.Message}" 
                            }, 
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            })), 
                        ctx.Token);
                    break;
                }
                break;
            case "/login" when request.HttpMethod == "POST":
                try
                {
                    var login = await Login(context, ctx.Token);

                    if (login != null) {
                        response.StatusCode = 200;
                        response.ContentType = "application/json";

                        await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(login, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })), ctx.Token);
                    }
                }
                catch (Exception ex)
                {
                    response.StatusCode = 403;
                    response.ContentType = "application/json";

                    await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                            new ErrorMessageModel
                            {
                                Error = $"Bad request, {ex.Message}"
                            },
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            })),
                        ctx.Token);
                    break;
                }
                break;
            case "/userinfo" when request.HttpMethod == "GET":
                try
                {
                    var userJwt = await Authorize(context, ctx.Token);

                    if (userJwt != null)
                    {
                        var user = await dbContext.GetUserById(userJwt.Id, ctx.Token);

                        if (user != null)
                        {
                            response.StatusCode = 200;
                            response.ContentType = "application/json";

                            await response.OutputStream.WriteAsync(
                                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new UserInfoModel(user), new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                })), ctx.Token);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    response.StatusCode = 403;
                    response.ContentType = "application/json";
                    await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                            new ErrorMessageModel
                            {
                                Error = $"Bad request, {ex.Message}"
                            },
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            })),
                        ctx.Token);
                    break;
                }
                break;
            case "/reset" when request.HttpMethod == "GET":
                await dbContext.DeleteAllUsers();
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                await response.OutputStream.WriteAsync(
                    Encoding.UTF8.GetBytes($"User table was cleared"), ctx.Token);
                break;
            case "/users" when request.HttpMethod == "GET":
                await dbContext.GetAllUserIds();
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                await response.OutputStream.WriteAsync(
                    Encoding.UTF8.GetBytes($"Check console"), ctx.Token);
                break;
            case "/iiif" when request.HttpMethod == "GET":
                string id = "No query";
                try
                {
                    id = request.Url?.Query.Split("?")[^1] ?? throw new Exception();

                    using Stream requestStream = (await ImgAPI.GetPictureById(id!, ctx.Token)).Content.ReadAsStream(ctx.Token);
                    using Stream responseStream = response.OutputStream;
                    await requestStream.CopyToAsync(responseStream, ctx.Token);
                    response.StatusCode = 200;
                    response.ContentType = "image/jpg";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(id + " : " + ex.Message);
                    response.StatusCode = 404;
                }
                break;
            default:
                await ShowResourseFile(context, ctx.Token);
                break;
        }

        response.OutputStream.Close();
        response.Close();
    });
}
httpListener.Stop();
httpListener.Close();


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


async Task<AuthResult?> Login(HttpListenerContext context, CancellationToken cancellationToken)
{
    var response = context.Response;

    using var sr = new StreamReader(context.Request.InputStream);
    var userLoginModel = JsonSerializer.Deserialize<UserLoginModel>(
        await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false),
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    if (userLoginModel == null)
    {
        response.StatusCode = 400;
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;

        await response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ErrorMessageModel
                {
                    Error = "No data passed!"
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })),
        cancellationToken).ConfigureAwait(false);
        return null;
    }

    var user = await dbContext.GetUserByLogin(userLoginModel!.Login, cancellationToken);
    if (user == null)
    {
        response.StatusCode = 401;
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;

        await response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ErrorMessageModel
                {
                    Error = $"No such user: {userLoginModel.Login}"
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })),
        cancellationToken).ConfigureAwait(false);
        return null;
    }

    if (!PasswordHasher.Validate(user.Password, userLoginModel.Password))
    {
        response.StatusCode = 401;
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;

        await response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ErrorMessageModel
                {
                    Error = "Wrong password!"
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })),
        cancellationToken).ConfigureAwait(false);
        return null;
    }

    return new AuthResult
    {
        Token = JwtWorker.GenerateJwtToken(user)
    };
}

async Task<AuthResult?> Register(HttpListenerContext context, CancellationToken cancellationToken)
{
    var response = context.Response;

    using var sr = new StreamReader(context.Request.InputStream);
    var userLoginModel = JsonSerializer.Deserialize<UserLoginModel>(
        await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false),
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

    var userValidatorRule = new UserValidationRules();
    var userValidationRulesExample = userValidatorRule.Validate(userLoginModel);

    if (!userValidationRulesExample.IsValid)
    {
        response.StatusCode = 400;
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;

        await response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ErrorMessageModel
                {
                    Error = $"Data isn't valid! Info: " +
                            $"{string.Join("\n\r", userValidationRulesExample.Errors.Select(er => er.ErrorMessage))}"
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })),
        cancellationToken).ConfigureAwait(false);
        return null;
    }

    userLoginModel!.Password = PasswordHasher.Hash(userLoginModel.Password);

    var checkUser = await dbContext.GetUserByLogin(userLoginModel!.Login, cancellationToken);

    if (checkUser != null)
    {
        response.StatusCode = 400;
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;

        await response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ErrorMessageModel
                {
                    Error = "Such user already exists"
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })),
        cancellationToken).ConfigureAwait(false);
        return null;
    }

    var registerResult = await dbContext.CreateUser(userLoginModel!.Login, userLoginModel.Password, userLoginModel.Username, cancellationToken);

    if (registerResult == null)
    {
        response.StatusCode = 400;
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;

        await response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ErrorMessageModel
                {
                    Error = "Sorry, something went wrong"
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })),
        cancellationToken).ConfigureAwait(false);
        return null;
    }

    return new AuthResult
    {
        Token = JwtWorker.GenerateJwtToken(registerResult)
    };
}

async Task<User?> Authorize(HttpListenerContext context, CancellationToken cancellationToken)
{
    var response = context.Response;

    var token = context.Request.Headers["Authorization"];
    Console.WriteLine(token);

    if (token == null)
    {
        response.StatusCode = 403;
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;

        await response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ErrorMessageModel
                {
                    Error = "You haven't used the site for a long time, please login again!"
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })),
        cancellationToken).ConfigureAwait(false);
        return null;
    }

    var tokenValidationResult = JwtWorker.ValidateJwtToken(token);

    if (tokenValidationResult.isSuccess)
    {
        Console.WriteLine("Success");
        return tokenValidationResult.user;
    }

    response.StatusCode = 403;
    response.ContentType = "application/json";
    response.ContentEncoding = Encoding.UTF8;

    await response.OutputStream.WriteAsync(
    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
        new ErrorMessageModel
        {
            Error = "You haven't used the site for a long time, please login again!"
        },
        new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })),
    cancellationToken).ConfigureAwait(false);
    return null;
}