using MessagePack;
using MongoDB.Bson.Serialization.Attributes;

namespace SfaChatGraph.Server.Models
{
	[Union(0, typeof(ApiMessage))]
	[Union(1, typeof(ApiAssistantMessage))]
	[Union(2, typeof(ApiToolCallMessage))]
	[Union(3, typeof(ApiToolResponseMessage))]
	public interface IApiMessage
	{
	}
}
