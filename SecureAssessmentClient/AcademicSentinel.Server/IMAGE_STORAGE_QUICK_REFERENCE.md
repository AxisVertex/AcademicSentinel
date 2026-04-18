# 📸 Image Storage Quick Reference

## At a Glance

| Aspect | Details |
|--------|---------|
| **Storage Location** | `wwwroot/images/{profiles\|rooms}/` |
| **Max File Size** | 5 MB |
| **Allowed Types** | JPG, PNG, GIF, WebP |
| **Database Tables** | User, Room (5 new fields each) |
| **API Endpoints** | 8 total (4 profile, 4 room) |
| **Service** | `IImageStorageService` |
| **Controller** | `ImagesController` |
| **DTOs** | 3 new (ImageUploadResponseDto, UserProfileDto, RoomWithImageDto) |
| **Build Status** | ✅ SUCCESSFUL |

## 8 API Endpoints

### Profile (4 endpoints)
```
POST   /api/images/profile          Upload profile image
GET    /api/images/profile/{id}     Get profile image
GET    /api/images/profile          Get user profile + image URL
DELETE /api/images/profile          Delete profile image
```

### Room (4 endpoints)
```
POST   /api/images/room/{id}        Upload room image
GET    /api/images/room/{id}        Get room image
GET    /api/images/room/{id}/details Get room + image URL
DELETE /api/images/room/{id}        Delete room image
```

## Database Fields Added

### User Model
- `ProfileImageUrl` - Public URL
- `ProfileImagePath` - File path
- `ProfileImageContentType` - MIME type
- `ProfileImageSize` - File size
- `ProfileImageUploadedAt` - Timestamp

### Room Model
- `RoomImageUrl` - Public URL
- `RoomImagePath` - File path
- `RoomImageContentType` - MIME type
- `RoomImageSize` - File size
- `RoomImageUploadedAt` - Timestamp

## File Naming Pattern

```
Profile: user_{userId}_{uniqueGuid}.{ext}
Example: user_5_a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6.jpg

Room:    room_{roomId}_{uniqueGuid}.{ext}
Example: room_3_x9y8z7w6-v5u4t3s2-r1q0p9o8.png
```

## Validation Rules

```
✅ File size: 0 < size ≤ 5 MB
✅ File type: image/jpeg, image/png, image/gif, image/webp
✅ Extension: .jpg, .jpeg, .png, .gif, .webp
✅ MIME type must match extension
❌ Empty files rejected
❌ Non-image files rejected
```

## Authorization

| Operation | User | Profile Owner | Room Owner |
|-----------|------|---------------|-----------|
| Upload profile | ✅ Self only | - | - |
| Delete profile | ✅ Self only | - | - |
| View profile | ✅ Public | - | - |
| Upload room | - | - | ✅ Owner |
| Delete room | - | - | ✅ Owner |
| View room | ✅ Public | - | - |

## Upload Request Format

```
POST /api/images/profile
Authorization: Bearer {token}
Content-Type: multipart/form-data

Form Data:
  image: File (binary)
```

## Upload Response Format

```json
{
  "success": true,
  "message": "Profile image uploaded successfully.",
  "url": "/images/profiles/user_5_abc123def456.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 245600,
  "uploadedAt": "2026-01-15T10:30:00Z"
}
```

## Error Responses

| Error | Status | Message |
|-------|--------|---------|
| File too large | 400 | "File size exceeds maximum allowed size of 5 MB." |
| Invalid type | 400 | "File type '.exe' is not allowed..." |
| Empty file | 400 | "File is empty." |
| Not owner | 403 | (Implicit - request forbidden) |
| Not found | 404 | "User not found." |
| Server error | 500 | Internal error during save |

## Frontend Usage (React)

### Upload
```typescript
const upload = async (file: File, token: string) => {
  const formData = new FormData();
  formData.append('image', file);

  const res = await fetch('/api/images/profile', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  });

  const data = await res.json();
  return data.url; // e.g., "/images/profiles/user_5_abc123.jpg"
};
```

### Display
```typescript
<img src="/images/profiles/user_5_abc123.jpg" alt="Profile" />
```

### Delete
```typescript
const del = async (token: string) => {
  await fetch('/api/images/profile', {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
};
```

## cURL Examples

### Upload
```bash
curl -X POST http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer TOKEN" \
  -F "image=@photo.jpg"
```

### Retrieve
```bash
curl http://localhost:5000/api/images/profile/5 \
  --output photo.jpg
```

### Delete
```bash
curl -X DELETE http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer TOKEN"
```

## Database Migration

```bash
dotnet ef migrations add AddImageStorageFields
dotnet ef database update
```

## Directory Structure Created

```
wwwroot/
└── images/
    ├── profiles/
    │   ├── user_1_guid.jpg
    │   ├── user_5_guid.png
    │   └── user_10_guid.webp
    └── rooms/
        ├── room_1_guid.jpg
        ├── room_3_guid.png
        └── room_7_guid.gif
```

## Files Modified/Created

| File | Status | Changes |
|------|--------|---------|
| Models/User.cs | UPDATED | +5 image fields |
| Models/Room.cs | UPDATED | +5 image fields |
| Services/ImageStorageService.cs | NEW | 400+ lines |
| Controllers/ImagesController.cs | NEW | 200+ lines, 8 endpoints |
| DTOs/AdditionalDTOs.cs | UPDATED | +3 DTOs |
| Program.cs | UPDATED | +service registration |
| IMAGE_STORAGE_GUIDE.md | NEW | Technical guide |
| IMAGE_STORAGE_IMPLEMENTATION.md | NEW | Implementation details |
| IMAGE_STORAGE_COMPLETE.md | NEW | This summary |
| DOCUMENTATION_INDEX.md | UPDATED | +image storage link |
| API_QUICK_REFERENCE.md | UPDATED | +8 endpoints |

## Testing

```bash
# Test valid image upload
curl -X POST http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer TOKEN" \
  -F "image=@valid.jpg"
# Expected: 200 OK with URL

# Test file too large (> 5MB)
curl -X POST http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer TOKEN" \
  -F "image=@large.jpg"
# Expected: 400 Bad Request

# Test invalid type
curl -X POST http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer TOKEN" \
  -F "image=@file.exe"
# Expected: 400 Bad Request

# Test public retrieval (no auth)
curl http://localhost:5000/api/images/profile/5
# Expected: 200 OK with image binary
```

## Key Features

✅ Automatic file cleanup on replacement  
✅ GUID-based unique filenames  
✅ Role-based access control  
✅ Comprehensive validation  
✅ MIME type verification  
✅ Public image serving  
✅ Database metadata tracking  
✅ Error handling & logging  

## Build Status

✅ **COMPILATION SUCCESSFUL** (0 errors, 0 warnings)

## Documentation

- **IMAGE_STORAGE_GUIDE.md** - Full technical reference
- **IMAGE_STORAGE_IMPLEMENTATION.md** - Implementation overview
- **IMAGE_STORAGE_COMPLETE.md** - Comprehensive summary
- **API_QUICK_REFERENCE.md** - Endpoint examples
- **DOCUMENTATION_INDEX.md** - Navigation guide
