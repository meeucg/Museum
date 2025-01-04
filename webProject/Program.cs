using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using webProject;

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
                        var responce = new ApiResponceMultiple();
                        responce.Data = new List<Art>();

                        var typesFilter = new string[] { "Painting", "Print", "Drawing and Watercolor",
                            "Architectural Drawing", "Photograph" };

                        foreach (Art art in search.Data)
                        {
                            if (typesFilter.Contains(art.ArtworkTypeTitle))
                            {
                                responce.Data.Add(art);
                            }
                        }

                        responce.Info = search.Info;
                        responce.Config = search.Config;

                        response.StatusCode = 200;
                        response.ContentType = "application/json";
                        await response.OutputStream.WriteAsync(
                            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(responce, new JsonSerializerOptions
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
            case "/register" when request.HttpMethod == "GET":
                string rLogin;
                string rPassword;

                try
                {
                    var authQuery = request.Url?.Query.Split("?")[^1].Split("&") ?? throw new Exception();
                    if (authQuery.Length != 2)
                    {
                        throw new Exception();
                    }

                    rLogin = authQuery[0];
                    rPassword = authQuery[1];

                    if (rLogin == "" || rPassword == "")
                    {
                        throw new Exception();
                    }

                    if (await dbContext.GetUser(rLogin) != null)
                    {
                        response.StatusCode = 400;
                        response.ContentType = "text/plain";
                        await response.OutputStream.WriteAsync(
                            Encoding.UTF8.GetBytes("Such user already exists"), ctx.Token);
                        break;
                    }

                    await dbContext.CreateUser(rLogin, PasswordHasher.Hash(rPassword));
                }
                catch
                {
                    response.StatusCode = 403;
                    response.ContentType = "text/plain";
                    await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes("Bad request"), ctx.Token);
                    break;
                }
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes($"Successfully added user: {rLogin}"), ctx.Token);
                break;
            case "/login" when request.HttpMethod == "GET":
                string login;
                string password;

                try
                {
                    var authQuery = request.Url?.Query.Split("?")[^1].Split("&") ?? throw new Exception();
                    if (authQuery.Length != 2)
                    {
                        throw new Exception();
                    }

                    login = authQuery[0];
                    password = authQuery[1];

                    if (login == "" || password == "")
                    {
                        throw new Exception();
                    }

                    var user = await dbContext.GetUser(login);

                    if (user != null)
                    {
                        if (PasswordHasher.Validate(user.Password, password))
                        {
                            response.StatusCode = 200;
                            response.ContentType = "text/plain";
                            await response.OutputStream.WriteAsync(
                                Encoding.UTF8.GetBytes($"Succsessfully loged in as {login}"), ctx.Token);
                            break;
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ContentType = "text/plain";
                            await response.OutputStream.WriteAsync(
                                Encoding.UTF8.GetBytes($"Wrong password"), ctx.Token);
                            break;
                        }
                    }

                    response.StatusCode = 404;
                    response.ContentType = "text/plain";
                    await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes($"User {login} doesn't exist"), ctx.Token);
                    break;

                }
                catch (Exception ex)
                {
                    response.StatusCode = 403;
                    response.ContentType = "text/plain";
                    await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes($"Bad request, {ex.Message}"), ctx.Token);
                    break;
                }
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
        //"jpg" => "image/jpg",
        _ => throw new ArgumentOutOfRangeException()
    };

    var file = await File.ReadAllBytesAsync($"./resources/{type}/{path}", token);
    await context.Response.OutputStream.WriteAsync(file, token);
}
