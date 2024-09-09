﻿using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using Npgsql;
using System.Text.Json;

namespace CarCareTracker.External.Implementations
{
    public class PGServiceRecordDataAccess: IServiceRecordDataAccess
    {
        private NpgsqlDataSource pgDataSource;
        private readonly ILogger<PGServiceRecordDataAccess> _logger;
        private static string tableName = "servicerecords";
        public PGServiceRecordDataAccess(IConfiguration config, ILogger<PGServiceRecordDataAccess> logger)
        {
            pgDataSource = NpgsqlDataSource.Create(config["POSTGRES_CONNECTION"]);
            _logger = logger;
            try
            {
                //create table if not exist.
                string initCMD = $"CREATE TABLE IF NOT EXISTS app.{tableName} (id INT GENERATED BY DEFAULT AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)";
                using (var ctext = pgDataSource.CreateCommand(initCMD))
                {
                    ctext.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        public List<ServiceRecord> GetServiceRecordsByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE vehicleId = @vehicleId";
                var results = new List<ServiceRecord>();
                using (var ctext = pgDataSource.CreateCommand(cmd))
                {
                    ctext.Parameters.AddWithValue("vehicleId", vehicleId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            ServiceRecord serviceRecord = JsonSerializer.Deserialize<ServiceRecord>(reader["data"] as string);
                            results.Add(serviceRecord);
                        }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<ServiceRecord>();
            }
        }
        public ServiceRecord GetServiceRecordById(int serviceRecordId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE id = @id";
                var result = new ServiceRecord();
                using (var ctext = pgDataSource.CreateCommand(cmd))
                {
                    ctext.Parameters.AddWithValue("id", serviceRecordId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            ServiceRecord serviceRecord = JsonSerializer.Deserialize<ServiceRecord>(reader["data"] as string);
                            result = serviceRecord;
                        }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new ServiceRecord();
            }
        }
        public bool DeleteServiceRecordById(int serviceRecordId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE id = @id";
                using (var ctext = pgDataSource.CreateCommand(cmd))
                {
                    ctext.Parameters.AddWithValue("id", serviceRecordId);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool SaveServiceRecordToVehicle(ServiceRecord serviceRecord)
        {
            try
            {
                if (serviceRecord.Id == default)
                {
                    string cmd = $"INSERT INTO app.{tableName} (vehicleId, data) VALUES(@vehicleId, CAST(@data AS jsonb)) RETURNING id";
                    using (var ctext = pgDataSource.CreateCommand(cmd))
                    {
                        ctext.Parameters.AddWithValue("vehicleId", serviceRecord.VehicleId);
                        ctext.Parameters.AddWithValue("data", "{}");
                        serviceRecord.Id = Convert.ToInt32(ctext.ExecuteScalar());
                        //update json data
                        if (serviceRecord.Id != default)
                        {
                            string cmdU = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                            using (var ctextU = pgDataSource.CreateCommand(cmdU))
                            {
                                var serializedData = JsonSerializer.Serialize(serviceRecord);
                                ctextU.Parameters.AddWithValue("id", serviceRecord.Id);
                                ctextU.Parameters.AddWithValue("data", serializedData);
                                return ctextU.ExecuteNonQuery() > 0;
                            }
                        }
                        return serviceRecord.Id != default;
                    }
                }
                else
                {
                    string cmd = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                    using (var ctext = pgDataSource.CreateCommand(cmd))
                    {
                        var serializedData = JsonSerializer.Serialize(serviceRecord);
                        ctext.Parameters.AddWithValue("id", serviceRecord.Id);
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
        public bool DeleteAllServiceRecordsByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE vehicleId = @id";
                using (var ctext = pgDataSource.CreateCommand(cmd))
                {
                    ctext.Parameters.AddWithValue("id", vehicleId);
                    ctext.ExecuteNonQuery();
                    return true;
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
