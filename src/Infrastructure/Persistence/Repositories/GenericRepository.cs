using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBoilerplate.Application.Common.Abstractions;

namespace EnterpriseBoilerplate.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<T> : IRepository<T> where T : class
    {
        protected readonly WriteDbContext _db;
        protected readonly DbSet<T> _set;
        public GenericRepository(WriteDbContext db) { _db = db; _set = db.Set<T>(); }

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => await _set.FindAsync([id], ct);
        public async Task AddAsync(T entity, CancellationToken ct = default) => await _set.AddAsync(entity, ct);
        public void Update(T entity) => _set.Update(entity);
        public void Remove(T entity) => _set.Remove(entity);
    }
}
