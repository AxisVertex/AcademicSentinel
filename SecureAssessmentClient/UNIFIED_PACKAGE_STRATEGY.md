# 🎯 Unified Package Management Strategy

## Overview

The solution now uses **centralized package management** via `Directory.Build.props`. This ensures all NuGet packages are versioned consistently across both projects and eliminates the risk of version mismatches.

## Architecture

### Single Source of Truth: `Directory.Build.props`

Located at the workspace root, this file defines **all** NuGet package versions for the entire solution.

```
SecureAssessmentClient/
├── Directory.Build.props          ← Central package version management
├── SecureAssessmentClient.csproj  ← Imports shared + client packages
├── AcademicSentinel.sln
└── AcademicSentinel.Server/
    └── AcademicSentinel.Server.csproj  ← Imports shared + server packages
```

### Package Categories

#### 1. **Shared Packages** (Applies to any framework)
Packages used by both client and server:
- `Newtonsoft.Json 13.0.3` - JSON serialization

#### 2. **Client-Specific Packages** (Applies only to .NET 9.0)
Automatically applied only to projects targeting `net9.0`:
- `log4net 2.0.15` - Logging framework
- `Microsoft.AspNetCore.SignalR.Client 8.0.10` - Real-time WebSocket communication
- `System.Text.Json 8.0.5` - JSON serialization (client-specific)
- `System.Net.Http.Json 8.0.0` - HTTP/JSON helpers (client-specific)

#### 3. **Server-Specific Packages** (Applies only to .NET 10.0)
Automatically applied only to projects targeting `net10.0`:
- `BCrypt.Net-Next 4.1.0` - Password hashing
- `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.5` - JWT authentication
- `Microsoft.AspNetCore.SignalR.Core 1.2.9` - SignalR server library
- `Microsoft.EntityFrameworkCore 10.0.5` - ORM base package
- `Microsoft.EntityFrameworkCore.Sqlite 10.0.5` - SQLite provider
- `Microsoft.EntityFrameworkCore.Tools 10.0.5` - EF Core CLI tools
- `Microsoft.IdentityModel.Tokens 8.0.1` - JWT token validation
- `Microsoft.OpenApi 1.6.14` - OpenAPI/Swagger support
- `Swashbuckle.AspNetCore 6.6.2` - Swagger UI
- `System.IdentityModel.Tokens.Jwt 8.0.1` - JWT handling

## How It Works

### MSBuild Conditions

The `Directory.Build.props` file uses MSBuild conditions to apply packages selectively:

```xml
<!-- Applied only to projects with TargetFramework starting with "net9" -->
<ItemGroup Condition="$(TargetFramework.StartsWith('net9'))">
  <PackageReference Include="log4net" Version="2.0.15" />
  ...
</ItemGroup>

<!-- Applied only to projects with TargetFramework starting with "net10" -->
<ItemGroup Condition="$(TargetFramework.StartsWith('net10'))">
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.5" />
  ...
</ItemGroup>
```

### Import Mechanism

When you run `dotnet build` or `dotnet restore`:
1. MSBuild finds `Directory.Build.props` in the workspace root
2. Automatically imports it for all projects in the tree
3. Each project evaluates conditions based on its `TargetFramework`
4. Only matching packages are added to each project

## Benefits

| Benefit | Previous State | Current State |
|---------|---|---|
| **Package Versioning** | Separate versions per .csproj | Single version source (props file) |
| **Version Mismatches** | ⚠️ Possible drift | ✅ Impossible - single source of truth |
| **Update Process** | Edit multiple .csproj files | Edit ONE props file |
| **Consistency** | Manual verification needed | Automatic across all projects |
| **.csproj Cleanliness** | Long package lists | Clean, minimal package sections |
| **New Packages** | Add to each project separately | Add once to props file |

## Adding/Updating Packages

### Add a New Package

1. **Determine package scope:**
   - Shared? → Add to shared ItemGroup
   - Client only? → Add to client ItemGroup (with net9 condition)
   - Server only? → Add to server ItemGroup (with net10 condition)

2. **Edit `Directory.Build.props`:**
   ```xml
   <ItemGroup Label="Client Packages - .NET 9.0" Condition="$(TargetFramework.StartsWith('net9'))">
     <PackageReference Include="NewPackage.Name" Version="X.Y.Z" />
   </ItemGroup>
   ```

3. **Restore packages:**
   ```powershell
   dotnet restore
   ```

4. **Both projects automatically get the new package** ✅

### Update an Existing Package Version

1. **Edit ONE line in `Directory.Build.props`:**
   ```xml
   <!-- Before -->
   <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
   
   <!-- After -->
   <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
   ```

2. **Restore packages:**
   ```powershell
   dotnet restore
   ```

3. **Both projects automatically use the new version** ✅

**No need to edit .csproj files!**

## Build Verification

### Build the Solution
```powershell
dotnet build "AcademicSentinel.sln"
```

Expected output:
```
Build succeeded with X warning(s) in Y.Zs
```

### Build Individual Projects
```powershell
# Build client
dotnet build "SecureAssessmentClient.csproj"

# Build server
cd AcademicSentinel.Server
dotnet build
```

### Verify Package Resolution
```powershell
dotnet list package
```

Shows all packages with their versions across the solution.

## Project Files Status

### `SecureAssessmentClient.csproj`
**Before (30+ lines of packages):**
```xml
<ItemGroup>
  <PackageReference Include="log4net" Version="2.0.15" />
  <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.10" />
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <!-- ... more packages ... -->
</ItemGroup>
```

**After (single comment):**
```xml
<!-- All packages managed centrally in Directory.Build.props -->
```

### `AcademicSentinel.Server.csproj`
**Before (40+ lines of packages with special metadata):**
```xml
<ItemGroup>
  <PackageReference Include="BCrypt.Net-Next" Version="4.1.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.5" />
  <!-- ... more packages ... -->
</ItemGroup>
```

**After (single comment):**
```xml
<!-- All packages managed centrally in Directory.Build.props -->
```

## Framework Compatibility

The solution handles different .NET versions gracefully:

| Project | Target Framework | Applied Packages |
|---------|---|---|
| **SecureAssessmentClient** | net9.0-windows7.0 | Shared + Client (.NET 9) |
| **AcademicSentinel.Server** | net10.0 | Shared + Server (.NET 10) |

Each project gets **only** the packages it can use, preventing compatibility errors like "Package X doesn't support net9.0".

## Special Package Metadata

Some packages require special handling (beyond just version):

### `Microsoft.EntityFrameworkCore.Tools`
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.5">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Why:** This package is build-time only and shouldn't be deployed. The metadata ensures it's handled correctly.

This metadata is preserved automatically in `Directory.Build.props`.

## Troubleshooting

### Issue: "Package X is not compatible with framework Y"

**Cause:** The conditional in `Directory.Build.props` is incorrect or missing.

**Solution:** Verify the condition matches the project's `TargetFramework`:
- `.NET 9.0` projects → `Condition="$(TargetFramework.StartsWith('net9'))"`
- `.NET 10.0` projects → `Condition="$(TargetFramework.StartsWith('net10'))"`

### Issue: "Package not found after editing props file"

**Solution:** Run `dotnet restore` to download packages:
```powershell
dotnet restore
```

### Issue: "IntelliSense not showing new packages"

**Solution:** Rebuild the solution:
```powershell
dotnet clean
dotnet build
```

Or restart Visual Studio to refresh IntelliSense cache.

## Future Enhancement: Transitive Dependencies

Currently all packages are explicit. For even cleaner management, consider:
1. Identifying minimal set of top-level packages
2. Allowing NuGet to manage transitive dependencies
3. Documenting rationale for each package

Example: If `Microsoft.AspNetCore.SignalR.Core` pulls in `Newtonsoft.Json` transitively, we might remove the explicit reference.

## Version Pinning Strategy

**Current Approach:** Specific semantic versions (e.g., 10.0.5)
- ✅ Reproducible builds
- ✅ No surprises from patch updates
- ✅ Full control over when to update

**Alternative:** Floating versions (e.g., 10.0.*)
- ✅ Automatic patch updates
- ⚠️ Less control
- ⚠️ Can break unexpectedly

**Recommendation:** Keep current approach (specific versions) until thesis is complete, then migrate to floating versions for production maintenance.

## Documentation References

- **Package Source:** `Directory.Build.props` in workspace root
- **Previous Build Fix:** `BUILD_FIX_FINAL.md` (explains why each package is needed)
- **Quick Build Reference:** `QUICK_START_GUIDE.md` (build commands)
- **Solution Structure:** `AcademicSentinel.sln` (project references)

## Deployment Checklist

✅ All packages defined in `Directory.Build.props`
✅ Client project (.NET 9) gets only compatible packages
✅ Server project (.NET 10) gets only compatible packages
✅ No duplicate package definitions in .csproj files
✅ Shared packages apply to both projects
✅ Full solution builds with `dotnet build AcademicSentinel.sln`
✅ Individual projects build independently
✅ No package version conflicts

---

**Last Updated:** Session 3 - Unified Package Management Complete
**Status:** ✅ PRODUCTION READY
**Next Action:** Integration testing with Option 9 server connection test
