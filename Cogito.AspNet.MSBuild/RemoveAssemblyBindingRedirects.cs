﻿using System.Xml.Linq;

using Microsoft.Build.Framework;

namespace Cogito.AspNet.MSBuild
{

    /// <summary>
    /// Removes the assembly binding redirects from the given file.
    /// </summary>
    public class RemoveAssemblyBindingRedirects : Microsoft.Build.Utilities.Task
    {

        static readonly XNamespace asmv1 = "urn:schemas-microsoft-com:asm.v1";

        /// <summary>
        /// File from which to remove binding redirects.
        /// </summary>
        [Required]
        public ITaskItem File { get; set; }

        public override bool Execute()
        {
            var file = XDocument.Load(File.ItemSpec);
            file.Root.Element("runtime")?.Elements(asmv1 + "assemblyBinding")?.Remove();
            file.Save(File.ItemSpec);

            return true;
        }

    }

}
