﻿using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using Npgsql;
using System.Text.Json;

namespace CarCareTracker.External.Implementations
{
    public class PGReminderRecordDataAccess : IReminderRecordDataAccess
    {
        private NpgsqlConnection pgDataSource;
        private readonly ILogger<PGReminderRecordDataAccess> _logger;
        private static string tableName = "reminderrecords";
        public PGReminderRecordDataAccess(IConfiguration config, ILogger<PGReminderRecordDataAccess> logger)
        {
            pgDataSource = new NpgsqlConnection(config["POSTGRES_CONNECTION"]);
            _logger = logger;
            try
            {
                pgDataSource.Open();
                //create table if not exist.
                string initCMD = $"CREATE TABLE IF NOT EXISTS app.{tableName} (id INT GENERATED ALWAYS AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)";
                using (var ctext = new NpgsqlCommand(initCMD, pgDataSource))
                {
                    ctext.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        public List<ReminderRecord> GetReminderRecordsByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE vehicleId = @vehicleId";
                var results = new List<ReminderRecord>();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("vehicleId", vehicleId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            ReminderRecord reminderRecord = JsonSerializer.Deserialize<ReminderRecord>(reader["data"] as string);
                            results.Add(reminderRecord);
                        }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<ReminderRecord>();
            }
        }
        public ReminderRecord GetReminderRecordById(int reminderRecordId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE id = @id";
                var result = new ReminderRecord();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", reminderRecordId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            ReminderRecord reminderRecord = JsonSerializer.Deserialize<ReminderRecord>(reader["data"] as string);
                            result = reminderRecord;
                        }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new ReminderRecord();
            }
        }
        public bool DeleteReminderRecordById(int reminderRecordId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE id = @id";
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", reminderRecordId);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool SaveReminderRecordToVehicle(ReminderRecord reminderRecord)
        {
            try
            {
                if (reminderRecord.Id == default)
                {
                    string cmd = $"INSERT INTO app.{tableName} (vehicleId, data) VALUES(@vehicleId, CAST(@data AS jsonb)) RETURNING id";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("vehicleId", reminderRecord.VehicleId);
                        ctext.Parameters.AddWithValue("data", "{}");
                        reminderRecord.Id = Convert.ToInt32(ctext.ExecuteScalar());
                        //update json data
                        if (reminderRecord.Id != default)
                        {
                            string cmdU = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                            using (var ctextU = new NpgsqlCommand(cmdU, pgDataSource))
                            {
                                var serializedData = JsonSerializer.Serialize(reminderRecord);
                                ctextU.Parameters.AddWithValue("id", reminderRecord.Id);
                                ctextU.Parameters.AddWithValue("data", serializedData);
                                return ctextU.ExecuteNonQuery() > 0;
                            }
                        }
                        return reminderRecord.Id != default;
                    }
                }
                else
                {
                    string cmd = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        var serializedData = JsonSerializer.Serialize(reminderRecord);
                        ctext.Parameters.AddWithValue("id", reminderRecord.Id);
                        ctext.Parameters.AddWithValue("data", serializedData);
                        return ctext.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool DeleteAllReminderRecordsByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE vehicleId = @id";
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", vehicleId);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}
