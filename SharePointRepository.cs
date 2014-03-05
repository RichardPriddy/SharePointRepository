using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using Amt.SharePoint.Integration.ModelAttributes;
using Microsoft.SharePoint.Client;

namespace Amt.SharePoint.Integration
{
    public class SharePointRepository<T> : ISharePointRepository<T> where T : ISharePointDomainModel, new()
    {
        private ClientContext _ctx;
        private readonly string _sharepointUrl;
        private readonly string _username;
        private readonly SecureString _password;

        public SharePointRepository(string sharepointUrl, string username, string password)
        {
            _sharepointUrl = sharepointUrl;
            _username = username;

            var secureStr = new SecureString();

            foreach (var c in password.ToCharArray())
            {
                secureStr.AppendChar(c);
            }

            _password = secureStr;

            Connect();
        }

        private void Connect()
        {
            _ctx = new ClientContext(_sharepointUrl)
                      {
                          AuthenticationMode = ClientAuthenticationMode.Default,
                          Credentials = new SharePointOnlineCredentials(_username, _password)
                      };
        }

        public void Add(T aggregateRoot)
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(GetListName());

            var itemCreateInfo = new ListItemCreationInformation();
            ListItem listItem = list.AddItem(itemCreateInfo);

            SetListItemProperties(aggregateRoot, listItem);

            listItem.Update();

            _ctx.ExecuteQuery();
        }

        public void Update(T aggregateRoot)
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(GetListName());

            ListItem listItem = list.GetItemById(aggregateRoot.ID);

            SetListItemProperties(aggregateRoot, listItem);

            listItem.Update();

            _ctx.ExecuteQuery();
        }

        public void Delete(T aggregateRoot)
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(GetListName());

            ListItem listItem = list.GetItemById(aggregateRoot.ID);

            listItem.DeleteObject();

            _ctx.ExecuteQuery();
        }

        public T GetById(int id)
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(GetListName());

            ListItem listItem = list.GetItemById(id);

            _ctx.Load(listItem);
            _ctx.ExecuteQuery();

            var t = typeof(T);
            var obj = new T();

            foreach (var propInfo in t.GetProperties())
            {
                try
                {
                    propInfo.SetValue(obj, Convert.ChangeType(listItem[GetPropertyName(propInfo)], propInfo.PropertyType), null);
                }
                catch { }
            }

            return obj;
        }

        public IEnumerable<T> GetByQuery(string query)
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(GetListName());

            var camlQuery = new CamlQuery { ViewXml = query };

            ListItemCollection listItems = list.GetItems(camlQuery);

            _ctx.Load(listItems);
            _ctx.ExecuteQuery();

            var returnList = new List<T>();

            foreach (var item in listItems)
            {
                var t = typeof(T);
                var obj = new T();

                foreach (var propInfo in t.GetProperties())
                {
                    try
                    {
                        propInfo.SetValue(obj, Convert.ChangeType(item[GetPropertyName(propInfo)], propInfo.PropertyType), null);
                    }
                    catch { }
                }

                returnList.Add(obj);
            }

            return returnList;
        }

        public void Disconnect()
        {
            _ctx.Dispose();
        }

        private static void SetListItemProperties(T aggregateRoot, ListItem listItem)
        {
            var t = typeof(T);
            foreach (var propInfo in t.GetProperties())
            {
                try
                {
                    if (propInfo.Name == "ID") continue;

                    listItem[GetPropertyName(propInfo)] = propInfo.GetValue(aggregateRoot);
                }
                catch { }
            }
        }

        private static string GetListName()
        {
            var attribute = Attribute.GetCustomAttribute(typeof(T), typeof(ListNameAttribute)) as ListNameAttribute;

            return attribute == null
                ? typeof(T).Name
                : attribute.ListName;
        }

        private static string GetPropertyName(PropertyInfo propInfo)
        {
            var attribute = propInfo.GetCustomAttribute<ColumnNameAttribute>();

            return attribute == null
                ? propInfo.Name
                : attribute.ColumnName;
        }
    }
}
