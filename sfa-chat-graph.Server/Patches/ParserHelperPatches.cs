using VDS.RDF.Parsing.Contexts;
using VDS.RDF.Parsing;
using VDS.RDF;
using HarmonyLib;

namespace sfa_chat_graph.Server.Patches
{
	/// <summary>
	/// Patches a bug in dotNetRdf, this is done to keep compatability to the original library without recompiling or submoduling it
	/// </summary>
	[HarmonyPatch(typeof(VDS.RDF.Parsing.ParserHelper))]
	[HarmonyPatch(nameof(VDS.RDF.Parsing.ParserHelper.TryResolveUri), typeof(IResultsParserContext), typeof(string))]
	class ParserHelperPatches
	{
		[HarmonyPrefix]
		static bool Prefix(IResultsParserContext context, string value, ref INode __result)
		{
			__result = TryResolveUriImpl(context, value);
			return false;
		}

		static INode TryResolveUriImpl(IResultsParserContext context, string value)
		{
			try
			{
				var uri = Tools.ResolveUri(value, context.Handler.BaseUri.AbsoluteUri, context.UriFactory);
				return context.Handler.CreateUriNode(context.UriFactory.Create(uri));
			}
			catch (UriFormatException formatEx)
			{
				throw new RdfParseException(
						"Unable to resolve the URI '" + value + "' due to the following error:\n" + formatEx.Message,
						formatEx);
			}
			catch (RdfException rdfEx)
			{
				throw new RdfParseException(
						"Unable to resolve the URI '" + value + "' due to the following error:\n" + rdfEx.Message, rdfEx);
			}
		}
	}
}
