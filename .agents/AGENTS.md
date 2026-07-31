# Repository Behavioral Rules

## Git Workflow Rules
- **No Automatic Git Commit or Push**: Never perform `git commit` or `git push` automatically. The user will verify the code on a real rig and execute all git commits and pushes manually.

## Versioning Rules
- **Updating the Plugin Version**: When you are asked to bump or update the version of the plugin, you MUST update all of the following 3 locations:
  1. `manifest.json`: Update the `Version` object (`Major`, `Minor`, `Patch`, `Build`).
  2. `Properties\AssemblyInfo.cs`: Update `[assembly: AssemblyVersion("...")]` and `[assembly: AssemblyFileVersion("...")]`. (N.I.N.A reads the version from here).
  3. `*.csproj`: Update the `<Version>` tag.
  *CRITICAL*: After updating these files, you must run `dotnet build` to ensure the new version is embedded into the generated `.dll`.
