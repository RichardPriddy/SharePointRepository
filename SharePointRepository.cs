﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Text;
using System.Web;
using Amt.SharePoint.Integration.ExtensionMethods;
using Amt.SharePoint.Integration.ModelAttributes;
using Microsoft.SharePoint.Client;

namespace Amt.SharePoint.Integration
{
    public class SharePointRepository<T> : ISharePointRepository<T> where T : SharePointDomainModel, new()
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
            var list = web.Lists.GetByTitle(TSharePointListName);

            var itemCreateInfo = new ListItemCreationInformation();
            ListItem listItem = list.AddItem(itemCreateInfo);

            SetListItemProperties(aggregateRoot, listItem);

            listItem.Update();

            _ctx.ExecuteQuery();
        }

        public void Update(T aggregateRoot)
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(TSharePointListName);

            ListItem listItem = list.GetItemById(aggregateRoot.ID);

            SetListItemProperties(aggregateRoot, listItem);

            listItem.Update();

            _ctx.ExecuteQuery();
        }

        public void Delete(T aggregateRoot)
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(TSharePointListName);

            ListItem listItem = list.GetItemById(aggregateRoot.ID);

            listItem.DeleteObject();

            _ctx.ExecuteQuery();
        }

        public TType GetById<TType>(int id) where TType : SharePointDomainModel, new()
        {
            var web = _ctx.Web;

            var attribute = Attribute.GetCustomAttribute(typeof(TType),
                typeof(ListNameAttribute)) as ListNameAttribute;

            var name = attribute == null
                ? typeof(TType).Name
                : attribute.ListName;

            var list = web.Lists.GetByTitle(name);

            ListItem listItem = list.GetItemById(id);

            _ctx.Load(listItem);
            _ctx.ExecuteQuery();

            var t = typeof(TType);
            var obj = new TType();

            foreach (var propInfo in t.GetProperties())
            {
                try
                {
                    SetPropertyValue(propInfo, obj, listItem);
                }
                catch { }
            }

            return obj;
        }
        
        public T GetById(int id)
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(TSharePointListName);

            ListItem listItem = list.GetItemById(id);

            _ctx.Load(listItem);
            _ctx.ExecuteQuery();

            var t = typeof(T);
            var obj = new T();

            foreach (var propInfo in t.GetProperties())
            {
                try
                {
                    SetPropertyValue(propInfo, obj, listItem);
                }
                catch { }
            }

            return obj;
        }

        public IEnumerable<T> GetByQuery(string query = "<Query></Query>")
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(TSharePointListName);

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
                        SetPropertyValue(propInfo, obj, item);
                    }
                    catch(Exception ex) { }
                }

                returnList.Add(obj);
            }

            return returnList;
        }

        public void Disconnect()
        {
            _ctx.Dispose();
        }

        private void SetPropertyValue<TType>(PropertyInfo propInfo, TType obj, ListItem item) where TType : SharePointDomainModel
        {
            var attribute = propInfo.GetCustomAttribute<LookupListNameAttribute>();

            if (attribute == null)
            {
                propInfo.SetValue(obj,
                    Convert.ChangeType(item[propInfo.PropertyName()], propInfo.PropertyType), null);
            }
            else
            {
                if (item[propInfo.PropertyName()] == null) return;

                var id = ((FieldLookupValue)(item[propInfo.PropertyName()])).LookupId;

                GenericInvoker invoker = DynamicMethods.
                    GenericMethodInvokerMethod(typeof(SharePointRepository<T>),
                        "GetById", new[] { propInfo.PropertyType },
                        new[] { propInfo.PropertyType });

                var lookupItem = invoker(this, id);

                propInfo.SetValue(obj, Convert.ChangeType(lookupItem, propInfo.PropertyType), null);
            }
        }

        private static void SetListItemProperties(T aggregateRoot, ListItem listItem)
        {
            var t = typeof(T);
            foreach (var propInfo in t.GetProperties())
            {
                try
                {
                    if (propInfo.Name == "ID") continue;
                    
                    var attribute = propInfo.GetCustomAttribute<LookupListNameAttribute>();

                    if (attribute == null)
                    {
                        listItem[propInfo.PropertyName()] = propInfo.GetValue(aggregateRoot);
                    }
                    else
                    {
                        var value = propInfo.GetValue(aggregateRoot) as SharePointDomainModel;

                        var lookupValue = new FieldLookupValue
                        {
                            LookupId = value.ID
                        };

                        listItem[propInfo.PropertyName()] = lookupValue;
                    }
                }
                catch { }
            }
        }

        private static string TSharePointListName
        {
            get
            {
                var attribute =
                    Attribute.GetCustomAttribute(typeof (T), typeof (ListNameAttribute)) as ListNameAttribute;

                return attribute == null
                    ? typeof (T).Name
                    : attribute.ListName;
            }
        }
    }
}
