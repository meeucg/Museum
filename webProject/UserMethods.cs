using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using webProject.Entities;
using webProject.Models;

namespace webProject;

public static class UserMethods
{
    public static async Task<AuthResult?> Login(HttpListenerContext context, CancellationToken cancellationToken)
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

        var user = await DbContext.GetUserByLogin(userLoginModel!.Login, cancellationToken);
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

    public static async Task<AuthResult?> Register(HttpListenerContext context, CancellationToken cancellationToken)
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

        var checkUser = await DbContext.GetUserByLogin(userLoginModel!.Login, cancellationToken);

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

        var registerResult = await DbContext.CreateUser(userLoginModel!.Login, userLoginModel.Password, userLoginModel.Username, cancellationToken);

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

    public static async Task<User?> Authorize(HttpListenerContext context, CancellationToken cancellationToken)
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
}
