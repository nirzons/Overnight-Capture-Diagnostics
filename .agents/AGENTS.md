# Repository Behavioral Rules

## Git Workflow Rules
- **No Automatic Git Commit or Push**: Never perform `git commit` or `git push` automatically. The user will verify the code on a real rig and execute all git commits and pushes manually.

## Versioning Rules
- **Updating the Plugin Version**: When you are asked to bump or update the version of the plugin, you MUST update all of the following 3 locations:
  1. `manifest.json`: Update the `Version` object (`Major`, `Minor`, `Patch`, `Build`).
  2. `Properties\AssemblyInfo.cs`: Update `[assembly: AssemblyVersion("...")]` and `[assembly: AssemblyFileVersion("...")]`. (N.I.N.A reads the version from here).
  3. `*.csproj`: Update the `<Version>` tag.
  *CRITICAL*: After updating these files, you must run `dotnet build` to ensure the new version is embedded into the generated `.dll`.

## GitHub Actions Release Rules
- **Untracked Files**: Before pushing a release tag, ALWAYS run `git status` to verify that no new source files are left as untracked. A local build might succeed with untracked files, but the GitHub Actions build will fail with missing context (`CS0103`) because those files are not in the repository.
- **Multiple .csproj Files (`MSB1011`)**: Do NOT leave temporary or test `.csproj` files in the repository root. GitHub Actions runners using .NET 8 will fail with `MSBUILD : error MSB1011` if there are multiple `.csproj` files when running `dotnet restore` or `dotnet build`. Ensure there is only one primary `.csproj` file in the directory. (Note: `.slnx` files are not natively supported by .NET 8 without preview flags and will be ignored by the runner).
