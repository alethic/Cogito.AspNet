# Cogito.AspNet

[![Build](https://github.com/alethic/Cogito.AspNet/actions/workflows/Cogito.AspNet.yml/badge.svg)](https://github.com/alethic/Cogito.AspNet/actions/workflows/Cogito.AspNet.yml)

MSBuild targets for composing an ASP.NET site out of several Web Application Projects, content and binding redirects included.

## Packages

**[Cogito.AspNet.MSBuild](https://www.nuget.org/packages/Cogito.AspNet.MSBuild)** — MSBuild targets for referencing an ASP.NET Web Application Project from another project.

Each package carries its own README with the detail; the links above go to nuget.org.

## Building

```shell
dotnet restore Cogito.AspNet.slnx
dotnet msbuild -p:Configuration=Release Cogito.AspNet.dist.msbuildproj
```

Packages are staged into `dist/nuget` and test suites into `dist/tests`; run a suite with
`dotnet test -f <tfm> <path to its assembly>`.

## License

MIT — see [LICENSE](LICENSE).
