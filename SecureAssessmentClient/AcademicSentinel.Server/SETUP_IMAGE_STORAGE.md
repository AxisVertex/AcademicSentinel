# 🚀 START HERE - Image Storage Setup

## You Asked: "Did you change anything in the database?"

**Answer:** 
- ✅ **Code changes:** YES - Added image fields to User and Room models
- ❌ **Database changes:** NOT YET - You need to run the migration
- ✅ **AppDbContext.cs:** NO changes needed (already correct)

---

## What's the Current State?

### ✅ Completed
- User.cs updated with 5 image fields
- Room.cs updated with 5 image fields
- ImageStorageService.cs created (400+ lines)
- ImagesController.cs created (8 endpoints)
- Program.cs updated with service registration
- All documentation created
- Build: **SUCCESSFUL** ✅

### ⏳ Pending (Your Action Required)
- Database migration needs to be applied
- Image directories need to be created

---

## How to Update the Database? (3 Steps)

### Option 1: PowerShell Terminal (Your Preference) ⭐

```powershell
cd "C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server"
dotnet ef migrations add AddImageStorageFields
dotnet ef database update
```

### Option 2: Visual Studio Package Manager Console

1. **Tools** → **NuGet Package Manager** → **Package Manager Console**
2. Make sure default project = **AcademicSentinel.Server**
3. Run:
   ```
   Add-Migration AddImageStorageFields
   Update-Database
   ```

---

## What Does This Do?

| Step | Command | Action |
|------|---------|--------|
| 1 | `Add-Migration AddImageStorageFields` | Creates migration file (auto-generated) |
| 2 | `Update-Database` | Adds 10 new columns to database tables |

### Columns Added to Users Table
```
✅ ProfileImageUrl
✅ ProfileImagePath
✅ ProfileImageContentType
✅ ProfileImageSize
✅ ProfileImageUploadedAt
```

### Columns Added to Rooms Table
```
✅ RoomImageUrl
✅ RoomImagePath
✅ RoomImageContentType
✅ RoomImageSize
✅ RoomImageUploadedAt
```

All columns are **nullable** - existing data stays safe ✅

---

## Did We Change AppDbContext.cs?

**NO** ✅ 

The AppDbContext is already perfectly configured:

```csharp
public DbSet<User> Users { get; set; }                    ✅
public DbSet<Room> Rooms { get; set; }                    ✅
public DbSet<RoomEnrollment> RoomEnrollments { get; set; }
public DbSet<SessionAssignment> SessionAssignments { get; set; }
public DbSet<SessionParticipant> SessionParticipants { get; set; }
public DbSet<MonitoringEvent> MonitoringEvents { get; set; }
public DbSet<RoomDetectionSettings> RoomDetectionSettings { get; set; }
public DbSet<ViolationLog> ViolationLogs { get; set; }
public DbSet<RiskSummary> RiskSummaries { get; set; }
```

Entity Framework will automatically detect the model changes and apply them via migration.

---

## Complete Process Overview

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  1. You edited User.cs & Room.cs                           │
│     (added 5 image fields to each)          ✅ DONE        │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  2. Run: dotnet ef migrations add ...                      │
│     (creates migration file)                 ⏳ YOU DO THIS  │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  3. Run: dotnet ef database update                         │
│     (applies migration to database)          ⏳ YOU DO THIS  │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  4. Create image directories                              │
│     mkdir wwwroot/images/profiles                          │
│     mkdir wwwroot/images/rooms                ⏳ YOU DO THIS  │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  5. Database is ready for image storage      ✅ DONE!      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## All-in-One Command

Copy and paste this into PowerShell:

```powershell
cd "C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server"; `
dotnet ef migrations add AddImageStorageFields; `
dotnet ef database update; `
mkdir "wwwroot\images\profiles"; `
mkdir "wwwroot\images\rooms"; `
Write-Host "✅ Database migration complete!" -ForegroundColor Green
```

---

## Verify Migration Was Applied

After running the commands, verify:

```powershell
# List all migrations
dotnet ef migrations list

# Output should show:
# AddImageStorageFields (Applied)
```

---

## What Files Were Created/Modified?

### Code Files (Already Done ✅)
- Models/User.cs - UPDATED (added 5 image fields)
- Models/Room.cs - UPDATED (added 5 image fields)
- Services/ImageStorageService.cs - NEW (400+ lines)
- Controllers/ImagesController.cs - NEW (200+ lines)
- DTOs/AdditionalDTOs.cs - UPDATED (added 3 DTOs)
- Program.cs - UPDATED (service registration)

### Migration File (Will Be Created)
- Migrations/[timestamp]_AddImageStorageFields.cs (auto-generated)

### Documentation Files (Already Done ✅)
- QUICK_DATABASE_UPDATE.md
- DATABASE_MIGRATION_GUIDE.md
- IMAGE_STORAGE_GUIDE.md
- IMAGE_STORAGE_QUICK_REFERENCE.md
- IMAGE_STORAGE_IMPLEMENTATION.md
- IMAGE_STORAGE_COMPLETE.md

---

## Database File Location

**Your SQLite database:**
```
C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server\academicsentinel.db
```

After migration, it will have the 10 new columns.

---

## What About Existing Data?

✅ **Safe!** All new columns are nullable, so:
- Existing users keep their data
- Existing rooms keep their data
- No data loss
- No data corruption

---

## Next: Test the Image API

After the migration completes, test the endpoints:

```bash
# Upload profile image
curl -X POST http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "image=@photo.jpg"

# View profile image
curl http://localhost:5000/api/images/profile/5 \
  --output photo.jpg

# Get current user with image URL
curl http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Troubleshooting

### "dotnet: The term 'dotnet' is not recognized"
**Solution:** Install .NET 10 or add dotnet to PATH

### "The default startup project is not set"
**Solution:** In Package Manager Console, set default project to `AcademicSentinel.Server`

### "Migration already exists"
**Solution:** Run `dotnet ef migrations list` to check status

### "No migrations have been applied"
**Solution:** The database exists but migration hasn't run yet:
```powershell
dotnet ef database update
```

---

## Documentation Quick Links

📄 **Migration Help:**
- [QUICK_DATABASE_UPDATE.md](QUICK_DATABASE_UPDATE.md) - 2 minute read
- [DATABASE_MIGRATION_GUIDE.md](DATABASE_MIGRATION_GUIDE.md) - Full guide

📸 **Image Storage:**
- [IMAGE_STORAGE_QUICK_REFERENCE.md](IMAGE_STORAGE_QUICK_REFERENCE.md) - Quick ref
- [IMAGE_STORAGE_GUIDE.md](IMAGE_STORAGE_GUIDE.md) - Full guide

📋 **Navigation:**
- [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md) - All docs

---

## Summary

| Question | Answer |
|----------|--------|
| Did you change the database? | Not yet - need to run migration |
| Did you change AppDbContext.cs? | No - it was already correct ✅ |
| How to update the database? | Run 2 commands (see above) |
| Will existing data be lost? | No - all new columns are nullable |
| How long does it take? | 2-3 minutes |
| Build status? | ✅ SUCCESSFUL |

---

## Do This Now

1. **Open PowerShell**
2. **Copy this command:**
   ```powershell
   cd "C:\Users\Computer\Documents\CodeFolder\CSharp\AcademicSentinel\AcademicSentinel.Server" && dotnet ef migrations add AddImageStorageFields && dotnet ef database update
   ```
3. **Paste and press Enter**
4. **Create image directories:**
   ```powershell
   mkdir "wwwroot\images\profiles"
   mkdir "wwwroot\images\rooms"
   ```
5. **Done!** ✅

Your database is now ready for the image storage feature! 🎉
