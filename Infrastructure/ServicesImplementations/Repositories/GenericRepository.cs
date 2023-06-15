using Core.Enums;
using Core.Interfaces;
using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ServicesImplementations.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly Func<CacheTech, ICacheService> _cacheService;
        private readonly static CacheTech cacheTech = CacheTech.Memory;
        private readonly string cacheKey = $"{typeof(T)}";
        public GenericRepository(ApplicationDbContext context, Func<CacheTech, ICacheService> cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }
        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);

            await _context.SaveChangesAsync();
            BackgroundJob.Enqueue(() => RefreshCache());
            return entity;
        }

        public async Task DeleteAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
            BackgroundJob.Enqueue(() => RefreshCache());
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            if (!_cacheService(cacheTech).TryGet(cacheKey, out IReadOnlyList<T> cachedList))
            {
                cachedList = await _context.Set<T>().ToListAsync();

                _cacheService(cacheTech).Set(cacheKey, cachedList);
            }
            return cachedList;
        }

        public virtual async Task<T> GetByIdAsync(long id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            BackgroundJob.Enqueue(() => RefreshCache());
        }
        public async Task RefreshCache()
        {
            _cacheService(cacheTech).Remove(cacheKey);

            var cachedList = await _context.Set<T>().ToListAsync();

            _cacheService(cacheTech).Set(cacheKey, cachedList);
        }
    }
}
