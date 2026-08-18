using System.IO;
using System.Linq;
using System.Xml.Linq;

using FluentAssertions;

using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.AspNet.MSBuild.Tasks.Test
{

    [TestClass]
    public class CopyAssemblyBindingRedirectsTests
    {

        static readonly XNamespace asmv1 = "urn:schemas-microsoft-com:asm.v1";

        static string GetSampleFile(string name)
        {
            return Path.Combine(Path.GetDirectoryName(typeof(CopyAssemblyBindingRedirectsTests).Assembly.Location), name);
        }

        static string CopyToTemp(string name)
        {
            var d = Path.GetTempFileName();
            File.Copy(GetSampleFile(name), d, true);
            return d;
        }

        [TestMethod]
        public void CanReplaceExistingRedirects()
        {
            var target = CopyToTemp("Target.config");

            var t = new CopyAssemblyBindingRedirects();
            t.SourceFile = new TaskItem(GetSampleFile("Source.config"));
            t.TargetFile = new TaskItem(target);
            t.Execute().Should().BeTrue();

            var doc = XDocument.Load(target);
            var identities = doc.Root.Element("runtime").Elements(asmv1 + "assemblyBinding").Elements(asmv1 + "dependentAssembly").Elements(asmv1 + "assemblyIdentity").Attributes("name").Select(i => (string)i).ToList();
            identities.Should().BeEquivalentTo(new[] { "Alpha.Assembly", "Newtonsoft.Json" });
            identities.Should().NotContain("Stale.Assembly");
        }

        [TestMethod]
        public void ShouldPreserveOrderWithinBinding()
        {
            var target = CopyToTemp("Target.config");

            var t = new CopyAssemblyBindingRedirects();
            t.SourceFile = new TaskItem(GetSampleFile("Source.config"));
            t.TargetFile = new TaskItem(target);
            t.Execute().Should().BeTrue();

            // the task orders assemblyBinding elements; dependentAssembly entries within a
            // binding keep their source order
            var doc = XDocument.Load(target);
            var identities = doc.Root.Element("runtime").Elements(asmv1 + "assemblyBinding").Elements(asmv1 + "dependentAssembly").Elements(asmv1 + "assemblyIdentity").Attributes("name").Select(i => (string)i).ToList();
            identities.Should().ContainInOrder("Newtonsoft.Json", "Alpha.Assembly");
        }

        [TestMethod]
        public void ShouldOrderAttributes()
        {
            var target = CopyToTemp("Target.config");

            var t = new CopyAssemblyBindingRedirects();
            t.SourceFile = new TaskItem(GetSampleFile("Source.config"));
            t.TargetFile = new TaskItem(target);
            t.Execute().Should().BeTrue();

            // Source.config deliberately scrambles the attribute order on the Newtonsoft entries
            var doc = XDocument.Load(target);
            var identity = doc.Root.Element("runtime").Elements(asmv1 + "assemblyBinding").Elements(asmv1 + "dependentAssembly").Elements(asmv1 + "assemblyIdentity").First(i => (string)i.Attribute("name") == "Newtonsoft.Json");
            identity.Attributes().Select(i => i.Name.LocalName).Should().ContainInOrder("name", "publicKeyToken", "culture");
            var redirect = identity.Parent.Element(asmv1 + "bindingRedirect");
            redirect.Attributes().Select(i => i.Name.LocalName).Should().ContainInOrder("oldVersion", "newVersion");
        }

        [TestMethod]
        public void ShouldPreserveUnrelatedContent()
        {
            var target = CopyToTemp("Target.config");

            var t = new CopyAssemblyBindingRedirects();
            t.SourceFile = new TaskItem(GetSampleFile("Source.config"));
            t.TargetFile = new TaskItem(target);
            t.Execute().Should().BeTrue();

            var doc = XDocument.Load(target);
            doc.Root.Element("appSettings").Should().NotBeNull();
            doc.Root.Element("system.web").Should().NotBeNull();
        }

        [TestMethod]
        public void ShouldCreateRuntimeElementWhenMissing()
        {
            var target = Path.GetTempFileName();
            File.WriteAllText(target, "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><appSettings /></configuration>");

            var t = new CopyAssemblyBindingRedirects();
            t.SourceFile = new TaskItem(GetSampleFile("Source.config"));
            t.TargetFile = new TaskItem(target);
            t.Execute().Should().BeTrue();

            var doc = XDocument.Load(target);
            doc.Root.Element("runtime").Should().NotBeNull();
            doc.Root.Element("runtime").Elements(asmv1 + "assemblyBinding").Elements(asmv1 + "dependentAssembly").Should().HaveCount(2);
        }

        [TestMethod]
        public void ShouldClearRedirectsWhenSourceHasNone()
        {
            var source = Path.GetTempFileName();
            File.WriteAllText(source, "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration></configuration>");
            var target = CopyToTemp("Target.config");

            var t = new CopyAssemblyBindingRedirects();
            t.SourceFile = new TaskItem(source);
            t.TargetFile = new TaskItem(target);
            t.Execute().Should().BeTrue();

            var doc = XDocument.Load(target);
            doc.Root.Element("runtime").Elements(asmv1 + "assemblyBinding").Should().BeEmpty();
        }

    }

}
