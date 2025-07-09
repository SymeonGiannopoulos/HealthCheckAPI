using HealthCheckAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

namespace HealthCheckAPI.Services
{
    public class UserService
    {
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(IConfiguration configuration, IPasswordHasher<User> passwordHasher)
        {
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        public User GetUserByUsername(string username)
        {
            using var connection = new SqliteConnection(_configuration.GetConnectionString("SqliteConnection"));
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Username, Password, Email, Role FROM Users WHERE Username = @username";
            command.Parameters.AddWithValue("@username", username);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Password = reader.GetString(2),
                    Email = reader.GetString(3),
                };
            }
            return null;
        }

        public bool VerifyPassword(User user, string password)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            return result == PasswordVerificationResult.Success;
        }

        public void CreateUser(User user, string password)
        {
            // Κάνουμε hash το password
            var hashedPassword = _passwordHasher.HashPassword(user, password);

            using var connection = new SqliteConnection(_configuration.GetConnectionString("SqliteConnection"));
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
        INSERT INTO Users (Username, Password, Email, Role)
        VALUES (@username, @password, @email, @role);
    ";
            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@password", hashedPassword);
            command.Parameters.AddWithValue("@email", user.Email ?? "");

            command.ExecuteNonQuery();
        }
    }

    


}
