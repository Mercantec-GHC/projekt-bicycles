using Blazor.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace Blazor.Services
{
    public class BikeService
    {
        private readonly string _connectionString;

        public BikeService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Bike> GetNewestBikes(int count = 10)
        {
            var bikes = new List<Bike>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string sql = $"SELECT * FROM bikes ORDER BY created_at DESC LIMIT {count};";

            using var command = new NpgsqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

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
                    ImageUrl = reader["image_url"]?.ToString() ?? "",
                    CreatedAt = (DateTime)reader["created_at"]
                });
            }

            return bikes;
        }
    }
}
