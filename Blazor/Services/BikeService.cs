using Blazor.Models;                      // Contains Bike model class
using Microsoft.AspNetCore.Components.Forms; // For IBrowserFile
using Npgsql;                             // PostgreSQL data provider
using System.Text;                        // For building dynamic SQL strings
namespace Blazor.Services
{
    // BikeService handles all database operations related to bikes

    public class BikeService(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        // ------------------------------------------------------------
        // 2. Get a single bike by its ID (for detailed view page)
        // ------------------------------------------------------------
        public async Task<Bike?> GetBikeById(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
            SELECT b.*, u.id AS user_id, u.name AS user_name, u.email, u.phone, u.created_at AS user_created_at FROM bikes b  JOIN users u ON u.id = b.user_id WHERE b.id = @id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Bike
                {
                    Id = (int)reader["id"],
                    Title = reader["title"].ToString(),
                    Price = (decimal)reader["price"],
                    UserId = (int)reader["user_id"],
                    Color = reader["color"].ToString(),
                    Type = reader["type"].ToString(),
                    BikeCondition = reader["bike_condition"].ToString(),
                    Brand = reader["brand"].ToString(),
                    Location = reader["location"].ToString(),
                    GearType = "Unknown",
                    ImageUrl = reader["image_url"]?.ToString() ?? "",
                    CreatedAt = (DateTime)reader["created_at"],
                    User = new User
                    {
                        ID = (int)reader["user_id"],
                        Name = reader["user_name"].ToString(),
                    }
                };
            }
            return null;
        }
        // ------------------------------------------------------------
        // 3. Get newest bikes (for homepage, sliders, carousel, etc.)
        // ------------------------------------------------------------
        public List<Bike> GetNewestBikes(int count = 8)
        {
            var bikes = new List<Bike>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = $@"
            SELECT b.*, u.id AS user_id, u.name AS user_name FROM bikes b LEFT JOIN users u ON u.id = b.user_id ORDER BY b.created_at DESC LIMIT {count}";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                bikes.Add(new Bike
                {
                    Id = (int)reader["id"],
                    Title = reader["title"].ToString(),
                    Price = (decimal)reader["price"],
                    UserId = (int)reader["user_id"],
                    Color = reader["color"].ToString(),
                    Type = reader["type"].ToString(),
                    BikeCondition = reader["bike_condition"].ToString(),
                    Brand = reader["brand"].ToString(),
                    Location = reader["location"].ToString(),
                    GearType = reader["gear_type"]?.ToString() ?? "Unknown",
                    BreakType = reader["break_type"]?.ToString(),
                    Weight = reader["weight"] != DBNull.Value ? (decimal)reader["weight"] : 0,
                    ModelYear = reader["model_year"] != DBNull.Value ? (int)reader["model_year"] : 0,
                    ImageUrl = reader["image_url"]?.ToString() ?? "",
                    CreatedAt = (DateTime)reader["created_at"],
                    User = new User
                    {
                        ID = (int)reader["user_id"],
                        Name = reader["user_name"]?.ToString() ?? "Unknown"
                    }
                });
            }
            return bikes;
        }
        // ------------------------------------------------------------
        // 4. Get bikes using advanced filters (brand, color, etc.)
        //    Used in the filtering/search page
        // ------------------------------------------------------------
        public List<Bike> GetBikes(
        string? brand = null,
        string? type = null,
        string? color = null,
        string? locationFilter = null,
        decimal? maxPrice = null,
        int? modelYear = null,
        string? condition = null)
        {
            var bikes = new List<Bike>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var sql = new StringBuilder(@"
        SELECT b.*, u.name AS user_name
        FROM bikes b
        LEFT JOIN users u ON u.id = b.user_id
        WHERE 1=1
    ");
            var cmd = new NpgsqlCommand { Connection = conn };
            if (!string.IsNullOrEmpty(brand))
            {
                sql.Append(" AND b.brand ILIKE @brand");
                cmd.Parameters.AddWithValue("brand", $"%{brand}%");
            }
            if (!string.IsNullOrEmpty(type))
            {
                sql.Append(" AND b.type ILIKE @type");
                cmd.Parameters.AddWithValue("type", type);
            }
            if (!string.IsNullOrEmpty(color))
            {
                sql.Append(" AND b.color ILIKE @color");
                cmd.Parameters.AddWithValue("color", $"%{color}%");
            }
            if (!string.IsNullOrEmpty(locationFilter))
            {
                sql.Append(" AND b.location ILIKE @location");
                cmd.Parameters.AddWithValue("location", $"%{locationFilter}%");
            }
            if (maxPrice.HasValue)
            {
                sql.Append(" AND b.price <= @maxPrice");
                cmd.Parameters.AddWithValue("maxPrice", maxPrice.Value);
            }
            if (modelYear.HasValue)
            {
                sql.Append(" AND b.model_year = @modelYear");
                cmd.Parameters.AddWithValue("modelYear", modelYear.Value);
            }
            if (!string.IsNullOrEmpty(condition))
            {
                sql.Append(" AND b.bike_condition = @condition");
                cmd.Parameters.AddWithValue("condition", condition);
            }
                sql.Append(" ORDER BY b.created_at DESC LIMIT 50");
                cmd.CommandText = sql.ToString();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                bikes.Add(new Bike
                {
                    Id = (int)reader["id"],
                    Title = reader["title"].ToString(),
                    Price = (decimal)reader["price"],
                    UserId = (int)reader["user_id"],
                    Brand = reader["brand"].ToString(),
                    Type = reader["type"].ToString(),
                    BikeCondition = reader["bike_condition"].ToString(),
                    Location = reader["location"].ToString(),
                    ImageUrl = reader["image_url"]?.ToString() ?? "",
                    CreatedAt = (DateTime)reader["created_at"],
                    User = new User
                    {
                        ID = (int)reader["user_id"],
                        Name = reader["user_name"].ToString()
                    }
                });
            }
            return bikes;
        }
        // ------------------------------------------------------------
        // 5. Create a new bike advertisement, optionally with image upload
        // ------------------------------------------------------------
        public async Task CreateAdAsync(Bike bike, IBrowserFile? imageFile = null)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            string? imageUrl = null;
            // Handle image upload if provided

            if (imageFile != null)
            {
                var uploads = Path.Combine("wwwroot/uploads");
                // Create upload directory if missing

                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                // Extract file name and full path
                var fileName = Path.GetFileName(imageFile.Name);
                var filePath = Path.Combine(uploads, fileName);

                // Save file to disk
                await using var stream = File.Create(filePath);
                await imageFile.OpenReadStream().CopyToAsync(stream);

                // Public URL for the image
                imageUrl = $"/uploads/{fileName}";
            }

            // SQL command for creating a new bike record
            var sql = @"
                INSERT INTO bikes 
                (user_id, title, price, color, type, model_year, gear_type, break_type, weight, bike_condition, 
                 target_audience, material, brand, location, description, image_url, created_at)
                VALUES 
                (@userId, @title, @price, @color, @type, @modelYear, @gearType, @breakType, @weight, @bikeCondition, 
                 @targetAudience, @material, @brand, @location, @description, @imageUrl, @createdAt);
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);

            // Add parameters safely (avoid SQL injection, handle nulls)

            cmd.Parameters.AddWithValue("userId", bike.UserId);
            cmd.Parameters.AddWithValue("title", bike.Title ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("price", bike.Price);
            cmd.Parameters.AddWithValue("color", bike.Color ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("type", bike.Type ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("modelYear", bike.ModelYear);
            cmd.Parameters.AddWithValue("gearType", bike.GearType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("breakType", bike.BreakType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("weight", bike.Weight);
            cmd.Parameters.AddWithValue("bikeCondition", bike.BikeCondition ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("targetAudience", bike.TargetAudience ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("material", bike.Material ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("brand", bike.Brand ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("location", bike.Location ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("description", bike.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("imageUrl", imageUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("createdAt", DateTime.Now);

            // Execute the insert command asynchronously
            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<bool> CheckUserExistsAsync(int userId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = "SELECT 1 FROM users WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("id", userId);
            var result = await cmd.ExecuteScalarAsync();

            return result != null;
        }
        public List<string> GetDistinctTypes() // New method to get distinct bike types
        {
            var types = new List<string>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT DISTINCT type FROM bikes WHERE type IS NOT NULL AND type <> '' ORDER BY type ASC",
                conn); // SQL to get distinct non-empty types

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var t = reader["type"]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(t))
                    types.Add(t);
            }
            return types;
        }
        public async Task<List<string>> GetDistinctTypesAsync() // New method to get distinct bike types
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT DISTINCT type FROM bikes WHERE type IS NOT NULL AND type <> '' ORDER BY type ASC",
                conn); // SQL to get distinct non-empty types

            var types = new List<string>();
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var type = reader["type"]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(type))
                    types.Add(type);
            }
            return types;
        }
    }
}

