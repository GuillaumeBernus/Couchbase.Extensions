﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Linq;
using Couchbase.Linq.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Couchbase.Extensions.Identity
{
    public class UserStore : UserStore<IdentityUser>
    {
        public UserStore(ICouchbaseIdentityBucketProvider provider) : base(provider)
        {
        }
    }


    public class UserStore<TUser> : IUserPasswordStore<TUser> where TUser : IdentityUser
    {
        private IBucketContext _context;

        public UserStore(ICouchbaseIdentityBucketProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            _context = new BucketContext(provider.GetBucket());
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.NormalizedUserName);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.Id == null)
            {
                user.Id = user.NormalizedUserName;
            }
            var result = await _context.Bucket.InsertAsync(user.Id, user).ConfigureAwait(false);
            if (result.Success)
            {
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(GetIdentityFailure(result));
        }

        public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var result = await _context.Bucket.ReplaceAsync(user.Id, user).ConfigureAwait(false);
            if (result.Success)
            {
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(GetIdentityFailure(result));
        }

        public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var result = await _context.Bucket.RemoveAsync(user.Id).ConfigureAwait(false);
            if (result.Success)
            {
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(GetIdentityFailure(result));
        }

        public async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var result = await _context.Bucket.GetAsync<TUser>(userId).ConfigureAwait(false);
            return result.Success ? result.Value : null;
        }

        public async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = await (from u in _context.Query<TUser>()
                where u.NormalizedUserName == normalizedUserName
                select u).Take(1).ExecuteAsync(cancellationToken).ConfigureAwait(false);

            return query.SingleOrDefault();
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PasswordHash != null);
        }

        private IdentityError GetIdentityFailure(IOperationResult operationResult)
        {
            return new IdentityError
            {
                Code = operationResult.Status.ToString(),
                Description = operationResult.Exception?.ToString()
            };
        }
    }
}
