using System.Collections.Generic;

namespace Amt.SharePoint.Integration
{
    public interface ISharePointRepository<T> where T : SharePointDomainModel
    {
        void Add(T aggregateRoot);

        void Update(T aggregateRoot);

        void Delete(T aggregateRoot);

        T GetById(int id);

        IEnumerable<T> GetByQuery(string query = "<Query></Query>");

        // I don't know if I should include this in the interface.
        TType GetById<TType>(int id) where TType : SharePointDomainModel, new();
    }
}
