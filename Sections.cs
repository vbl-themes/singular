using Statiq.Web.Modules;
using Statiq.Web.Pipelines;

namespace Singular;

public class Sections : Pipeline {
	public static class Keys {
		public const string SectionSources = nameof(SectionSources);
		public const string SectionOrderKey = nameof(SectionOrderKey);
	}
	
	public Sections(Templates templates) {
		Dependencies.AddRange(nameof(Inputs), nameof(Content), nameof(Data));

		ProcessModules = new ModuleList {
			new GetPipelineDocuments(Config.FromDocument(doc => doc.Get<ContentType>(WebKeys.ContentType) != ContentType.Asset || doc.MediaTypeEquals(MediaTypes.CSharp))),
			new FilterDocuments(Config.FromDocument(IsSection)),
			
			new ForEachDocument {
				new ExecuteConfig(Config.FromDocument((archiveDoc, _) => {
					return new ModuleList {
						new ReplaceDocuments(archiveDoc.GetList(WebKeys.ArchivePipelines, new[] { nameof(Content) }).ToArray()),
						new MergeMetadata(Config.FromValue(archiveDoc.Yield())).KeepExisting(),
						new FilterSources(archiveDoc.GetList<string>(Sections.Keys.SectionSources)),
						new OrderDocuments(archiveDoc.GetString(Sections.Keys.SectionOrderKey)),
						
						// Execute post-processing on found sources to make sure that partial templates are rendered
						templates.GetModule(ContentType.Content, Phase.PostProcess),
						
						// Make rendered documents available for looping and fix destiantion file name 
						new ReplaceDocuments(Config.FromContext(ctx => archiveDoc.Clone(new MetadataItems { { Statiq.Common.Keys.Children, ctx.Inputs } }).Yield())),
						new SetDestination(Config.FromSettings(s => archiveDoc.Destination.ChangeExtension(s.GetPageFileExtensions()[0])))
					};
				}))			
			}
		};

		PostProcessModules = new ModuleList {
			new ExecuteSwitch(Config.FromDocument(doc => doc.Get<ContentType>(WebKeys.ContentType)))
				.Case(ContentType.Data, templates.GetModule(ContentType.Data, Phase.PostProcess))
				.Case(ContentType.Content, (IModule)new RenderContentPostProcessTemplates(templates))
		};

		OutputModules = new ModuleList {
			new WriteFiles()
		};
	}

	public static bool IsSection(IDocument document) =>
		document.ContainsKey(Sections.Keys.SectionSources) || 
		document.ContainsKey(Sections.Keys.SectionOrderKey);
}