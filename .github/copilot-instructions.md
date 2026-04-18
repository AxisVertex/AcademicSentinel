# Copilot Instructions

## Project Guidelines
- Room state validation is critical to AcademicSentinel's workflow. Students cannot join/enroll before the teacher starts the session. Implementation enforces: 1) Enrollment only in "Pending" state, 2) JoinLiveExam only in "Active" state, 3) Assignment only in "Pending" state. Validations added to EnrollByCode endpoint and JoinLiveExam SignalR method with clear error messages.
- Implement SAC detection across multiple files/modules rather than a single file. Organize the implementation in step-by-step phases with careful data-flow handling. Ensure RTFM detects only actual window switching events, excluding focus-loss baseline/foreground-loss events, and logs details of the switched-to window.

## Image Storage Implementation
- Image storage for student/teacher profiles and room icons is implemented. 
- System stores images in `wwwroot/images/{profiles|rooms}/` with database metadata in User and Room models. 
- Each user and room can have one profile/room image. 
- `IImageStorageService` handles validation (5MB max, image types only), file operations, and cleanup. 
- `ImagesController` exposes 8 endpoints for upload, retrieval, and deletion. 
- All images are publicly accessible via GET endpoints (AllowAnonymous). 
- Profile uploads require user authentication, while room uploads require instructor ownership. 
- File naming uses the pattern `user_{id}_{guid}` and `room_{id}_{guid}` to prevent collisions.
- User and Room models have been updated to include 5 image fields each. All 10 new database columns are nullable, ensuring existing data remains safe. 
- To update the database, execute the following commands: 
  1. `cd AcademicSentinel.Server`
  2. `dotnet ef migrations add AddImageStorageFields`
  3. `dotnet ef database update`
- After updating the database, create the `wwwroot/images/{profiles|rooms}` directories. 
- Migration is auto-generated; no manual edits are required. 
- Build: SUCCESSFUL. AppDbContext.cs requires no changes.