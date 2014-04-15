using System.Collections.Generic;
using System.IO;

namespace Amt.SharePoint.Integration
{
    public interface ISharePointRepository<T> where T : SharePointDomainModel
    {
        void Add(T aggregateRoot);

        void Update(T aggregateRoot);

        void Delete(T aggregateRoot);

        T GetById(int id);

        IEnumerable<T> GetByIds(IEnumerable<int> ids);

        IEnumerable<T> GetByQuery(string query = "<Query></Query>");

        void DownloadFile<TType>(TType aggregateRoot, Stream download) where TType : SharePointDocumentDomainModel;

        // I don't know if I should include this in the interface.
        TType GetById<TType>(int id) where TType : SharePointDomainModel, new();
    }
}
