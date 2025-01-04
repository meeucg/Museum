using System.Collections.ObjectModel;
using System.Data;
using System.Net;
using Npgsql;

namespace webProject;

public class DbContext
{
    private const string DbConnectionString = "Host=localhost;Port=5002;Username=admin;Password=20052005;Database=postgres";
    private readonly NpgsqlConnection _dbConnection = new(DbConnectionString);

    public async Task<User> CreateUser(string login, string password, CancellationToken cancellationToken = default)
    {
        await _dbConnection.OpenAsync(cancellationToken);
        try
        {
            const string sqlQuery = "INSERT INTO users (login, password, role) VALUES (@login, @password, @role) RETURNING id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("login", login);
            cmd.Parameters.AddWithValue("password", password);
            cmd.Parameters.AddWithValue("role", "user");
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            return new User
            {
                Login = login,
                Password = password,
                Id = Convert.ToInt32(result),
                Role = "user"
            };
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }


    public async Task<User?> GetUser(string login, CancellationToken cancellationToken = default)
    {
        await _dbConnection.OpenAsync(cancellationToken);
        try
        {
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
                };
            }
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