using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application
{
    public interface IGenericRepository<T> where T : class
    {
        T GetById(object id);

        Task<T> GetByIdAsync(object id);

        IEnumerable<T> GetAll();

        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);       

        Task<T> FirstOrDefualt(Expression<Func<T, bool>> predicate);

        Task Add(T entity);
        void AddRange(IEnumerable<T> entities);

        void Remove(T entity);

        void RemoveRange(IEnumerable<T> entities);

        void Update(T entity);

        Task<int> GetCount(Expression<Func<T, bool>> predicate);
    }
}
