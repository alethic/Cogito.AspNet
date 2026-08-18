using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Buildalyzer;
using Buildalyzer.Environment;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.AspNet.MSBuild.Tests
{

    [TestClass]
    public class ProjectTests
    {

        /// <summary>
        /// Version of the package produced by the build and staged into the output.
        /// </summary>
        static string PackageVersion;

        /// <summary>
        /// Root of the fixture project directory in the test output.
        /// </summary>
        static string TestRoot;

        /// <summary>
        /// Local feed containing the package under test.
        /// </summary>
        static string NuGetFeed;

        /// <summary>
        /// Temporary directory holding the restored packages.
        /// </summary>
        static string TempRoot;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var location = Path.GetDirectoryName(typeof(ProjectTests).Assembly.Location);

            var properties = File.ReadAllLines(Path.Combine(location, "Cogito.AspNet.MSBuild.Tests.properties"))
                .Select(i => i.Split(new[] { '=' }, 2))
                .ToDictionary(i => i[0], i => i[1]);
            PackageVersion = properties["PackageVersion"];

            TestRoot = Path.Combine(location, "Project");
            NuGetFeed = Path.Combine(location, "nuget");

            TempRoot = Path.Combine(Path.GetTempPath(), "Cogito.AspNet.MSBuild.Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(TempRoot);
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

        [TestMethod]
        public void CanBuildTestProject()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                Assert.Inconclusive("Web Application Projects require Windows.");

            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(Path.Combine(TestRoot, "Sample.Host", "Sample.Host.csproj"));
            analyzer.SetGlobalProperty("Configuration", "Release");
            analyzer.SetGlobalProperty("PackageVersion", PackageVersion);
            analyzer.SetGlobalProperty("RestorePackagesPath", Path.Combine(TempRoot, "packages"));
            analyzer.SetGlobalProperty("RestoreAdditionalProjectSources", NuGetFeed);
            analyzer.AddBinaryLogger(Path.Combine(TempRoot, "msbuild.binlog"));

            var options = new EnvironmentOptions();
            options.Preference = EnvironmentPreference.Framework;
            options.DesignTime = false;
            options.TargetsToBuild.Clear();
            options.TargetsToBuild.Add("Clean");
            options.TargetsToBuild.Add("Restore");
            options.TargetsToBuild.Add("Build");

            var results = analyzer.Build(options);
            results.OverallSuccess.Should().BeTrue();

            var output = Path.Combine(TestRoot, "Sample.Host", "bin", "Release");

            // FileSystem method: the published site imported as a directory tree
            File.Exists(Path.Combine(output, "web", "Web.config")).Should().BeTrue();
            File.Exists(Path.Combine(output, "web", "Default.aspx")).Should().BeTrue();
            File.Exists(Path.Combine(output, "web", "bin", "Sample.Web.dll")).Should().BeTrue();

            // Package method: the deployment package and its sidecar files
            File.Exists(Path.Combine(output, "zip", "Sample.Web.zip")).Should().BeTrue();
            File.Exists(Path.Combine(output, "zip", "Sample.Web.SetParameters.xml")).Should().BeTrue();
        }

    }

}
