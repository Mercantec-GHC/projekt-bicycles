using Blazor.Models;
using Microsoft.AspNetCore.Components.Forms;
using Npgsql;
using System.Text;

namespace Blazor.Services
{
    public class BikeService(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        // 1. Get first 20 bikes (for general listing page)
        public List<Bike> GetAllBikes()
        {
            var bikes = new List<Bike>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM bikes ORDER BY id LIMIT 20;", conn);
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
                    GearType = "Unknown",
                    ImageUrl = reader["image_url"]?.ToString() ?? "",
                    CreatedAt = (DateTime)reader["created_at"]
                });
            }

            return bikes;
        }

        // 2. Get newest bikes (for homepage / carousel etc.)
        public List<Bike> GetNewestBikes(int count = 8)
        {
            var bikes = new List<Bike>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = $"SELECT * FROM bikes ORDER BY created_at DESC LIMIT {count};";
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
                    GearType = "Unknown",
                    ImageUrl = reader["image_url"]?.ToString() ?? "",
                    CreatedAt = (DateTime)reader["created_at"]
                });
            }

            return bikes;
        }

        // 3. Get bikes with filters (for filter page)
        public List<Bike> GetBikes( //method that returns a list of Bike objects.
            string? brand = null, //It accepts optional filters (nullable parameters). If a parameter is not provided, the filter is ignored.
            string? type = null,
            string? color = null,
            string? locationFilter = null,
            decimal? maxPrice = null,
            int? modelYear = null,
            string? condition = null)

        {
            var bikes = new List<Bike>(); //Creates an empty list where all fetched bikes will be stored.
            using var conn = new NpgsqlConnection(_connectionString); //using var ensures the connection is automatically closed and disposed when done
            conn.Open();

            var sql = new StringBuilder("SELECT * FROM bikes WHERE 1=1");// 1=1 is a common trick to simplify appending AND conditions
            var cmd = new NpgsqlCommand { Connection = conn };

            if (!string.IsNullOrEmpty(brand)) //Each filter checks whether a parameter has value before adding SQL conditions.
            {
                sql.Append(" AND brand ILIKE @brand"); //Adds a case-insensitive filter (ILIKE) to SQL.
                cmd.Parameters.AddWithValue("brand", $"%{brand}%");
            }

            if (!string.IsNullOrEmpty(type))
            {
                sql.Append(" AND type ILIKE @type");
                cmd.Parameters.AddWithValue("type", type);
            }
            if (!string.IsNullOrEmpty(color))
            {
                sql.Append(" AND color ILIKE @color");
                cmd.Parameters.AddWithValue("color", $"%{color}%");
            }

            if (!string.IsNullOrEmpty(locationFilter))
            {
                sql.Append(" AND location ILIKE @location");
                cmd.Parameters.AddWithValue("location", $"%{locationFilter}%");
            }


            if (maxPrice.HasValue)
            {
                sql.Append(" AND price <= @maxPrice");
                cmd.Parameters.AddWithValue("maxPrice", maxPrice.Value);
            }

            if (modelYear.HasValue)
            {
                sql.Append(" AND model_year = @modelYear");
                cmd.Parameters.AddWithValue("modelYear", modelYear.Value);
            }

            if (!string.IsNullOrEmpty(condition))
            {
                sql.Append(" AND bike_condition = @condition");
                cmd.Parameters.AddWithValue("condition", condition);
            }

            sql.Append(" ORDER BY created_at DESC LIMIT 50");
            cmd.CommandText = sql.ToString();

            using var reader = cmd.ExecuteReader(); //fetch data from the database.
            while (reader.Read())
            {
                bikes.Add(new Bike
                {
                    Id = (int)reader["id"],  //Reads fields directly from the database row.
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
                    ModelYear = reader["model_year"] != DBNull.Value ? (int)reader["model_year"] : 0,
                    CreatedAt = (DateTime)reader["created_at"]
                });
            }

            return bikes;
        }
    
            public async Task CreateAdAsync(Bike bike, IBrowserFile? imageFile = null)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            string? imageUrl = null;
            if (imageFile != null)
            {
                var uploads = Path.Combine("wwwroot/uploads");
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                var fileName = Path.GetFileName(imageFile.Name);
                var filePath = Path.Combine(uploads, fileName);

                await using var stream = File.Create(filePath);
                await imageFile.OpenReadStream().CopyToAsync(stream);

                imageUrl = $"/uploads/{fileName}";
            }

            var sql = @"
                INSERT INTO bikes 
                (user_id, title, price, color, type, model_year, gear_type, break_type, weight, bike_condition, target_audience, material, brand, location, description, image_url, created_at)
                VALUES 
                (@userId, @title, @price, @color, @type, @modelYear, @gearType, @breakType, @weight, @bikeCondition, @targetAudience, @material, @brand, @location, @description, @imageUrl, @createdAt);
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
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

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
