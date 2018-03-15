using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Euclid7.Services
{
    //NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IDotUpdate" in both code and config file together.
    [ServiceContract]
    public interface IDotUpdate
    {
		[OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "Update")]
        string Update(Stream JSONdataStream);
	}
}