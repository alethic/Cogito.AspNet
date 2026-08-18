# Cogito.AspNet

MSBuild extensions for ASP.NET Web Application Projects.

## Cogito.AspNet.MSBuild

A development-dependency package that adds two capabilities to classic Web Application Projects
(`.csproj` with the `{349c5851-65df-11da-9384-00065b846f21}` project type):

### Binding redirect generation for Web.config

The stock `GenerateBindingRedirects` target skips web application projects. With this package
installed and `AutoGenerateBindingRedirects` enabled, generation of the output config is forced,
and after each build the computed `<assemblyBinding>` section is merged back into `Web.config`,
replacing the existing section wholesale.

```xml
<PropertyGroup>
    <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
</PropertyGroup>
```

Set `CopyGeneratedBindingRedirectsToWebConfig` to `false` to generate the output config without
rewriting `Web.config`.

### WebProjectReference

A dedicated reference item for consuming a web project's publish output. Unlike a
`ProjectReference`, the referenced project's assembly is not referenced — the referenced
project is published through the Web Publishing Pipeline (its `WebPublish` target) into a
staging directory, and the staged output is imported into the containing project's output
and publish directories.

```xml
<ItemGroup>
    <WebProjectReference Include="..\My.Web\My.Web.csproj">
        <WebTargetPath>My.Web\</WebTargetPath>
        <WebPublishMethod>FileSystem</WebPublishMethod>
        <WebProperties>PrecompileBeforePublish=true</WebProperties>
    </WebProjectReference>
</ItemGroup>
```

- `WebTargetPath` selects the relative directory under the output where the imported output
  is placed, and defaults to the referenced project's name.
- `WebPublishMethod` selects the Web Publishing Pipeline publish method: `FileSystem`
  (default) imports the published site as a directory tree; `Package` imports the
  deployment package (`PackageAsSingleFile`) and its sidecar files.
- `WebProperties` passes additional semicolon-separated MSBuild properties to the
  referenced project's publish.
