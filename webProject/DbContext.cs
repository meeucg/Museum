using System.Collections.ObjectModel;
using System.Data;
using System.Net;
using System.Text;
using System.Transactions;
using Npgsql;
using webProject.Entities;
using webProject.Models;

namespace webProject;

public static class DbContext
{
    private const string DbConnectionString = "Host=localhost;Port=5002;Username=admin;Password=20052005;Database=postgres";
    private static readonly NpgsqlConnection _dbConnection = new(DbConnectionString);


    public static async Task<User?> CreateUser(UserLoginModel user, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);
            long id;

            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    const string sqlQuery1 = "INSERT INTO users (login, password, role, username) VALUES (@login, @password, @role, @username) RETURNING id;";
                    var cmd1 = new NpgsqlCommand(sqlQuery1, _dbConnection);
                    cmd1.Parameters.AddWithValue("login", user.Login);
                    cmd1.Parameters.AddWithValue("password", user.Password);
                    cmd1.Parameters.AddWithValue("role", "user");
                    cmd1.Parameters.AddWithValue("username", user.Username);
                    cmd1.Transaction = transaction;
                    var result = await cmd1.ExecuteScalarAsync(cancellationToken) ?? throw new Exception();
                    id = Convert.ToInt32(result);

                    const string sqlQuery2 = @"INSERT INTO collections (owner_id, name, description) VALUES (@owner_id, 'Favorite', 'This is a default likes collection.')";
                    var cmd2 = new NpgsqlCommand(sqlQuery2, _dbConnection);
                    cmd2.Parameters.AddWithValue("owner_id", id);
                    cmd2.Transaction = transaction;
                    await cmd2.ExecuteNonQueryAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    Console.WriteLine(ex.Message);
                    return null;
                }
                
            }

            return new User
            {
                Login = user.Login,
                Password = user.Password,
                Id = id,
                Role = "user",
                Username = user.Username
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public static async Task<User?> CreateUserLegacy(UserLoginModel user, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "INSERT INTO users (login, password, role, username) VALUES (@login, @password, @role, @username) RETURNING id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("login", user.Login);
            cmd.Parameters.AddWithValue("password", user.Password);
            cmd.Parameters.AddWithValue("role", "user");
            cmd.Parameters.AddWithValue("username", user.Username);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            return new User
            {
                Login = user.Login,
                Password = user.Password,
                Id = Convert.ToInt32(result),
                Role = "user",
                Username = user.Username
            };
        }
        catch
        {
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public static async Task<long?> GetUserLikesCollectionId(long ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = @"SELECT id FROM collections WHERE owner_id = @owner_id AND name = 'Favorite' LIMIT 1";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("owner_id", ownerId);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            if (reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
                return reader.GetInt32("id");
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

    public static async Task<Collection?> CreateCollection(CollectionModel collection, long ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "INSERT INTO collections (owner_id, name, description) VALUES (@owner_id, @name, @description) RETURNING id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("owner_id", ownerId);
            cmd.Parameters.AddWithValue("name", collection.Name);
            cmd.Parameters.AddWithValue("description", collection.Description);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            return new Collection
            {
                Id = Convert.ToInt32(result),
                OwnerId = ownerId,
                Name = collection.Name,
                Description = collection.Description,
            };
        }
        catch
        {
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public static async Task<Collection?> GetCollectionById(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "SELECT * FROM collections WHERE id = @id";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("id", id);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            
            if (reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
                return new Collection
                {
                    Id = id,
                    OwnerId = reader.GetInt32("owner_id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
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

    public static async Task<List<long>?> GetAllUserCollectionIds(long ownerId,CancellationToken cancellationToken = default)
    {
        var ids = new List<long>();
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "SELECT id FROM collections WHERE owner_id = @owner_id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("owner_id", ownerId);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                int id = reader.GetInt32(0);
                ids.Add(id);
            }
            foreach (var i in ids)
            {
                Console.WriteLine(i);
            }
            return ids;
        }
        catch
        {
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public static async Task<List<long>?> GetAllPictureIdsFromCollection(long collectionId, CancellationToken cancellationToken = default)
    {
        var ids = new List<long>();
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "SELECT picture_id FROM collection_items WHERE collection_id = @collection_id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("collection_id", collectionId);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                int id = reader.GetInt32(0);
                ids.Add(id);
            }
            return ids;
        }
        catch
        {
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public static async Task<Picture?> CreateUserPicture(UserPictureModel picture, string imageUrl, long ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "INSERT INTO pictures (name, description, image_url, owner_id, content_type) " +
                "VALUES (@name, @description, @image_url, @owner_id, @content_type) RETURNING id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("name", picture.Name);
            cmd.Parameters.AddWithValue("description", picture.Description);
            cmd.Parameters.AddWithValue("image_url", imageUrl);
            cmd.Parameters.AddWithValue("content_type", picture.ContentType);
            cmd.Parameters.AddWithValue("owner_id", ownerId);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            return new Picture
            {
                Name = picture.Name,
                Description = picture.Description,
                ImageUrl = imageUrl,
                OwnerId = ownerId,
                Id = Convert.ToInt32(result),
                ContentType = picture.ContentType
            };
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public static async Task<Picture?> CreateApiPicture(UserPictureModel picture, string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "INSERT INTO pictures (name, description, image_url, owner_id, content_type) " +
                "VALUES (@name, @description, @image_url, @owner_id, @content_type) RETURNING id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("name", picture.Name);
            cmd.Parameters.AddWithValue("description", picture.Description);
            cmd.Parameters.AddWithValue("image_url", imageUrl);
            cmd.Parameters.AddWithValue("content_type", picture.ContentType);
            cmd.Parameters.AddWithValue("owner_id", DBNull.Value);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            return new Picture
            {
                Name = picture.Name,
                Description = picture.Description,
                ImageUrl = imageUrl,
                OwnerId = null,
                Id = Convert.ToInt32(result),
                ContentType = picture.ContentType
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public static async Task<User?> GetUserByLogin(string login, CancellationToken cancellationToken = default)
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

    public static async Task<Picture?> GetPictureById(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "SELECT * FROM pictures WHERE id = @id";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("id", id);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
                return new Picture
                {
                    Id = id,
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    ImageUrl = reader.GetString("image_url"),
                    OwnerId = reader["owner_id"] != DBNull.Value ? reader.GetInt32("owner_id") : null,
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

    public static async Task<User?> GetUserById(long id, CancellationToken cancellationToken = default)
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

    public static async Task<long?> GetPictureIdByApiId(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "SELECT id FROM api_ids WHERE api_id = @api_id";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("api_id", id);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
                return reader.GetInt64("id");
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

    public static async Task<long?> GetApiIdByPictureId(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "SELECT api_id FROM api_ids WHERE id = @id";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("id", id);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
                return reader.GetInt64("api_id");
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

    public static async Task<bool> SetApiIdToPictureId(int apiId, long id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "INSERT INTO api_ids (api_id, id) VALUES (@api_id, @id);";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("api_id", apiId);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
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

    public static async Task<bool> AddPictureToCollection(long pictureId, long collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "INSERT INTO collection_items (picture_id, collection_id) VALUES (@picture_id, @collection_id);";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("picture_id", pictureId);
            cmd.Parameters.AddWithValue("collection_id", collectionId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
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

    public static async Task<bool> RemovePictureFromCollection(long pictureId, long collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);

            const string sqlQuery = "DELETE FROM collection_items WHERE picture_id = @picture_id AND collection_id = @collection_id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("picture_id", pictureId);
            cmd.Parameters.AddWithValue("collection_id", collectionId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
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

    public static async Task Like(long pictureId, long userId, CancellationToken cancellationToken = default)
    {
        var favorite = await GetUserLikesCollectionId(userId, cancellationToken) ?? throw new Exception("No favorite collection");
        if(!await AddPictureToCollection(pictureId, favorite, cancellationToken)) {
            throw new Exception("Sorry, something went wrong");
        }
    }

    public static async Task Disike(long pictureId, long userId, CancellationToken cancellationToken = default)
    {
        var favorite = await GetUserLikesCollectionId(userId, cancellationToken) ?? throw new Exception("No favorite collection");
        if (!await RemovePictureFromCollection(pictureId, favorite, cancellationToken))
        {
            throw new Exception("Sorry, something went wrong");
        }
    }

    public static async Task<List<long>> GetAllUserApiLikes(long userId, CancellationToken cancellationToken = default)
    {
        var favorite = await GetUserLikesCollectionId(userId, cancellationToken) ?? throw new Exception("No favorite collection");
        var likes = await GetAllPictureIdsFromCollection(favorite, cancellationToken) ?? throw new Exception("Something went wrong");
        var result = new List<long>();
        foreach (var like in likes)
        {
            var apiId = await GetApiIdByPictureId(like, cancellationToken);
            if (apiId != null) {
                result.Add(apiId ?? 0);
            }
        }
        return result;
    }

    public static async Task<bool> DeleteAllUsers(CancellationToken cancellationToken = default) {
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

    public static async Task<bool> GetAllUserIds(CancellationToken cancellationToken = default) {
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