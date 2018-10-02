using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Amt.SharePoint.Integration.ExtensionMethods;
using Amt.SharePoint.Integration.ModelAttributes;
using Amt.SharePoint.Integration.Models;
using Microsoft.SharePoint.Client;
using File = Microsoft.SharePoint.Client.File;

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
            var url = _sharepointUrl;

            _ctx = new ClientContext(url + TSharePointSubSiteName)
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

            aggregateRoot.ID = listItem.Id;
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

        public void DownloadFile<TType>(TType aggregateRoot, Stream download) where TType : SharePointDocumentDomainModel
        {
            var fileInfo = File.OpenBinaryDirect(_ctx, aggregateRoot.FileRef);

            using (var memory = new MemoryStream())
            {
                var buffer = new byte[1024 * 64];
                int nread;

                while ((nread = fileInfo.Stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memory.Write(buffer, 0, nread);
                }

                memory.Seek(0, SeekOrigin.Begin);
                memory.CopyTo(download);
            }
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

        public IEnumerable<T> GetByIds(IEnumerable<int> ids)
        {
            return GetByQuery(ids.ToCamlQuery("Or"));
        }

        public IEnumerable<T> GetByQuery(string query = "<View><Query></Query></View>")
        {
            var web = _ctx.Web;
            var list = web.Lists.GetByTitle(TSharePointListName);

            query = AppendViewFields(query);

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

        /// <summary>
        /// Takes a CAML query and appends all properties of associated type into the ViewFields section of the CAML query.
        /// We ignore anything marked with the Display Property Attribute.
        /// </summary>
        /// <param name="query">The CAML query.</param>
        /// <returns>The CAML query with ViewFields appended.</returns>
        private string AppendViewFields(string query)
        {
            if (query.Contains("ViewFields"))
            {
                return query;
            }

            var viewFieldsBuilder = new StringBuilder("<ViewFields>");
            foreach (var propInfo in typeof(T).GetProperties())
            {
                // Ignore anything that's marked as a dislpay property.
                var displayPropertyAttribute = propInfo.GetCustomAttribute<DisplayPropertyAttribute>();
                if (displayPropertyAttribute != null) continue;
                // Don't map ignored properties
                var ignoredPropertyAttribute = propInfo.GetCustomAttribute<IgnoredPropertyAttribute>();
                if (ignoredPropertyAttribute != null) continue;

                viewFieldsBuilder.Append(string.Format("<FieldRef Name='{0}' />", propInfo.PropertyName()));
            }
            viewFieldsBuilder.Append("</ViewFields>");
            viewFieldsBuilder.Append("</View>");

            return query.Replace("</View>", viewFieldsBuilder.ToString());
        }

        private void SetPropertyValue<TType>(PropertyInfo propInfo, TType obj, ListItem item) where TType : SharePointDomainModel
        {
            // Don't map ignored properties
            var ignoredPropertyAttribute = propInfo.GetCustomAttribute<IgnoredPropertyAttribute>();
            if (ignoredPropertyAttribute != null) return;

            var attribute = propInfo.GetCustomAttribute<LookupListNameAttribute>();

            if (attribute == null)
            {
                var underlyingType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;

                if (underlyingType.FullName == "Amt.SharePoint.Integration.Models.User")
                {
                    var fieldUserValue = (FieldUserValue)item[propInfo.PropertyName()];
                    var user = _ctx.Web.SiteUsers.GetById(fieldUserValue.LookupId);
                    _ctx.Load(user);
                    _ctx.ExecuteQuery();

                    var sharePointUser = new Models.User
                    {
                        Email = user.Email,
                        ID = user.Id,
                        LoginName = user.LoginName,
                        Title = user.Title
                    };

                    propInfo.SetValue(obj, sharePointUser, null);
                }
                if (underlyingType.FullName == "Amt.SharePoint.Integration.Models.Hyperlink")
                {
                    var fieldUrlValue = (FieldUrlValue)item[propInfo.PropertyName()];

                    if (fieldUrlValue == null)
                    {
                        propInfo.SetValue(obj, null, null);
                        return;
                    }

                    //TODO: Downlaod content if downloadable.
                    var url = fieldUrlValue.Url;

                    var downloadableContent = propInfo.GetCustomAttribute<DownloadableContentAttribute>();
                    if (downloadableContent != null)
                    {
                        var rootUrl = string.Join("/", _sharepointUrl.Split('/').Take(3).ToArray());
                        url = url.Replace(rootUrl + "/:i:/r", "");
                        url = url.Replace(rootUrl, "");
                        url = url.Split('?')[0];
                        DownloadImage(url);
                    }

                    var hyperlink = new Hyperlink
                    {
                        Url = url,
                        Description = fieldUrlValue.Description,
                        TypeId = fieldUrlValue.TypeId
                    };

                    propInfo.SetValue(obj, hyperlink, null);
                }
                if (underlyingType.FullName == "Amt.SharePoint.Integration.Models.PublishingImage")
                {
                    var fieldUrlValue = (string)item[propInfo.PropertyName()];

                    if (fieldUrlValue == null)
                    {
                        propInfo.SetValue(obj, null, null);
                        return;
                    }

                    Match matches = Regex.Match(fieldUrlValue, "alt=\"(?<AltTag>.*?)\".* src=\"(?<SrcTag>.*?)\"");

                    DownloadImage(matches.Groups["SrcTag"].Value);

                    var image = new PublishingImage
                    {
                        Alt = matches.Groups["AltTag"].Value,
                        Src = matches.Groups["SrcTag"].Value
                    };

                    propInfo.SetValue(obj, image, null);
                }
                else
                {
                propInfo.SetValue(obj, Convert.ChangeType(item[propInfo.PropertyName()], underlyingType), null);
            }
            }
            else
            {
                if (item[propInfo.PropertyName()] == null) return;

                if (propInfo.PropertyType.IsArray)
                {
                    Type arrayType = propInfo.PropertyType.GetElementType();

                    var array = Array.CreateInstance(arrayType, ((FieldLookupValue[])(item[propInfo.PropertyName()])).Count());
                    
                    for (var index = 0; 
                             index < ((FieldLookupValue[]) (item[propInfo.PropertyName()])).Length; 
                             index++)
                    {
                        var lookupId = ((FieldLookupValue[]) (item[propInfo.PropertyName()]))[index].LookupId;

                        GenericInvoker invoker = DynamicMethods.
                            GenericMethodInvokerMethod(typeof (SharePointRepository<T>),
                                "GetById", new[] { arrayType },
                                new[] { arrayType });

                        var lookupItem = invoker(this, lookupId);
                        
                        array.SetValue(lookupItem, index);
                    }

                    propInfo.SetValue(obj, array, null);
                }
                else
                {
                    var lookupId = ((FieldLookupValue) (item[propInfo.PropertyName()])).LookupId;

                    GenericInvoker invoker = DynamicMethods.
                        GenericMethodInvokerMethod(typeof (SharePointRepository<T>),
                            "GetById", new[] {propInfo.PropertyType},
                            new[] {propInfo.PropertyType});

                    var lookupItem = invoker(this, lookupId);

                    propInfo.SetValue(obj, Convert.ChangeType(lookupItem, propInfo.PropertyType), null);
                }
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

                    // Can't map lookup property values
                    var lookupPropertyAttribute = propInfo.GetCustomAttribute<LookupPropertyValueAttribute>();
                    if (lookupPropertyAttribute != null && lookupPropertyAttribute.IsLookupProperty) continue;
                    // Can't map generated properties
                    var generatedPropertyAttribute = propInfo.GetCustomAttribute<GeneratedPropertyAttribute>();
                    if (generatedPropertyAttribute != null && generatedPropertyAttribute.IsPropertyGenerated) continue;
                    // Don't map ignored properties
                    var ignoredPropertyAttribute = propInfo.GetCustomAttribute<IgnoredPropertyAttribute>();
                    if (ignoredPropertyAttribute != null) continue;

                    var lookupListNameAttribute = propInfo.GetCustomAttribute<LookupListNameAttribute>();

                    if (propInfo.PropertyType.IsArray)
                    {
                        if (lookupListNameAttribute != null)
                        {
                            var values = propInfo.GetValue(aggregateRoot) as SharePointDomainModel[];

                            listItem[propInfo.PropertyName()] = values.Select(value => new FieldLookupValue
                            {
                                LookupId = value.ID
                            }).ToArray();
                        }
                        else
                        {
                            listItem[propInfo.PropertyName()] = propInfo.GetValue(aggregateRoot);
                        }
                    }
                    else
                    {
                        if (lookupListNameAttribute == null && propInfo.GetValue(aggregateRoot) != null)
                        {
                            // If the value is a date time and is date time min value, then SharePoint doesn't like it.
                            if (propInfo.PropertyType != typeof(DateTime) ||
                                (DateTime) propInfo.GetValue(aggregateRoot) != DateTime.MinValue)
                        {
                            listItem[propInfo.PropertyName()] = propInfo.GetValue(aggregateRoot);
                        }
                        }
                        else if(propInfo.GetValue(aggregateRoot) != null)
                        {
                            var value = propInfo.GetValue(aggregateRoot) as SharePointDomainModel;

                            var lookupValue = new FieldLookupValue
                            {
                                LookupId = value.ID
                            };

                            listItem[propInfo.PropertyName()] = lookupValue;
                        }
                    }
                }
                catch { }
            }
        }

        private void DownloadImage(string url)
        {
            var filePath = (HttpContext.Current.Server.MapPath("~") + url).Replace("\\/", "\\").Replace("/", "\\").Replace("%20", " ");
            var localPath = filePath.Substring(0, filePath.LastIndexOf("\\"));
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            if (!System.IO.File.Exists(filePath))
            {
                try
                {
                    FileInformation spFileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(_ctx, url);

                    using (Stream destination = System.IO.File.Create(filePath))
                    {
                        for (int a = spFileInfo.Stream.ReadByte(); a != -1; a = spFileInfo.Stream.ReadByte())
                            destination.WriteByte((byte) a);
                    }
                }
                catch (Exception ex)
                {
                    var e = ex;
                }
            }
        }
        
        private static string TSharePointListName
        {
            get
            {
                var attribute =
                    Attribute.GetCustomAttributes(typeof(T), typeof(ListNameAttribute)) as ListNameAttribute[];

                if (attribute == null)
                {
                    return typeof(T).Name;
                }
                if (attribute.Length == 0)
                {
                    return typeof(T).Name;
                }
                if (attribute.Length == 1)
                {
                    return attribute[0].ListName;
                }
                return attribute.First().ListName;
            }
        }

        private static string TSharePointSubSiteName
        {
            get
            {
                var attribute =
                    Attribute.GetCustomAttribute(typeof(T), typeof(SubSiteNameAttribute)) as SubSiteNameAttribute;

                return attribute == null
                    ? ""
                    : attribute.SubSiteName;
            }
        }
    }
}
