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
                    ImageUrl = reader["image_url"]?.ToString() ?? "", // <-- добавили считывание URL
                    CreatedAt = (DateTime)reader["created_at"]
                });
            }

            return bikes;
        }

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
                    ImageUrl = reader["image_url"]?.ToString() ?? "", // <-- добавили считывание URL
                    CreatedAt = (DateTime)reader["created_at"]
                });
            }

            return bikes;
        }
    }
}
