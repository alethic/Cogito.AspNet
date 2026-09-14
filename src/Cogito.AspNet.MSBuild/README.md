# Cogito.AspNet.MSBuild

MSBuild targets for referencing an ASP.NET Web Application Project from another project.

## Why

A Web Application Project is not a normal project reference. Referencing one gets you its assembly
but none of the content — the `.aspx`, `.ascx`, `web.config` and static files that make it a site —
so composing a site out of several web projects means copying files with hand-written targets and
keeping the binding redirects in step.

## Install

```shell
dotnet add package Cogito.AspNet.MSBuild
```

## Use

Reference the web project with `WebProjectReference` instead of `ProjectReference`:

```xml
<ItemGroup>
    <WebProjectReference Include="..\Contoso.Web\Contoso.Web.csproj" />
</ItemGroup>
```

The referenced project's content is published into the consuming site, and its assembly binding
redirects are merged into the consuming `web.config` rather than overwriting it.

Targets .NET Framework.

## License

MIT.
