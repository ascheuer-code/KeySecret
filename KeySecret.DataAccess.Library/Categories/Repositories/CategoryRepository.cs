﻿using KeySecret.DataAccess.Library.Categories.Models;
using KeySecret.DataAccess.Library.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace KeySecret.DataAccess.Library.Categories.Repositories
{
    public class CategoryRepository : IRepository<CategoryModel>
    {
        private readonly string _connectionString;
        private readonly ILogger<CategoryRepository> _logger;

        private const string _spGetOne = "KeySecretDB.dbo.spCategories_GetOneItem";
        private const string _spGetAll = "KeySecretDB.dbo.spCategories_GetAllItems";
        private const string _spInsert = "KeySecretDB.dbo.spCategories_InsertItem";
        private const string _spUpdate = "KeySecretDB.dbo.spCategories_UpdateItem";
        private const string _spDelete = "KeySecretDB.dbo.spCategories_DeleteItem";

        public CategoryRepository(string connectionString, ILogger<CategoryRepository> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<CategoryModel> GetItemAsync(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                CategoryModel model = null;
                SqlTransaction transaction = connection.BeginTransaction(nameof(GetItemAsync));

                SqlCommand command = new SqlCommand(_spGetOne, connection, transaction);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int)).Value = id;

                try
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            model = new CategoryModel()
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString()
                            };
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    throw await TransactionRollbackAsync(transaction, ex, nameof(GetItemAsync));
                }

                return model;
            }
        }

        public async Task<IEnumerable<CategoryModel>> GetItemsAsync()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var list = new List<CategoryModel>();
                SqlTransaction transaction = connection.BeginTransaction(nameof(GetItemsAsync));

                SqlCommand command = new SqlCommand(_spGetAll, connection, transaction);
                command.CommandType = CommandType.StoredProcedure;

                try
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CategoryModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString()
                            });
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    throw await TransactionRollbackAsync(transaction, ex, nameof(GetItemsAsync));
                }

                return list;
            }
        }

        public async Task<CategoryModel> InsertItemAsync(CategoryModel item)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlTransaction transaction = connection.BeginTransaction(nameof(InsertItemAsync));

                SqlCommand command = new SqlCommand(_spInsert, connection, transaction);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar)).Value = item.Name;

                try
                {
                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    transaction = connection.BeginTransaction("GetIdentifier");
                    command = new SqlCommand("select @@IDENTITY", connection, transaction);

                    item.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
                }
                catch (Exception ex)
                {
                    throw await TransactionRollbackAsync(transaction, ex, nameof(InsertItemAsync));
                }

                return item;
            }
        }

        public async Task UpdateItemAsync(CategoryModel item)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlTransaction transaction = connection.BeginTransaction(nameof(UpdateItemAsync));
                SqlCommand command = new SqlCommand(_spUpdate, connection, transaction);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@Id", SqlDbType.NVarChar)).Value = item.Id;
                command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar)).Value = item.Name == null ? DBNull.Value : item.Name;

                try
                {
                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    throw await TransactionRollbackAsync(transaction, ex, nameof(UpdateItemAsync));
                }
            }
        }

        public async Task DeleteItemAsync(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlTransaction transaction = connection.BeginTransaction(nameof(DeleteItemAsync));
                SqlCommand command = new SqlCommand(_spDelete, connection, transaction);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@Id", SqlDbType.NVarChar)).Value = id;

                try
                {
                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    throw await TransactionRollbackAsync(transaction, ex, nameof(DeleteItemAsync));
                }
            }
        }


        /// <summary>
        /// Tries a Rollback. If it fails, the method will return a Exception
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ex"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private async Task<Exception> TransactionRollbackAsync(SqlTransaction transaction, Exception ex, string origin)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception ex2)
            {
                _logger.LogError($"Fehler beim abrufen in {origin}, Rollback fehlgeschlagen.");

                return new Exception(
                    "\r\n\r\n" +
                    $"Caller:\t{origin}\r\n" +
                    $"Message:\t{ex2.Message}\r\n\r\n" +
                    ex2.StackTrace
                );
            }

            _logger.LogError($"Fehler beim abrufen in {origin}, Rollback erfolgreich.");

            return new Exception(
                    "\r\n\r\n" +
                    $"Caller:\t{origin}\r\n" +
                    $"Message:\t{ex.Message}\r\n\r\n" +
                    ex.StackTrace
                );
        }
    }
}