using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	internal class DirectoryContentResponse
	{
		public required ContentModel[] Content { get; set; }
	}
}
