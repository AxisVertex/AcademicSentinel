# 📸 Image Storage System Documentation

## Overview

The AcademicSentinel PSS now includes a comprehensive image storage system for:
- **Student Profile Pictures** - Avatar/profile images for each student
- **Teacher Profile Pictures** - Avatar/profile images for each instructor
- **Room Icons/Images** - Cover images or icons for each monitoring room

This system handles secure storage, retrieval, validation, and metadata tracking.

---

## Architecture

### Components

```
┌─────────────────────────────────┐
│   Frontend (SAC/IMC)            │
│   Upload Images                 │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│   ImagesController              │
│   - Upload endpoints            │
│   - Retrieval endpoints         │
│   - Delete endpoints            │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│   IImageStorageService          │
│   - File validation             │
│   - Save to disk                │
│   - Retrieve from disk          │
│   - Delete from disk            │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│   File System                   │
│   /wwwroot/images/profiles/     │
│   /wwwroot/images/rooms/        │
│                                 │
│   Database (User/Room Models)   │
│   - Image URLs                  │
│   - Image metadata              │
└─────────────────────────────────┘
```

---

## Database Models

### User Model - Image Fields

```csharp
public class User
{
    // ... existing fields ...

    // Profile image storage
    public string? ProfileImageUrl { get; set; }           // URL to stored image
    public string? ProfileImagePath { get; set; }          // Local file path
    public string? ProfileImageContentType { get; set; }   // MIME type (image/png, etc.)
    public long? ProfileImageSize { get; set; }            // File size in bytes
    public DateTime? ProfileImageUploadedAt { get; set; }  // Upload timestamp
}
```

### Room Model - Image Fields

```csharp
public class Room
{
    // ... existing fields ...

    // Room image/icon storage
    public string? RoomImageUrl { get; set; }              // URL to stored image
    public string? RoomImagePath { get; set; }             // Local file path
    public string? RoomImageContentType { get; set; }      // MIME type (image/png, etc.)
    public long? RoomImageSize { get; set; }               // File size in bytes
    public DateTime? RoomImageUploadedAt { get; set; }     // Upload timestamp
}
```

---

## API Endpoints

### Profile Image Endpoints

#### Upload Profile Image
```
POST /api/images/profile
Authorization: Bearer {token}
Content-Type: multipart/form-data

Parameters:
  - image: IFormFile (required)

Response (200 OK):
{
  "success": true,
  "message": "Profile image uploaded successfully.",
  "url": "/images/profiles/user_5_guid123.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 245600,
  "uploadedAt": "2024-01-15T10:30:00Z"
}

Error Responses:
  - 400 Bad Request: Invalid file size, type, or format
  - 401 Unauthorized: Missing authentication
  - 404 Not Found: User not found
```

#### Get User Profile Image
```
GET /api/images/profile/{userId}
Authorization: Optional (AllowAnonymous)

Response (200 OK):
  - Binary image file with appropriate Content-Type header

Error Responses:
  - 404 Not Found: User not found or has no image
```

#### Delete Profile Image
```
DELETE /api/images/profile
Authorization: Bearer {token}

Response (200 OK):
{
  "message": "Profile image deleted successfully."
}

Error Responses:
  - 400 Bad Request: Failed to delete image
  - 401 Unauthorized: Missing authentication
  - 404 Not Found: User not found
```

#### Get Current User Profile (with image URL)
```
GET /api/images/profile
Authorization: Bearer {token}

Response (200 OK):
{
  "id": 5,
  "email": "student@university.edu",
  "role": "Student",
  "createdAt": "2024-01-10T08:00:00Z",
  "profileImageUrl": "/images/profiles/user_5_guid123.jpg",
  "profileImageUploadedAt": "2024-01-15T10:30:00Z"
}
```

### Room Image Endpoints

#### Upload Room Image
```
POST /api/images/room/{roomId}
Authorization: Bearer {token} (Instructor only)
Content-Type: multipart/form-data

Parameters:
  - roomId: int (route parameter)
  - image: IFormFile (required)

Response (200 OK):
{
  "success": true,
  "message": "Room image uploaded successfully.",
  "url": "/images/rooms/room_3_guid456.png",
  "contentType": "image/png",
  "sizeBytes": 512000,
  "uploadedAt": "2024-01-15T11:45:00Z"
}

Error Responses:
  - 400 Bad Request: Invalid file or instructor not owner
  - 401 Unauthorized: Missing authentication
  - 403 Forbidden: Not the instructor of this room
  - 404 Not Found: Room not found
```

#### Get Room Image
```
GET /api/images/room/{roomId}
Authorization: Optional (AllowAnonymous)

Response (200 OK):
  - Binary image file with appropriate Content-Type header

Error Responses:
  - 404 Not Found: Room not found or has no image
```

#### Delete Room Image
```
DELETE /api/images/room/{roomId}
Authorization: Bearer {token} (Instructor only)

Parameters:
  - roomId: int (route parameter)

Response (200 OK):
{
  "message": "Room image deleted successfully."
}

Error Responses:
  - 400 Bad Request: Failed to delete image
  - 401 Unauthorized: Missing authentication
  - 403 Forbidden: Not the instructor of this room
  - 404 Not Found: Room not found
```

#### Get Room Details with Image URL
```
GET /api/images/room/{roomId}/details
Authorization: Bearer {token}

Response (200 OK):
{
  "id": 3,
  "subjectName": "Advanced Mathematics",
  "instructorId": 2,
  "status": "Active",
  "enrollmentCode": "ABC123",
  "createdAt": "2024-01-10T09:00:00Z",
  "roomImageUrl": "/images/rooms/room_3_guid456.png",
  "roomImageUploadedAt": "2024-01-15T11:45:00Z"
}
```

---

## File Storage Details

### Storage Locations

```
ProjectRoot/
  └─ wwwroot/
     └─ images/
        ├─ profiles/
        │  ├─ user_1_guid123.jpg
        │  ├─ user_2_guid456.png
        │  └─ user_5_guid789.jpg
        └─ rooms/
           ├─ room_1_guid111.png
           ├─ room_3_guid222.jpg
           └─ room_7_guid333.webp
```

### Filename Format

- **Profile Images:** `user_{userId}_{uniqueGuid}.{extension}`
- **Room Images:** `room_{roomId}_{uniqueGuid}.{extension}`

GUID ensures uniqueness and prevents naming collisions when images are replaced.

---

## File Validation Rules

### Allowed File Types
- ✅ JPG/JPEG (`image/jpeg`)
- ✅ PNG (`image/png`)
- ✅ GIF (`image/gif`)
- ✅ WebP (`image/webp`)

### File Size Limits
- Maximum: **5 MB** (5,242,880 bytes)

### Validation Checks
1. File must not be empty
2. File must be under 5 MB
3. File extension must be in allowed list
4. File MIME type must be in allowed list
5. MIME type must match extension

### Example Validation Response

```
POST /api/images/profile
{
  "file": "large_image.jpg" (6 MB)
}

Response (400 Bad Request):
{
  "error": "File size exceeds maximum allowed size of 5 MB."
}
```

---

## Usage Examples

### React/TypeScript (Frontend)

#### Upload Profile Image

```typescript
async function uploadProfileImage(file: File): Promise<void> {
  const formData = new FormData();
  formData.append('image', file);

  const response = await fetch('/api/images/profile', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    },
    body: formData
  });

  const result = await response.json();
  console.log('Image URL:', result.url);
  // Update UI with: /images/profiles/user_5_guid123.jpg
}
```

#### Upload Room Image

```typescript
async function uploadRoomImage(roomId: number, file: File): Promise<void> {
  const formData = new FormData();
  formData.append('image', file);

  const response = await fetch(`/api/images/room/${roomId}`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    },
    body: formData
  });

  const result = await response.json();
  console.log('Room image URL:', result.url);
}
```

#### Get Current User Profile with Image

```typescript
async function getCurrentUserProfile() {
  const response = await fetch('/api/images/profile', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  const user = await response.json();
  // User object includes profileImageUrl
  return user;
}
```

#### Display Image

```typescript
// Direct image display (no auth needed)
<img src="/images/profiles/user_5_guid123.jpg" alt="User" />

// Or using URL from API response
const user = await getCurrentUserProfile();
if (user.profileImageUrl) {
  <img src={user.profileImageUrl} alt={user.email} />
}
```

#### Delete Profile Image

```typescript
async function deleteProfileImage(): Promise<void> {
  const response = await fetch('/api/images/profile', {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  if (response.ok) {
    console.log('Image deleted successfully');
    // Update UI to remove profile image
  }
}
```

### cURL Examples

#### Upload Profile Image

```bash
curl -X POST http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "image=@/path/to/profile.jpg"
```

#### Get Profile Image

```bash
curl http://localhost:5000/api/images/profile/5 \
  --output profile_picture.jpg
```

#### Upload Room Image

```bash
curl -X POST http://localhost:5000/api/images/room/3 \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "image=@/path/to/room_icon.png"
```

#### Delete Profile Image

```bash
curl -X DELETE http://localhost:5000/api/images/profile \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Security Considerations

### File Validation
- ✅ File size limited to 5 MB
- ✅ Only image MIME types allowed
- ✅ File extension verified
- ✅ MIME type verification

### Access Control
- ✅ Profile images: Users can only upload/delete their own
- ✅ Room images: Only instructors who own the room can upload/delete
- ✅ Image retrieval: Can be public (AllowAnonymous)

### Storage Security
- ✅ Files stored outside web root (in wwwroot for serving)
- ✅ Unique filenames prevent file discovery/enumeration
- ✅ Old images deleted when replaced

### Best Practices
- Always validate on the backend (done)
- Use HTTPS for file transfers (configure in production)
- Implement rate limiting for uploads (recommended)
- Regular backup of image storage directory
- Monitor disk space usage

---

## Migration Notes

If upgrading an existing database:

```csharp
// Add migration
dotnet ef migrations add AddImageStorageFields

// Update database
dotnet ef database update
```

New columns will be nullable for existing users/rooms.

---

## Troubleshooting

### Images Not Persisting
- Check wwwroot/images/ directory exists
- Verify write permissions on the directory
- Ensure StaticFiles middleware is added to Program.cs

### 404 on Image Retrieval
- Confirm image file exists in storage directory
- Check that database record has correct URL
- Verify filename matches pattern: `user_{id}_*.` or `room_{id}_*.`

### Upload Failed with 400 Error
- Check file size is under 5 MB
- Verify file type is supported (JPG, PNG, GIF, WebP)
- Confirm MIME type matches file extension

### Authorization Issues
- Verify JWT token is valid and not expired
- For room images: Confirm logged-in instructor owns the room
- Check Authorization header format: `Bearer {token}`

---

## Related Documentation

- [ROOM_STATE_VALIDATION.md](ROOM_STATE_VALIDATION.md) - Workflow constraints
- [API_QUICK_REFERENCE.md](API_QUICK_REFERENCE.md) - All endpoints
- [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) - Feature overview
