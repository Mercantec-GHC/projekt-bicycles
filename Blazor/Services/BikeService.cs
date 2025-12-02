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

        public List<Bike> GetNewestBikes(int count = 8)
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
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Price = reader.GetDecimal(reader.GetOrdinal("price")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    Color = reader.GetString(reader.GetOrdinal("color")),
                    Type = reader.GetString(reader.GetOrdinal("type")),
                    BikeCondition = reader.GetString(reader.GetOrdinal("bike_condition")),
                    Brand = reader.GetString(reader.GetOrdinal("brand")),
                    Location = reader.GetString(reader.GetOrdinal("location")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url"))
                                ? ""
                                : reader.GetString(reader.GetOrdinal("image_url")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            return bikes;
        }
    }
}
