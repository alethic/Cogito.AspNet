using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

using Buildalyzer;
using Buildalyzer.Environment;

using FluentAssertions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.AspNet.MSBuild.Tests
{

    [TestClass]
    public class ProjectTests
    {

        /// <summary>
        /// Forwards MSBuild events to the test context.
        /// </summary>
        class TargetLogger : Logger
        {

            readonly TestContext context;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="context"></param>
            /// <exception cref="ArgumentNullException"></exception>
            public TargetLogger(TestContext context)
            {
                this.context = context ?? throw new ArgumentNullException(nameof(context));
            }

            public override void Initialize(IEventSource eventSource)
            {
                eventSource.AnyEventRaised += OnAnyEventRaised;
            }

            void OnAnyEventRaised(object sender, BuildEventArgs args)
            {
                context.WriteLine(args.Message);
            }

        }

        public static Dictionary<string, string> Properties { get; set; }

        public static string TestRoot { get; set; }

        public static string TempRoot { get; set; }

        public static string WorkRoot { get; set; }

        public static string NuGetPackageRoot { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var location = Path.GetDirectoryName(typeof(ProjectTests).Assembly.Location);

            // properties to load into test build
            Properties = File.ReadAllLines(Path.Combine(location, "Cogito.AspNet.MSBuild.Tests.properties")).Select(i => i.Split(new[] { '=' }, 2)).ToDictionary(i => i[0], i => i[1]);

            // root of the project collection itself
            TestRoot = Path.Combine(location, "Project");

            // temporary directory
            TempRoot = Path.Combine(Path.GetTempPath(), "Cogito.AspNet.MSBuild.Tests", Guid.NewGuid().ToString());
            if (Directory.Exists(TempRoot))
                Directory.Delete(TempRoot, true);
            Directory.CreateDirectory(TempRoot);

            // work directory
            WorkRoot = Path.Combine(context.TestRunResultsDirectory, "Cogito.AspNet.MSBuild.Tests", "ProjectTests");
            if (Directory.Exists(WorkRoot))
                Directory.Delete(WorkRoot, true);
            Directory.CreateDirectory(WorkRoot);

            // other required sub directories
            NuGetPackageRoot = Path.Combine(TempRoot, "nuget", "packages");

            // nuget.config file that defines package sources
            new XDocument(
                new XElement("configuration",
                    new XElement("config",
                        new XElement("add",
                            new XAttribute("key", "globalPackagesFolder"),
                            new XAttribute("value", NuGetPackageRoot))),
                    new XElement("packageSources",
                        new XElement("clear"),
                        new XElement("add",
                            new XAttribute("key", "nuget.org"),
                            new XAttribute("value", "https://api.nuget.org/v3/index.json")),
                        new XElement("add",
                            new XAttribute("key", "dev"),
                            new XAttribute("value", Path.Combine(location, "nuget"))))))
                .Save(Path.Combine(TestRoot, "nuget.config"));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                if (TempRoot != null && Directory.Exists(TempRoot))
                    Directory.Delete(TempRoot, true);
            }
            catch
            {
                // temp cleanup is best effort
            }
        }

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void CanBuildTestProject()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                Assert.Inconclusive("Web Application Projects require Windows.");

            var manager = new AnalyzerManager();

            // WebProjectReference is deliberately not a ProjectReference, so the referenced project is
            // not in the host's restore graph. A solution build restores both; this does the same, so
            // the package's targets land in Sample.Web's scope and GetWebPublishPath is defined there.
            var restoreOptions = new EnvironmentOptions();
            restoreOptions.WorkingDirectory = TestRoot;
            restoreOptions.Preference = EnvironmentPreference.Framework;
            restoreOptions.DesignTime = false;
            restoreOptions.TargetsToBuild.Clear();
            restoreOptions.TargetsToBuild.Add("Restore");
            restoreOptions.Arguments.Add("/v:d");

            foreach (var web in new[] { "Sample.Web", "Sample.Web.Package" })
            {
                var webAnalyzer = manager.GetProject(Path.Combine(TestRoot, web, web + ".csproj"));
                webAnalyzer.AddBuildLogger(new TargetLogger(TestContext));
                webAnalyzer.SetGlobalProperty("ImportDirectoryBuildProps", "false");
                webAnalyzer.SetGlobalProperty("ImportDirectoryBuildTargets", "false");
                webAnalyzer.SetGlobalProperty("PackageVersion", Properties["PackageVersion"]);
                webAnalyzer.SetGlobalProperty("RestorePackagesPath", NuGetPackageRoot + Path.DirectorySeparatorChar);
                webAnalyzer.SetGlobalProperty("Configuration", "Release");
                webAnalyzer.Build(restoreOptions).OverallSuccess.Should().BeTrue();
            }

            var analyzer = manager.GetProject(Path.Combine(TestRoot, "Sample.Host", "Sample.Host.csproj"));
            analyzer.AddBuildLogger(new TargetLogger(TestContext));
            analyzer.AddBinaryLogger(Path.Combine(WorkRoot, "msbuild.binlog"));
            analyzer.SetGlobalProperty("ImportDirectoryBuildProps", "false");
            analyzer.SetGlobalProperty("ImportDirectoryBuildTargets", "false");
            analyzer.SetGlobalProperty("PackageVersion", Properties["PackageVersion"]);
            analyzer.SetGlobalProperty("RestorePackagesPath", NuGetPackageRoot + Path.DirectorySeparatorChar);
            analyzer.SetGlobalProperty("Configuration", "Release");

            var options = new EnvironmentOptions();
            options.WorkingDirectory = TestRoot;
            options.Preference = EnvironmentPreference.Framework;
            options.DesignTime = false;
            options.TargetsToBuild.Clear();
            options.TargetsToBuild.Add("Clean");
            options.TargetsToBuild.Add("Restore");
            options.TargetsToBuild.Add("Build");
            options.TargetsToBuild.Add("Publish");
            options.Arguments.Add("/v:d");

            var results = analyzer.Build(options);
            TestContext.AddResultFile(Path.Combine(WorkRoot, "msbuild.binlog"));
            results.OverallSuccess.Should().BeTrue();

            var binDir = Path.Combine(TestRoot, "Sample.Host", "bin", "Release");

            // check in build output and publish output
            foreach (var i in new[] { "", "publish" })
            {
                var outDir = Path.Combine(binDir, i);

                // FileSystem method: the published site imported as a directory tree
                File.Exists(Path.Combine(outDir, "web", "Web.config")).Should().BeTrue();
                File.Exists(Path.Combine(outDir, "web", "Default.aspx")).Should().BeTrue();
                File.Exists(Path.Combine(outDir, "web", "bin", "Sample.Web.dll")).Should().BeTrue();

                // Package method: the deployment package and its sidecar files
                File.Exists(Path.Combine(outDir, "zip", "Sample.Web.Package.zip")).Should().BeTrue();
                File.Exists(Path.Combine(outDir, "zip", "Sample.Web.Package.SetParameters.xml")).Should().BeTrue();
            }
        }

    }

}
