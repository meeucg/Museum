﻿using System.Net;
using System.Text;
using System.Text.Json;
using webProject;
using webProject.Models;
using webProject.Entities;
using HandlebarsDotNet;
using System.Net.Mime;

var httpListener = new HttpListener();
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
    var query = "";
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
                query = request.Url?.Query.Split("?")[^1];
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
                    var register = await UserMethods.Register(context, ctx.Token);

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
                    var login = await UserMethods.Login(context, ctx.Token);

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
            case "/profile" when request.HttpMethod == "GET":
                query = request.QueryString["id"];
                try
                {
                    int id;
                    if (!Int32.TryParse(query, out id))
                    {
                        throw new Exception("Not valid url");
                    }

                    var user = await DbContext.GetUserById(id);
                    if (user != null)
                    {
                        string profileTemplatePath = "./resources/hbs/profileTemplate.hbs";
                        string source = File.ReadAllText(profileTemplatePath);
                        var template = Handlebars.Compile(source);
                        var data = new
                        {
                            username = user.Username,
                            email = user.Login,
                            edit = true,
                            avatarLetter = char.ToUpper(user.Username[0]),
                            collections = new List<Collection> { 
                                new Collection("Name 1", "Description example. 32 symbols."),
                                new Collection("Name 2", "Description example. 32 symbols."),
                                new Collection("Name 3", "Description example. 32 symbols."),
                                new Collection("Name 4", "Description example. 32 symbols."),
                                new Collection("Name 5", "Description example. 32 symbols."),
                                new Collection("Name 6", "Description example. 32 symbols."),
                                new Collection("Name 7", "Description example. 32 symbols."),
                            },
                            banner = "/image3.png",
                        };
                        var result = template(data);

                        response.ContentType = "text/html";
                        response.StatusCode = 200;
                        await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(result), ctx.Token);
                        break;
                    }
                    else { 
                        throw new Exception("such user doesn't exist");
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
                    var userJwt = await UserMethods.Authorize(context, ctx.Token);

                    if (userJwt != null)
                    {
                        var user = await DbContext.GetUserById(userJwt.Id, ctx.Token);

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
                await DbContext.DeleteAllUsers();
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                await response.OutputStream.WriteAsync(
                    Encoding.UTF8.GetBytes($"User table was cleared"), ctx.Token);
                break;
            case "/users" when request.HttpMethod == "GET":
                await DbContext.GetAllUserIds();
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                await response.OutputStream.WriteAsync(
                    Encoding.UTF8.GetBytes($"Check console"), ctx.Token);
                break;
            case "/minio" when request.HttpMethod == "POST":
                var userInfo = await UserMethods.Authorize(context, ctx.Token);

                if (userInfo == null) {
                    response.StatusCode = 400;
                    break;
                }

                await MinioMuseum.SetBanner(userInfo.Id, "image/png", request.InputStream, ctx.Token);
                break;
            case "/minio" when request.HttpMethod == "GET":
                userInfo = await UserMethods.Authorize(context, ctx.Token);

                if (userInfo == null) {
                    response.StatusCode = 400;
                    break;
                }

                await MinioMuseum.GetBanner(context, userInfo.Id, ctx.Token);
                break;
            case "/iiif" when request.HttpMethod == "GET":
                try
                {
                    string id = request.Url?.Query.Split("?")[^1] ?? throw new Exception();

                    using Stream requestStream = (await ImgAPI.GetPictureById(id!, ctx.Token)).Content.ReadAsStream(ctx.Token);
                    using Stream responseStream = response.OutputStream;
                    await requestStream.CopyToAsync(responseStream, ctx.Token);
                    response.StatusCode = 200;
                    response.ContentType = "image/jpg";
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(id + " : " + ex.Message);
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