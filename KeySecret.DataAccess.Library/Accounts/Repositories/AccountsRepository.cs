﻿using KeySecret.DataAccess.Library.Accounts.Models;
using KeySecret.DataAccess.Library.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace KeySecret.DataAccess.Library.Accounts.Repositories
{
    public class AccountsRepository : IRepository<AccountModel>
    {
        private readonly string _connectionString;
        private readonly ILogger<AccountsRepository> _logger;

        private const string _spGetOne = "KeySecretDB.dbo.spAccounts_GetOneItem";
        private const string _spGetAll = "KeySecretDB.dbo.spAccounts_GetAllItems";
        private const string _spInsert = "KeySecretDB.dbo.spAccounts_InsertItem";
        private const string _spUpdate = "KeySecretDB.dbo.spAccounts_UpdateItem";
        private const string _spDelete = "KeySecretDB.dbo.spAccounts_DeleteItem";

        public AccountsRepository(string connectionString, ILogger<AccountsRepository> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Requests a single account from the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<AccountModel> GetItemAsync(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                AccountModel model = null;
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
                            model = new AccountModel()
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                WebAdress = reader["WebAdress"].ToString(),
                                Password = reader["Password"].ToString(),
                                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
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

        /// <summary>
        /// Requests all accounts from the database
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AccountModel>> GetItemsAsync()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var list = new List<AccountModel>();
                SqlTransaction transaction = connection.BeginTransaction(nameof(GetItemsAsync));

                SqlCommand command = new SqlCommand(_spGetAll, connection, transaction);
                command.CommandType = CommandType.StoredProcedure;

                try
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AccountModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                WebAdress = reader["WebAdress"].ToString(),
                                Password = reader["Password"].ToString(),
                                CategoryId = reader["CategoryId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CategoryId"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
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

        /// <summary>
        /// Insert a account to the database
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<AccountModel> InsertItemAsync(AccountModel item)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlTransaction transaction = connection.BeginTransaction(nameof(InsertItemAsync));

                SqlCommand command = new SqlCommand(_spInsert, connection, transaction);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar)).Value = item.Name;
                command.Parameters.Add(new SqlParameter("@WebAdress", SqlDbType.NVarChar)).Value = item.WebAdress;
                command.Parameters.Add(new SqlParameter("@Password", SqlDbType.NVarChar)).Value = item.Password;
                command.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int)).Value = item.CategoryId == 0 ? DBNull.Value : item.CategoryId;

                try
                {
                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    transaction = connection.BeginTransaction("GetIdentifier");
                    command = new SqlCommand("select @@IDENTITY", connection, transaction);

                    item.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
                    item.CreatedDate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    throw await TransactionRollbackAsync(transaction, ex, nameof(InsertItemAsync));
                }

                return item;
            }
        }

        /// <summary>
        /// Updates a account in the database
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task UpdateItemAsync(AccountModel item)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlTransaction transaction = connection.BeginTransaction(nameof(UpdateItemAsync));
                SqlCommand command = new SqlCommand(_spUpdate, connection, transaction);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@Id", SqlDbType.NVarChar)).Value = item.Id;
                command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar)).Value = item.Name == null ? DBNull.Value : item.Name;
                command.Parameters.Add(new SqlParameter("@WebAdress", SqlDbType.NVarChar)).Value = item.WebAdress == null ? DBNull.Value : item.WebAdress;
                command.Parameters.Add(new SqlParameter("@Password", SqlDbType.NVarChar)).Value = item.Password == null ? DBNull.Value : item.Password;
                command.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int)).Value = item.CategoryId == 0 ? DBNull.Value : item.CategoryId;

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

        /// <summary>
        /// Deletes a account in the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeleteItemAsync(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string sql = "DELETE FROM KeySecretDB.dbo.Accounts WHERE Id=@Id";
                SqlTransaction transaction = connection.BeginTransaction(nameof(DeleteItemAsync));
                SqlCommand command = new SqlCommand(sql, connection, transaction);
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
                return new Exception(
                    "\r\n\r\n" +
                    $"Caller:\t{origin}\r\n" +
                    $"Message:\t{ex2.Message}\r\n\r\n" +
                    ex2.StackTrace
                );
            }

            return new Exception(
                    "\r\n\r\n" +
                    $"Caller:\t{origin}\r\n" +
                    $"Message:\t{ex.Message}\r\n\r\n" +
                    ex.StackTrace
                );
        }
    }
}