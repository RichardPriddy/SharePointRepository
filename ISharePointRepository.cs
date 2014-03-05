using System.Collections.Generic;

namespace Amt.SharePoint.Integration
{
    public interface ISharePointRepository<T> where T : ISharePointDomainModel
    {
        void Add(T aggregateRoot);

        void Update(T aggregateRoot);

        void Delete(T aggregateRoot);

        T GetById(int id);

        IEnumerable<T> GetByQuery(string query);
    }
}
