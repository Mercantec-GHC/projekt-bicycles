using Blazor.Models;
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
            using var conn = new NpgsqlConnection(_connectionString); //using var ensures the connection is automatically closed and disposed when done
            conn.Open();

            var sql = new StringBuilder("SELECT * FROM bikes WHERE 1=1");// 1=1 is a common trick to simplify appending AND conditions
            var cmd = new NpgsqlCommand { Connection = conn };

            if (!string.IsNullOrEmpty(brand))
            {
                sql.Append(" AND brand ILIKE @brand");
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
                    ModelYear = reader["model_year"] != DBNull.Value ? (int)reader["model_year"] : 0,
                    CreatedAt = (DateTime)reader["created_at"]
                });
            }

            return bikes;
        }
    }
}

