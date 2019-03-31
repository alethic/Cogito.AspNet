using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Framework;

namespace Cogito.AspNet.MSBuild
{

    /// <summary>
    /// Copies the assembly bindings redirects from one config file to another.
    /// </summary>
    public class CopyAssemblyBindingRedirects : Microsoft.Build.Utilities.Task
    {

        /// <summary>
        /// File from which to obtain binding redirects.
        /// </summary>
        [Required]
        public ITaskItem SourceFile { get; set; }

        /// <summary>
        /// File to copy binding redirects.
        /// </summary>
        [Required]
        public ITaskItem TargetFile{ get; set; }

        public override bool Execute()
        {
            var source = XDocument.Load(SourceFile.ItemSpec);
            var target = XDocument.Load(TargetFile.ItemSpec);

            // load new assembly bindings
            var items = source.Root.Element("runtime")?.Elements("assemblyBinding") ?? Enumerable.Empty<XElement>();

            // ensure output runtime element exists
            if (target.Root.Element("runtime") == null)
                target.Root.Add(new XElement("runtime"));

            // remove existing binding elements
            target.Root.Element("runtime").Elements("assemblyBinding").Remove();
            target.Root.Element("runtime").Add(items);

            // save new file
            target.Save(TargetFile.ItemSpec);

            return true;
        }

    }

}
