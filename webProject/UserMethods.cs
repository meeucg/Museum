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
            throw new Exception("No data passed");
        }

        var user = await DbContext.GetUserByLogin(userLoginModel.Login, cancellationToken);
        if (user == null)
        {
            response.StatusCode = 401;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            throw new Exception($"No such user: {userLoginModel.Login}");
        }

        if (!PasswordHasher.Validate(user.Password, userLoginModel.Password))
        {
            response.StatusCode = 401;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            throw new Exception("Wrong password");
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
            throw new Exception($"Data isn't valid! Info: " +
                                $"{string.Join("\n\r", userValidationRulesExample.Errors.Select(er => er.ErrorMessage))}");
        }

        userLoginModel!.Password = PasswordHasher.Hash(userLoginModel.Password);

        var checkUser = await DbContext.GetUserByLogin(userLoginModel!.Login, cancellationToken);

        if (checkUser != null)
        {
            response.StatusCode = 400;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            throw new Exception("Such user already exists");
        }

        var registerResult = await DbContext.CreateUser(userLoginModel, cancellationToken);

        if (registerResult == null)
        {
            response.StatusCode = 400;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            throw new Exception("Sorry, something went wrong");
        }

        return new AuthResult
        {
            Token = JwtWorker.GenerateJwtToken(registerResult)
        };
    }

    public static async Task<User?> Authorize(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var response = context.Response;
        var token = context.Request.Cookies["token"];

        if (token == null)
        {
            response.StatusCode = 403;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            throw new Exception("You haven't used the site for a long time, please login again!");
        }

        var tokenValidationResult = JwtWorker.ValidateJwtToken(token.Value);

        if (tokenValidationResult.isSuccess)
        {
            return tokenValidationResult.user;
        }

        response.StatusCode = 403;
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;
        throw new Exception("You haven't used the site for a long time, please login again!");
    }
}
