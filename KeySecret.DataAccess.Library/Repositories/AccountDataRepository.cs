﻿using KeySecret.DataAccess.Library.Data;
using KeySecret.DataAccess.Library.Models;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeySecret.DataAccess.Library.Repositories
{
    /// <summary>
    /// Connection class between API endpoint and database for <see cref="AccountModel"/>
    /// </summary>
    public class AccountDataRepository : IRepository<AccountModel>
    {
        private readonly DataDbContext _context;

        public AccountDataRepository(DataDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get item by id
        /// </summary>
        /// <param name="id">Id as <seealso cref="Guid"/></param>
        /// <returns><see cref="AccountModel"/>, if found</returns>
        /// <exception cref="Exception">If the database returns no item a <seealso cref="Exception"/> is thrown</exception>
        public async Task<AccountModel> GetItemAsync(Guid id)
        {
            var account = await _context.Accounts
                .Include(c => c.Category)
                .FirstOrDefaultAsync(a => a.AccountId == id);

            if (account == null)
                throw new Exception();

            return account;
        }

        /// <summary>
        /// Get every item from the database
        /// </summary>
        /// <returns><see cref="AccountModel"/></returns>
        /// <exception cref="Exception">If the database returns no items a <seealso cref="Exception"/> is thrown</exception>
        public async Task<IEnumerable<AccountModel>> GetItemsAsync()
        {
            var accountsList = await _context.Accounts
                .Include(c => c.Category)
                .ToListAsync();

            if (accountsList == null)
                throw new Exception();

            return accountsList;
        }

        /// <summary>
        /// Insert a new item to the database
        /// </summary>
        /// <param name="item"><see cref="AccountModel"/></param>
        /// <returns>The created <see cref="AccountModel"/></returns>
        /// <exception cref="DbUpdateException"></exception>
        public async Task<AccountModel> InsertItemAsync(AccountModel item)
        {
            item.ModifiedDate = DateTime.Now;
            item.AccountId = item.AccountId == Guid.Empty
                            ? Guid.NewGuid()
                            : item.AccountId;

            await _context.Accounts.AddAsync(item);

            if (item.Category != null)
                _context.Entry(item.Category).State = EntityState.Detached;

            var result = await _context.SaveChangesAsync();
            if (result == 0)
                throw new DbUpdateException();

            return item;
        }

        /// <summary>
        /// Updates a item on the Database
        /// </summary>
        /// <param name="item"><see cref="AccountModel"/></param>
        /// <exception cref="Exception"></exception>
        public void UpdateItemAsync(AccountModel item)
        {
            if (!CategoryIDAndModelAreSame(item))
                throw new Exception("The given Category/CategoryId are not the same.");

            var account = _context.Accounts
                .Include(a => a.Category)
                .FirstOrDefault(a => a.AccountId == item.AccountId);

            if (account == null || account.AccountId != item.AccountId)
                throw new Exception("Update failed. Account is null or ID's are different.");

            UpdateModelValues(item, account);

            var result = _context.SaveChanges();
        }

        /// <summary>
        /// Deletes a item on the database
        /// </summary>
        /// <param name="id">Id as <seealso cref="Guid"/></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DbUpdateException"></exception>
        public void DeleteItemAsync(Guid id)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.AccountId == id);
            if (account == null)
                throw new Exception();

            _context.Accounts.Remove(account);

            var result = _context.SaveChanges();
            if (result == 0)
                throw new DbUpdateException();
        }

        /// <summary>
        /// Update the values from the given request model and the db model if they are different
        /// </summary>
        /// <param name="item">Request model</param>
        /// <param name="account">Database model</param>
        private void UpdateModelValues(AccountModel item, AccountModel account)
        {
            bool needsUpdate = false;

            if (account.Name != item.Name)
            {
                account.Name = item.Name;
                needsUpdate = true;
            }

            if (account.WebAddress != item.WebAddress)
            {
                account.WebAddress = item.WebAddress;
                needsUpdate = true;
            }

            if (account.Password != item.Password)
            {
                account.Password = item.Password;
                needsUpdate = true;
            }

            if (account.CategoryId != item.CategoryId)
            {
                account.CategoryId = item.CategoryId;
                needsUpdate = true;
            }

            if (needsUpdate)
                account.ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Check if both fields, Category-Object and CategoryId, are the same and all required fields set
        /// </summary>
        /// <param name="item">The incoming update object</param>
        private bool CategoryIDAndModelAreSame(AccountModel item)
        {
            if (item.Category != null &&
                item.CategoryId != null &&
                item.CategoryId != item.Category.CategoryId)
            {
                return false;
            }

            if (item.Category != null &&
                item.CategoryId == null)
            {
                item.CategoryId = item.Category.CategoryId;
            }

            if (item.Category == null &&
                item.CategoryId != null)
            {
                item.Category = _context.Categories.FirstOrDefault(c => c.CategoryId == item.CategoryId);
            }

            return true;
        }
    }
}