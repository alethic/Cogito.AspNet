using System.IO;
using System.Xml.Linq;

using FluentAssertions;

using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.AspNet.MSBuild.Tasks.Test
{

    [TestClass]
    public class RemoveAssemblyBindingRedirectsTests
    {

        static readonly XNamespace asmv1 = "urn:schemas-microsoft-com:asm.v1";

        static string CopyToTemp(string name)
        {
            var s = Path.Combine(Path.GetDirectoryName(typeof(RemoveAssemblyBindingRedirectsTests).Assembly.Location), name);
            var d = Path.GetTempFileName();
            File.Copy(s, d, true);
            return d;
        }

        [TestMethod]
        public void CanRemoveRedirects()
        {
            var file = CopyToTemp("Target.config");

            var t = new RemoveAssemblyBindingRedirects();
            t.File = new TaskItem(file);
            t.Execute().Should().BeTrue();

            var doc = XDocument.Load(file);
            doc.Root.Element("runtime").Should().NotBeNull();
            doc.Root.Element("runtime").Elements(asmv1 + "assemblyBinding").Should().BeEmpty();
            doc.Root.Element("appSettings").Should().NotBeNull();
        }

        [TestMethod]
        public void ShouldPreserveFormatting()
        {
            var file = CopyToTemp("Target.config");

            var t = new RemoveAssemblyBindingRedirects();
            t.File = new TaskItem(file);
            t.Execute().Should().BeTrue();

            var text = File.ReadAllText(file).Replace("\r\n", "\n");
            text.Should().EndWith("\n");
            text.Should().Contain("  <appSettings>\n    <add key=\"Keep\" value=\"true\" />\n  </appSettings>");
        }

        [TestMethod]
        public void ShouldIgnoreFileWithoutRuntimeElement()
        {
            var file = Path.GetTempFileName();
            File.WriteAllText(file, "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><appSettings /></configuration>");

            var t = new RemoveAssemblyBindingRedirects();
            t.File = new TaskItem(file);
            t.Execute().Should().BeTrue();

            var doc = XDocument.Load(file);
            doc.Root.Element("appSettings").Should().NotBeNull();
        }

    }

}
