using System;
using System.Net.Security;
using System.ServiceModel;

namespace Amt.SharePoint.Integration.Authentication
{
    [ServiceContract]
    public interface IWsTrustFeb2005Contract
    {
        [OperationContract(ProtectionLevel = ProtectionLevel.EncryptAndSign, Action = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue", ReplyAction = "http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/Issue", AsyncPattern = true)]
        IAsyncResult BeginIssue(System.ServiceModel.Channels.Message request, AsyncCallback callback, object state);
        System.ServiceModel.Channels.Message EndIssue(IAsyncResult asyncResult);
    }
}