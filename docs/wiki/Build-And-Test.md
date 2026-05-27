# Build And Test

## Restore

```powershell
dotnet restore .\DragonMarkdown.slnx
```

## Build

```powershell
dotnet build .\DragonMarkdown.slnx
```

## Test

```powershell
dotnet test .\DragonMarkdown.slnx --no-build
```

## Coverage

```powershell
dotnet test .\DragonMarkdown.slnx --no-build --collect:"XPlat Code Coverage" --settings .\coverlet.runsettings --results-directory .\TestResults\Coverage
```

Coverage expectations:

- App logic should stay above 80 percent line coverage.
- Core logic should stay above 80 percent line coverage.
- Avalonia shell composition and native browser host files are excluded from unit coverage and should be checked with smoke/UI tests.

## Run Locally

```powershell
dotnet run --project .\src\DragonMarkdown.App\DragonMarkdown.App.csproj
```

With a startup folder:

```powershell
dotnet run --project .\src\DragonMarkdown.App\DragonMarkdown.App.csproj -- C:\docs\project
```
