using Statiq.Markdown;
using Statiq.Web.Modules;
using Statiq.Web.Pipelines;

namespace Singular;

public class Sections : Pipeline {
	public Sections(Templates templates) {
		Dependencies.Add(nameof(Content));

		InputModules = new ModuleList {
			new ReadFiles("./index.cshtml")
		};

		ProcessModules = new ModuleList {
			new ExecuteBranch {
				new ConcatDocuments(nameof(Content)),
				new RenderContentProcessTemplates(templates),
				new ExecuteModules()
			}
		};

		PostProcessModules = new ModuleList {
			new SetDestination("output.html")
		};

		OutputModules = new ModuleList {
			new WriteFiles()
		};
	}
}