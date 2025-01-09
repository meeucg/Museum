using System.Collections.ObjectModel;
using System.Data;
using System.Net;
using System.Text;
using Npgsql;
using webProject.Entities;
using webProject.Models;

namespace webProject;

public class DbContext
{
    private const string DbConnectionString = "Host=localhost;Port=5002;Username=admin;Password=20052005;Database=postgres";
    private readonly NpgsqlConnection _dbConnection = new(DbConnectionString);

    public async Task<User?> CreateUser(string login, string password, string username, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "INSERT INTO users (login, password, role, username) VALUES (@login, @password, @role, @username) RETURNING id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("login", login);
            cmd.Parameters.AddWithValue("password", password);
            cmd.Parameters.AddWithValue("role", "user");
            cmd.Parameters.AddWithValue("username", username);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            Console.WriteLine(result);

            return new User
            {
                Login = login,
                Password = password,
                Id = Convert.ToInt32(result),
                Role = "user",
                Username = username
            };
        }
        catch {
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }


    public async Task<User?> GetUserByLogin(string login, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "SELECT * FROM users WHERE login = @login";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("login", login);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
                return new User
                {
                    Id = reader.GetInt64("id"),
                    Login = login,
                    Password = reader.GetString("password"),
                    Role = reader.GetString("role"),
                    Username = reader.GetString("username"),
                };
            }
        }
        catch {
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }

        return null;
    }

    public async Task<User?> GetUserById(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "SELECT * FROM users WHERE id = @id";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("id", id);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
                return new User
                {
                    Id = id,
                    Login = reader.GetString("login"),
                    Password = reader.GetString("password"),
                    Role = reader.GetString("role"),
                    Username = reader.GetString("username"),
                };
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }

        return null;
    }

    public async Task<bool> DeleteAllUsers(CancellationToken cancellationToken = default) {
        await _dbConnection.OpenAsync(cancellationToken);
        try
        {
            const string sqlQuery = "BEGIN; DELETE FROM users; SELECT setval('user_id_seq', 1, false); COMMIT;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            return true;
        }
        catch 
        {
            return false;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public async Task<bool> GetAllUserIds(CancellationToken cancellationToken = default) {
        await _dbConnection.OpenAsync(cancellationToken);
        List<int> ids = new List<int>();
        try
        {
            const string sqlQuery = "SELECT * FROM users;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while(await reader.ReadAsync(cancellationToken)){
                int id = reader.GetInt32(0); 
                ids.Add(id);
            }
            foreach(var i in ids){
                Console.WriteLine(i);
            }
            return true;
        }
        catch 
        {
            return false;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }
}