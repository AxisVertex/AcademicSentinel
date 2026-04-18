# 📊 Database Migration Guide - Image Storage Fields

## Quick Answer: How to Update Your Database

You need to create and apply an Entity Framework Core migration. Here are the step-by-step instructions:

---

## Step 1: Open Package Manager Console

In Visual Studio:
1. Go to **Tools** → **NuGet Package Manager** → **Package Manager Console**
   - Or press: `Ctrl + ` (backtick)

2. Make sure the default project is set to **AcademicSentinel.Server**

---

## Step 2: Create the Migration

Run this command in the Package Manager Console:

```powershell
dotnet ef migrations add AddImageStorageFields
```

Or if you prefer using the PMC directly:

```
Add-Migration AddImageStorageFields
```

**What this does:**
- Detects changes in your User and Room models (the 10 new image fields)
- Creates a new migration file in `Migrations/` folder
- Names it with timestamp: `[timestamp]_AddImageStorageFields.cs`

**You should see:**
```
Build started...
Build succeeded.
Migration "20240115123456_AddImageStorageFields" added to project.
```

---

## Step 3: Apply the Migration to Database

Run this command:

```powershell
dotnet ef database update
```

Or in PMC:

```
Update-Database
```

**What this does:**
- Executes all pending migrations
- Adds the 10 new columns to User and Room tables
- Updates your database schema

**You should see:**
```
Build started...
Build succeeded.
Applying migration '20240115123456_AddImageStorageFields'...
Done.
```

---

## Alternative: Using PowerShell Terminal (Your Preference)

Since you prefer PowerShell, you can run these from a terminal instead:

```powershell
# Navigate to your project
cd C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server

# Create migration
dotnet ef migrations add AddImageStorageFields

# Apply migration
dotnet ef database update
```

---

## What Changed in the Models

### User.cs - Added 5 Fields
```csharp
public string? ProfileImageUrl { get; set; }           // /images/profiles/user_5_guid.jpg
public string? ProfileImagePath { get; set; }          // C:\project\wwwroot\images\profiles\...
public string? ProfileImageContentType { get; set; }   // image/jpeg, image/png, etc.
public long? ProfileImageSize { get; set; }            // 245600 (in bytes)
public DateTime? ProfileImageUploadedAt { get; set; }  // 2026-01-15T10:30:00Z
```

### Room.cs - Added 5 Fields
```csharp
public string? RoomImageUrl { get; set; }              // /images/rooms/room_3_guid.png
public string? RoomImagePath { get; set; }             // C:\project\wwwroot\images\rooms\...
public string? RoomImageContentType { get; set; }      // image/jpeg, image/png, etc.
public long? RoomImageSize { get; set; }               // 512000 (in bytes)
public DateTime? RoomImageUploadedAt { get; set; }     // 2026-01-15T11:45:00Z
```

---

## Database Schema Changes

After applying the migration, your tables will have these new columns:

### Users Table
```sql
-- New columns added
ALTER TABLE Users ADD ProfileImageUrl NVARCHAR(MAX) NULL;
ALTER TABLE Users ADD ProfileImagePath NVARCHAR(MAX) NULL;
ALTER TABLE Users ADD ProfileImageContentType NVARCHAR(MAX) NULL;
ALTER TABLE Users ADD ProfileImageSize BIGINT NULL;
ALTER TABLE Users ADD ProfileImageUploadedAt DATETIME2 NULL;
```

### Rooms Table
```sql
-- New columns added
ALTER TABLE Rooms ADD RoomImageUrl NVARCHAR(MAX) NULL;
ALTER TABLE Rooms ADD RoomImagePath NVARCHAR(MAX) NULL;
ALTER TABLE Rooms ADD RoomImageContentType NVARCHAR(MAX) NULL;
ALTER TABLE Rooms ADD RoomImageSize BIGINT NULL;
ALTER TABLE Rooms ADD RoomImageUploadedAt DATETIME2 NULL;
```

**All columns are nullable (NULL)**, so existing data won't be affected.

---

## AppDbContext Verification

Your `AppDbContext.cs` is already correctly configured with all DbSets:

```csharp
public DbSet<User> Users { get; set; }           // ✅ Image fields will be added
public DbSet<Room> Rooms { get; set; }           // ✅ Image fields will be added
public DbSet<RoomEnrollment> RoomEnrollments { get; set; }
public DbSet<SessionAssignment> SessionAssignments { get; set; }
public DbSet<SessionParticipant> SessionParticipants { get; set; }
public DbSet<MonitoringEvent> MonitoringEvents { get; set; }
public DbSet<RoomDetectionSettings> RoomDetectionSettings { get; set; }
public DbSet<ViolationLog> ViolationLogs { get; set; }
public DbSet<RiskSummary> RiskSummaries { get; set; }
```

✅ **No changes needed** - AppDbContext already has the right configuration!

---

## Step-by-Step Terminal Commands

If you want to use PowerShell terminal (your preference), here's the complete process:

```powershell
# Open PowerShell
# Navigate to the server project directory
cd "C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server"

# Verify you're in the right directory
pwd  # Should show: C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server
ls   # Should show: AcademicSentinel.Server.csproj

# Create the migration
dotnet ef migrations add AddImageStorageFields

# Apply the migration
dotnet ef database update

# Verify the migration was applied
dotnet ef migrations list
```

---

## Expected File Created

After running `Add-Migration`, a new file will be created:

**File location:** `AcademicSentinel.Server/Migrations/[timestamp]_AddImageStorageFields.cs`

**File structure:**
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace AcademicSentinel.Server.Migrations
{
    public partial class AddImageStorageFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Creates the 10 new columns
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImagePath",
                table: "Users",
                type: "TEXT",
                nullable: true);

            // ... (continues for all 10 fields)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback code (optional)
        }
    }
}
```

You don't need to edit this file - it's auto-generated!

---

## Database File Location

Your SQLite database is stored at:

**Default location (from appsettings.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=academicsentinel.db"
  }
}
```

**Full path:** `C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server\academicsentinel.db`

After running `Update-Database`, this file will have the new columns.

---

## Troubleshooting

### Issue: "No migrations have been applied to the database"

**Solution:** Run this to create and apply the initial migration:
```powershell
dotnet ef database update
```

### Issue: "Could not locate .csproj or .sln"

**Solution:** Make sure you're in the correct directory:
```powershell
cd "C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server"
```

### Issue: EF Tools not installed

**Solution:** Install Entity Framework Core tools:
```powershell
dotnet tool install --global dotnet-ef
```

### Issue: Migration already exists error

**Solution:** List migrations to check:
```powershell
dotnet ef migrations list
```

If the migration already exists, you can:
- Apply it if not applied: `dotnet ef database update`
- Or remove and recreate it:
  ```powershell
  dotnet ef migrations remove
  dotnet ef migrations add AddImageStorageFields
  dotnet ef database update
  ```

### Issue: "The model has changed but no migration was created"

**Solution:** Entity Framework can't detect changes. Try:
```powershell
# Force rebuild
dotnet build

# Then create migration with verbose output
dotnet ef migrations add AddImageStorageFields -v
```

---

## Verification: How to Check If Migration Was Applied

After running `Update-Database`, verify with:

```powershell
# List all migrations
dotnet ef migrations list

# You should see:
# 20240110120000_InitialCreate
# 20240115123456_AddImageStorageFields  <-- Should show as "Applied"
```

Or using SQL:
```powershell
# Open the database in SQLite (if installed)
sqlite3 academicsentinel.db

# Check the Users table
sqlite> .schema Users

# You should see the new columns:
# ProfileImageUrl TEXT,
# ProfileImagePath TEXT,
# ProfileImageContentType TEXT,
# ProfileImageSize INTEGER,
# ProfileImageUploadedAt DATETIME,
```

---

## The Complete Process (Summary)

1. **Open Package Manager Console** or PowerShell terminal
2. **Navigate to project directory** (if using PowerShell)
3. **Create migration:** `dotnet ef migrations add AddImageStorageFields`
4. **Apply migration:** `dotnet ef database update`
5. **Verify:** Check that database has new columns
6. **Done!** Your database is ready for image storage

---

## What Happens Automatically

When you run `dotnet ef database update`:

✅ EF Core compares your models to the last migration
✅ Finds User and Room models with new image fields
✅ Generates SQL to add new columns
✅ Executes SQL against your SQLite database
✅ Updates the `__EFMigrationsHistory` table (internal tracking)
✅ Updates `DesignTimeDbContextFactory` 

**No manual SQL needed!** - Entity Framework handles everything.

---

## Next Steps After Migration

1. **Create image directories:**
   ```powershell
   mkdir "wwwroot\images\profiles"
   mkdir "wwwroot\images\rooms"
   ```

2. **Start the application:**
   ```powershell
   dotnet run
   ```

3. **Test image upload endpoints:**
   ```bash
   curl -X POST http://localhost:5000/api/images/profile \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -F "image=@photo.jpg"
   ```

---

## Quick Reference Commands

```powershell
# All-in-one command (from your project directory)
cd "C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server" && `
  dotnet ef migrations add AddImageStorageFields && `
  dotnet ef database update

# Or separate commands:
dotnet ef migrations add AddImageStorageFields
dotnet ef database update

# Check status:
dotnet ef migrations list
```

---

## Database Backup (Recommended Before Migration)

Before running the migration, backup your database:

```powershell
Copy-Item "academicsentinel.db" "academicsentinel.db.backup"
```

Then if something goes wrong, you can restore:

```powershell
Remove-Item "academicsentinel.db"
Rename-Item "academicsentinel.db.backup" "academicsentinel.db"
```

---

That's it! Your database will be updated with the image storage fields. 🎉
