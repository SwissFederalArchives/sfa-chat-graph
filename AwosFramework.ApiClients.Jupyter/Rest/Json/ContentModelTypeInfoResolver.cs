using AwosFramework.ApiClients.Jupyter.Rest.Models.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Json
{
	internal class ContentModelTypeInfoResolver : DefaultJsonTypeInfoResolver
	{
		private readonly IJupyterRestClient _client;

		public ContentModelTypeInfoResolver(IJupyterRestClient client)
		{
			_client=client;
		}

		private ContentModel CreateContentModelObject(Type specificType)
		{
			var instance = (ContentModel)Activator.CreateInstance(specificType, _client);
			return instance;
		}

		public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
		{
			var info = base.GetTypeInfo(type, options);
			if (type.IsAssignableTo(typeof(ContentModel)) && type.IsAbstract == false)
				info.CreateObject = () => CreateContentModelObject(type);

			return info;
		}
	}
}
