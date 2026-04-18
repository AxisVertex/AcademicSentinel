# ⚡ Quick Database Update - 3 Steps

## The Absolute Quickest Way (Copy & Paste)

Open **PowerShell** and paste this:

```powershell
cd "C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server"
dotnet ef migrations add AddImageStorageFields
dotnet ef database update
```

**That's it!** Your database is updated. ✅

---

## Or Use Visual Studio Package Manager Console

1. In Visual Studio: **Tools** → **NuGet Package Manager** → **Package Manager Console**

2. Make sure default project = **AcademicSentinel.Server**

3. Paste these commands:

```
Add-Migration AddImageStorageFields
Update-Database
```

**Done!** ✅

---

## What Gets Added to Database?

### Users Table (5 new columns)
```
ProfileImageUrl           (TEXT)
ProfileImagePath          (TEXT)
ProfileImageContentType   (TEXT)
ProfileImageSize          (BIGINT)
ProfileImageUploadedAt    (DATETIME)
```

### Rooms Table (5 new columns)
```
RoomImageUrl              (TEXT)
RoomImagePath             (TEXT)
RoomImageContentType      (TEXT)
RoomImageSize             (BIGINT)
RoomImageUploadedAt       (DATETIME)
```

All columns are **nullable** - existing data unaffected ✅

---

## Status Check

After running the commands, verify migration was applied:

```powershell
dotnet ef migrations list
```

You should see:
```
20240115123456_AddImageStorageFields (Applied)
```

---

## What Was Changed Before Migration?

### Code Changes ✅ (DONE)
- ✅ User.cs - Added 5 image fields
- ✅ Room.cs - Added 5 image fields
- ✅ ImageStorageService.cs - Created
- ✅ ImagesController.cs - Created
- ✅ Program.cs - Service registered

### Database Changes ⏳ (NEEDS MIGRATION)
- ⏳ Add 5 columns to Users table
- ⏳ Add 5 columns to Rooms table

**→ Run the migration commands above to complete!**

---

## AppDbContext.cs Status

✅ **Already correct!** No changes needed.

Current DbSets (all present):
```csharp
public DbSet<User> Users { get; set; }
public DbSet<Room> Rooms { get; set; }
public DbSet<RoomEnrollment> RoomEnrollments { get; set; }
public DbSet<SessionAssignment> SessionAssignments { get; set; }
public DbSet<SessionParticipant> SessionParticipants { get; set; }
public DbSet<MonitoringEvent> MonitoringEvents { get; set; }
public DbSet<RoomDetectionSettings> RoomDetectionSettings { get; set; }
public DbSet<ViolationLog> ViolationLogs { get; set; }
public DbSet<RiskSummary> RiskSummaries { get; set; }
```

---

## After Migration Complete

Create the image directories:

```powershell
mkdir "wwwroot\images\profiles"
mkdir "wwwroot\images\rooms"
```

Then you're ready to use the API! 🎉

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| EF tools not found | `dotnet tool install --global dotnet-ef` |
| Wrong directory | `cd "C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server"` |
| Wrong project | Make sure `AcademicSentinel.Server.csproj` exists in current directory |
| Already migrated? | Run `dotnet ef migrations list` to check |

---

## Done! ✅

Your database is now ready for image storage!
