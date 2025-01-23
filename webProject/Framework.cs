using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using webProject.Models;

namespace webProject;

public class Endpoint {
    public Endpoint(Predicate<HttpListenerContext> Condition, Func<HttpListenerContext, CancellationToken, Task> Body) {
        this.Condition = Condition;
        this.Body = Body;
    }
    public Predicate<HttpListenerContext> Condition { private set; get; }
    public Func<HttpListenerContext, CancellationToken, Task> Body { private set; get; }
}
public class Framework
{
    public List<Endpoint>? Endpoints { private set; get; }
    public Func<HttpListenerContext, CancellationToken, Task>? Default { private set; get; }
    public Framework() { }
    public Framework WithEndpoints(List<Endpoint> Endpoints) {
        this.Endpoints = Endpoints;
        return this;
    }
    public Framework WithDefault(Func<HttpListenerContext, CancellationToken, Task> Default) {
        this.Default = Default;
        return this;
    }
    public async Task Run() {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:5001/");
        httpListener.Start();
        Console.WriteLine("Started listening...");

        while (httpListener.IsListening) {
            var context = await httpListener.GetContextAsync();
            var ctx = new CancellationTokenSource();

            _ = Task.Run(async () =>
                {
                    foreach (Endpoint endpoint in Endpoints) {
                        if (endpoint.Condition(context))
                        {
                            try {
                                await endpoint.Body(context, ctx.Token);
                            }
                            catch (Exception ex) {
                                var error = ex.Message == "" ? new ErrorMessageModel(null) : new ErrorMessageModel(ex.Message);
                                Console.WriteLine(error.Error);
                                context.Response.StatusCode = 403;
                                context.Response.ContentType = "application/json";

                                await context.Response.OutputStream.WriteAsync(
                                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                                        new ErrorMessageModel(ex.Message),
                                        new JsonSerializerOptions
                                        {
                                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                        })),
                                    ctx.Token);
                            }
                            context.Response.OutputStream.Close();
                            context.Response.Close();
                            return;
                        }
                    }
                    if (Default != null) {
                        await Default(context, ctx.Token);
                    }
                    context.Response.OutputStream.Close();
                    context.Response.Close();
                }
            );
        }
        httpListener.Stop();
        httpListener.Close();
    }
}
