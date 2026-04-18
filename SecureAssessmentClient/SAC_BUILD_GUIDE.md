# STUDENT SIDE SECURE ASSESSMENT CLIENT (SAC) - STEP-BY-STEP BUILD GUIDE

**Version:** 1.0
**Target Framework:** .NET 10
**Language:** C#
**UI Framework:** WPF (Windows Presentation Foundation)
**IDE:** Visual Studio 2026
**Target OS:** Windows 10+

---

## 🔄 DEVELOPMENT SPLIT & COORDINATION

> **⚠️ IMPORTANT: This is a **team development project** with role-based phase allocation.**

### Backend Developer (Your Current Focus)
- **Responsible For:** Phases 1-3, 5-9
- **Focus:** Authentication, SignalR communication, monitoring services, detection engines, event logging
- **Deliverable:** Complete backend API-ready services with no UI dependencies
- **Timeline:** Phases 1-3 completed ✅ → Continue to Phases 5-9

### UI Developer (Co-Developer)
- **Responsible For:** Phases 4, 10
- **Focus:** WPF Windows, Views, Converters, user interface
- **Deliverable:** UI layer that consumes backend services via Models and Service interfaces
- **Coordination:** Will integrate once backend services are ready

### ⚙️ Integration Points
- **Phase 3** (AuthService): Provides authentication interface for UI LoginWindow (Phase 4)
- **Phase 5** (SignalRService): Provides real-time communication for UI MonitoringWindow (Phase 10)
- **Phase 6-9** (Detection/Logger Services): Services run independently; UI will bind to event data via Models
- **Data Models** (Phases 2): Already created and shared - no UI needed to test these

### 📝 Co-Developer Handoff Checklist
- [ ] Backend Phases 1-3 complete and documented
- [ ] Phases 5-9 services fully implemented with public interfaces
- [ ] Models and DTOs finalized (Phase 2) ✅
- [ ] All services have clear public method signatures for UI consumption
- [ ] Example configuration and constants documented
- [ ] **Then:** UI Developer integrates Phase 4 & 10 using backend services

---

## TABLE OF CONTENTS
1. [Project Setup & File Structure](#project-setup--file-structure)
2. [Phase 1: Project Creation & Dependencies](#phase-1-project-creation--dependencies)
3. [Phase 2: Data Models & DTOs](#phase-2-data-models--dtos)
4. [Phase 3: Authentication & Login](#phase-3-authentication--login)
5. [Phase 4: Room Discovery & UI](#phase-4-room-discovery--ui)
6. [Phase 5: SignalR Connection](#phase-5-signalr-connection)
7. [Phase 6: Environment Integrity Detection](#phase-6-environment-integrity-detection)
8. [Phase 7: Behavioral Monitoring Modules](#phase-7-behavioral-monitoring-modules)
9. [Phase 8: Decision Engine & Risk Classification](#phase-8-decision-engine--risk-classification)
10. [Phase 9: Event Logging & Transmission](#phase-9-event-logging--transmission)
11. [Phase 10: UI Integration & Final Testing](#phase-10-ui-integration--final-testing)

---

## PROJECT SETUP & FILE STRUCTURE

### CREATE SOLUTION IN VISUAL STUDIO 2026

**Steps:**
1. Open Visual Studio 2026
2. Create New Project → WPF Application (.NET)
3. Project Name: `SecureAssessmentClient`
4. Solution Name: `SystemFourCUDA` (or your main solution)
5. Framework: .NET 10
6. Location: `e:\Darryll pogi\FEU files Darryll\3rd Year\3rd Year Second Sem\FOR Thesis\Codes\SystemFourCUDA`

### RECOMMENDED FOLDER STRUCTURE

```
SecureAssessmentClient/
├── Models/
│   ├── Authentication/
│   │   ├── LoginRequest.cs
│   │   ├── LoginResponse.cs
│   │   └── AuthToken.cs
│   ├── Room/
│   │   ├── RoomDto.cs
│   │   ├── RoomStatus.cs
│   │   └── JoinRoomRequest.cs
│   └── Monitoring/
│       ├── MonitoringEvent.cs
│       ├── RiskLevel.cs
│       └── DetectionSettings.cs
├── Services/
│   ├── AuthService.cs
│   ├── RoomService.cs
│   ├── SignalRService.cs
│   ├── DetectionService/
│   │   ├── EnvironmentIntegrityService.cs
│   │   ├── BehavioralMonitoringService.cs
│   │   ├── DecisionEngineService.cs
│   │   └── EventLoggerService.cs
│   └── ApiService.cs
├── UI/
│   ├── Windows/
│   │   ├── LoginWindow.xaml
│   │   ├── LoginWindow.xaml.cs
│   │   ├── RoomDashboardWindow.xaml
│   │   ├── RoomDashboardWindow.xaml.cs
│   │   ├── MonitoringWindow.xaml
│   │   └── MonitoringWindow.xaml.cs
│   ├── Views/
│   │   ├── RoomOrbControl.xaml
│   │   ├── RoomOrbControl.xaml.cs
│   │   ├── MonitoringIndicator.xaml
│   │   └── MonitoringIndicator.xaml.cs
│   └── Converters/
│       ├── RoomStatusToBrushConverter.cs
│       └── ConnectionStatusConverter.cs
├── Utilities/
│   ├── Constants.cs
│   ├── ApiClient.cs
│   ├── TokenManager.cs
│   └── Logger.cs
├── Config/
│   ├── AppSettings.json
│   └── ServerConfig.cs
├── Resources/
│   └── Strings.xaml (i18n)
├── App.xaml
├── App.xaml.cs
└── MainWindow.xaml (initial startup)
```

---

## PHASE 1: PROJECT CREATION & DEPENDENCIES

### STEP 1.1: INSTALL REQUIRED NUGET PACKAGES

**Tools to Use:** NuGet Package Manager (Visual Studio → Tools → NuGet Package Manager → Package Manager Console)

**Execute these commands in Package Manager Console:**

```powershell
Install-Package Microsoft.AspNetCore.SignalR.Client -Version 10.0.0
Install-Package System.Net.Http.Json -Version 10.0.0
Install-Package System.Text.Json -Version 10.0.0
Install-Package Newtonsoft.Json -Version 13.0.3
Install-Package log4net -Version 2.0.15
Install-Package WindowsAPICodePack-Shell -Version 1.4.1
```

**Why Each Package:**
- `SignalR.Client`: Real-time bi-directional communication
- `System.Net.Http.Json`: JSON API serialization
- `System.Text.Json`: JSON parsing
- `Newtonsoft.Json`: Alternative JSON support
- `log4net`: Event and error logging
- `WindowsAPICodePack-Shell`: Windows API for system monitoring

### STEP 1.2: CREATE APP CONFIGURATION FILES

**File:** `Config/AppSettings.json`

```json
{
  "ServerConfig": {
    "ApiBaseUrl": "https://localhost:5001",
    "SignalRHubUrl": "https://localhost:5001/hubs/room",
    "Environment": "Development"
  },
  "Monitoring": {
    "EnableEnvironmentCheck": true,
    "EnableBehavioralMonitoring": true,
    "EventTransmissionInterval": 5000,
    "ReconnectionRetryCount": 5,
    "ReconnectionRetryDelay": 2000
  },
  "Logging": {
    "LogFilePath": "Logs/SAC.log",
    "LogLevel": "Info"
  }
}
```

**File:** `Config/ServerConfig.cs`

```csharp
using System.Text.Json;

namespace SecureAssessmentClient.Config
{
    public class ServerConfig
    {
        public ServerSettings ServerSettings { get; set; }
        public MonitoringSettings MonitoringSettings { get; set; }
        public LoggingSettings LoggingSettings { get; set; }

        public static ServerConfig Load(string configPath = "Config/AppSettings.json")
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<ServerConfig>(json);
            return config ?? new ServerConfig();
        }
    }

    public class ServerSettings
    {
        public string ApiBaseUrl { get; set; }
        public string SignalRHubUrl { get; set; }
        public string Environment { get; set; }
    }

    public class MonitoringSettings
    {
        public bool EnableEnvironmentCheck { get; set; }
        public bool EnableBehavioralMonitoring { get; set; }
        public int EventTransmissionInterval { get; set; }
        public int ReconnectionRetryCount { get; set; }
        public int ReconnectionRetryDelay { get; set; }
    }

    public class LoggingSettings
    {
        public string LogFilePath { get; set; }
        public string LogLevel { get; set; }
    }
}
```

### STEP 1.3: CREATE UTILITIES

**File:** `Utilities/Constants.cs`

```csharp
namespace SecureAssessmentClient.Utilities
{
    public static class Constants
    {
        public const string TOKEN_KEY = "auth_token";
        public const string USER_ID_KEY = "user_id";
        public const string USER_ROLE_KEY = "user_role";

        // Event Types
        public const string EVENT_ALT_TAB = "ALT_TAB";
        public const string EVENT_WINDOW_SWITCH = "WINDOW_SWITCH";
        public const string EVENT_PROCESS_DETECTED = "PROCESS_DETECTED";
        public const string EVENT_CLIPBOARD_COPY = "CLIPBOARD_COPY";
        public const string EVENT_CLIPBOARD_PASTE = "CLIPBOARD_PASTE";
        public const string EVENT_SCREENSHOT = "SCREENSHOT";
        public const string EVENT_IDLE = "IDLE";
        public const string EVENT_VM_DETECTED = "VM_DETECTED";
        public const string EVENT_HAS_DETECTED = "HAS_DETECTED";

        // Risk Levels
        public const string RISK_SAFE = "Safe";
        public const string RISK_SUSPICIOUS = "Suspicious";
        public const string RISK_CHEATING = "Cheating";

        // Room Status
        public const string ROOM_PENDING = "Pending";
        public const string ROOM_COUNTDOWN = "Countdown";
        public const string ROOM_ACTIVE = "Active";
        public const string ROOM_ENDED = "Ended";
    }
}
```

**File:** `Utilities/TokenManager.cs`

```csharp
using System.Security.Cryptography;
using System.Text;

namespace SecureAssessmentClient.Utilities
{
    public static class TokenManager
    {
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SecureAssessmentClient",
            "token.cfg"
        );

        public static void SaveToken(string token)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TokenFilePath));
            var encrypted = EncryptToken(token);
            File.WriteAllText(TokenFilePath, encrypted);
        }

        public static string GetToken()
        {
            if (!File.Exists(TokenFilePath))
                return null;

            var encrypted = File.ReadAllText(TokenFilePath);
            return DecryptToken(encrypted);
        }

        public static void ClearToken()
        {
            if (File.Exists(TokenFilePath))
                File.Delete(TokenFilePath);
        }

        private static string EncryptToken(string token)
        {
            var key = Encoding.UTF8.GetBytes("SecureAssessmentClientKey1234567");
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(Encoding.UTF8.GetBytes(token));
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private static string DecryptToken(string encryptedToken)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes("SecureAssessmentClientKey1234567");
                var buffer = Convert.FromBase64String(encryptedToken);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    var iv = new byte[aes.IV.Length];
                    Array.Copy(buffer, 0, iv, 0, iv.Length);
                    aes.IV = iv;
                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
```

**File:** `Utilities/Logger.cs`

```csharp
using log4net;
using log4net.Config;

namespace SecureAssessmentClient.Utilities
{
    public static class Logger
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Logger));

        static Logger()
        {
            XmlConfigurator.Configure(new FileInfo("Config/log4net.config"));
        }

        public static void Info(string message) => log.Info(message);
        public static void Error(string message, Exception ex = null) => log.Error(message, ex);
        public static void Warn(string message) => log.Warn(message);
        public static void Debug(string message) => log.Debug(message);
    }
}
```

**File:** `Config/log4net.config`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <log4net>
    <appender name="FileAppender" type="log4net.Appenders.RollingFileAppender">
      <file value="Logs/SAC.log" />
      <appendToFile value="true" />
      <rollingStyle value="Size" />
      <maxSizeRollBackups value="5" />
      <maximumFileSize value="10MB" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
      </layout>
    </appender>
    <root>
      <level value="INFO" />
      <appender-ref ref="FileAppender" />
    </root>
  </log4net>
</configuration>
```

---

## PHASE 2: DATA MODELS & DTOS

### STEP 2.1: AUTHENTICATION MODELS

**File:** `Models/Authentication/LoginRequest.cs`

```csharp
namespace SecureAssessmentClient.Models.Authentication
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
```

**File:** `Models/Authentication/LoginResponse.cs`

```csharp
namespace SecureAssessmentClient.Models.Authentication
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public AuthToken Token { get; set; }
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
```

**File:** `Models/Authentication/AuthToken.cs`

```csharp
namespace SecureAssessmentClient.Models.Authentication
{
    public class AuthToken
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}
```

### STEP 2.2: ROOM MODELS

**File:** `Models/Room/RoomStatus.cs`

```csharp
namespace SecureAssessmentClient.Models.Room
{
    public enum RoomStatus
    {
        Pending,
        Countdown,
        Active,
        Ended
    }
}
```

**File:** `Models/Room/RoomDto.cs`

```csharp
namespace SecureAssessmentClient.Models.Room
{
    public class RoomDto
    {
        public string Id { get; set; }
        public string SubjectName { get; set; }
        public string InstructorId { get; set; }
        public RoomStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsJoined { get; set; }
        public DateTime? JoinedAt { get; set; }
    }
}
```

**File:** `Models/Room/JoinRoomRequest.cs`

```csharp
namespace SecureAssessmentClient.Models.Room
{
    public class JoinRoomRequest
    {
        public string RoomId { get; set; }
    }

    public class EnrollWithCodeRequest
    {
        public string RoomCode { get; set; }
    }
}
```

### STEP 2.3: MONITORING MODELS

**File:** `Models/Monitoring/RiskLevel.cs`

```csharp
namespace SecureAssessmentClient.Models.Monitoring
{
    public enum RiskLevel
    {
        Safe,
        Suspicious,
        Cheating
    }

    public enum ViolationType
    {
        Passive,
        Aggressive
    }
}
```

**File:** `Models/Monitoring/MonitoringEvent.cs`

```csharp
namespace SecureAssessmentClient.Models.Monitoring
{
    public class MonitoringEvent
    {
        public string EventType { get; set; }
        public ViolationType ViolationType { get; set; }
        public int SeverityScore { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
        public string SessionId { get; set; }
    }
}
```

**File:** `Models/Monitoring/DetectionSettings.cs`

```csharp
namespace SecureAssessmentClient.Models.Monitoring
{
    public class DetectionSettings
    {
        public string RoomId { get; set; }
        public bool EnableClipboardMonitoring { get; set; }
        public bool EnableProcessDetection { get; set; }
        public bool EnableIdleDetection { get; set; }
        public int IdleThresholdSeconds { get; set; }
        public bool EnableFocusDetection { get; set; }
        public bool EnableVirtualizationCheck { get; set; }
        public bool StrictMode { get; set; }
    }
}
```

---

## PHASE 3: AUTHENTICATION & LOGIN

### STEP 3.1: API SERVICE

**File:** `Services/ApiService.cs`

```csharp
using SecureAssessmentClient.Models.Authentication;
using SecureAssessmentClient.Models.Room;
using SecureAssessmentClient.Models.Monitoring;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error($"Login failed: {responseContent}");
                return new LoginResponse { Success = false, Message = "Login failed" };
            }

            var result = JsonSerializer.Deserialize<LoginResponse>(responseContent);
            if (result?.Token != null)
            {
                TokenManager.SaveToken(result.Token.AccessToken);
                SetAuthToken(result.Token.AccessToken);
            }

            return result;
        }

        public async Task<List<RoomDto>> GetAvailableRoomsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/rooms/my");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<RoomDto>>(content) ?? new List<RoomDto>();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get available rooms", ex);
                return new List<RoomDto>();
            }
        }

        public async Task<bool> JoinRoomAsync(string roomId)
        {
            try
            {
                var request = new JoinRoomRequest { RoomId = roomId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/join", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to join room", ex);
                return false;
            }
        }

        public async Task<RoomDto> GetRoomDetailsAsync(string roomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/rooms/{roomId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RoomDto>(content);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get room details", ex);
                return null;
            }
        }

        public async Task<DetectionSettings> GetDetectionSettingsAsync(string roomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/rooms/{roomId}/settings");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DetectionSettings>(content);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get detection settings", ex);
                return null;
            }
        }
    }
}
```

### STEP 3.2: AUTH SERVICE

**File:** `Services/AuthService.cs`

```csharp
using SecureAssessmentClient.Models.Authentication;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services
{
    public class AuthService
    {
        private readonly ApiService _apiService;

        public AuthService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<(bool Success, string UserId, string Message)> LoginAsync(string email, string password)
        {
            try
            {
                var request = new LoginRequest { Email = email, Password = password };
                var response = await _apiService.LoginAsync(request);

                if (response.Success && response.User != null)
                {
                    Logger.Info($"User {email} logged in successfully");
                    return (true, response.User.Id, "Login successful");
                }

                return (false, null, response.Message ?? "Login failed");
            }
            catch (Exception ex)
            {
                Logger.Error("Login error", ex);
                return (false, null, "An error occurred during login");
            }
        }

        public void Logout()
        {
            TokenManager.ClearToken();
            Logger.Info("User logged out");
        }

        public string GetStoredToken() => TokenManager.GetToken();

        public bool IsAuthenticated() => !string.IsNullOrEmpty(TokenManager.GetToken());
    }
}
```

### STEP 3.3: LOGIN WINDOW UI

**File:** `UI/Windows/LoginWindow.xaml`

```xml
<Window x:Class="SecureAssessmentClient.UI.Windows.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Secure Assessment Client - Login"
        Height="400" Width="500"
        Background="#F5F5F5"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize">
    <Grid>
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Width="350">
            <TextBlock Text="Secure Assessment Client"
                       FontSize="24" FontWeight="Bold"
                       Foreground="#333333" TextAlignment="Center" Margin="0,0,0,30"/>

            <TextBlock Text="Email:" FontSize="12" Foreground="#555555" Margin="0,0,0,5"/>
            <TextBox x:Name="EmailTextBox"
                     Height="40" Padding="10,8"
                     FontSize="12"
                     Background="White"
                     Foreground="#333333"
                     BorderThickness="1" BorderBrush="#CCCCCC"/>

            <TextBlock Text="Password:" FontSize="12" Foreground="#555555" Margin="0,15,0,5"/>
            <PasswordBox x:Name="PasswordBox"
                         Height="40" Padding="10,8"
                         FontSize="12"
                         Background="White"
                         BorderThickness="1" BorderBrush="#CCCCCC"/>

            <Button x:Name="LoginButton"
                    Content="Login"
                    Height="40"
                    Margin="0,25,0,0"
                    Background="#007ACC"
                    Foreground="White"
                    FontSize="12" FontWeight="Bold"
                    Click="LoginButton_Click"
                    Cursor="Hand"/>

            <TextBlock x:Name="ErrorMessage"
                       Foreground="#D32F2F"
                       TextAlignment="Center"
                       Margin="0,15,0,0"
                       TextWrapping="Wrap"/>
        </StackPanel>
    </Grid>
</Window>
```

**File:** `UI/Windows/LoginWindow.xaml.cs`

```csharp
using SecureAssessmentClient.Services;
using SecureAssessmentClient.Utilities;
using System.Windows;

namespace SecureAssessmentClient.UI.Windows
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;
        private readonly ApiService _apiService;

        public LoginWindow(AuthService authService, ApiService apiService)
        {
            InitializeComponent();
            _authService = authService;
            _apiService = apiService;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage.Text = "Email and password are required";
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "Logging in...";

            var (success, userId, message) = await _authService.LoginAsync(email, password);

            if (success)
            {
                RoomDashboardWindow dashboard = new RoomDashboardWindow(_apiService);
                dashboard.Show();
                this.Close();
            }
            else
            {
                ErrorMessage.Text = message;
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Login";
            }
        }
    }
}
```

---

## PHASE 4: ROOM DISCOVERY & UI

### STEP 4.1: ROOM SERVICE

**File:** `Services/RoomService.cs`

```csharp
using SecureAssessmentClient.Models.Room;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services
{
    public class RoomService
    {
        private readonly ApiService _apiService;

        public RoomService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<RoomDto>> GetMyRoomsAsync()
        {
            try
            {
                return await _apiService.GetAvailableRoomsAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get rooms", ex);
                return new List<RoomDto>();
            }
        }

        public async Task<bool> JoinRoomAsync(string roomId)
        {
            try
            {
                var success = await _apiService.JoinRoomAsync(roomId);
                if (success)
                {
                    Logger.Info($"Joined room {roomId}");
                }
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to join room", ex);
                return false;
            }
        }

        public async Task<RoomDto> GetRoomDetailsAsync(string roomId)
        {
            return await _apiService.GetRoomDetailsAsync(roomId);
        }
    }
}
```

### STEP 4.2: ROOM DASHBOARD WINDOW (MAIN UI)

**File:** `UI/Windows/RoomDashboardWindow.xaml`

```xml
<Window x:Class="SecureAssessmentClient.UI.Windows.RoomDashboardWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Secure Assessment Client - Room Dashboard"
        Height="600" Width="900"
        Background="#F5F5F5"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="60"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#007ACC" Padding="20,0">
            <Grid>
                <TextBlock Text="My Exam Rooms"
                           FontSize="20" FontWeight="Bold"
                           Foreground="White"
                           VerticalAlignment="Center"/>
                <Button Content="Logout"
                        HorizontalAlignment="Right"
                        VerticalAlignment="Center"
                        Background="#D32F2F"
                        Foreground="White"
                        Click="LogoutButton_Click"
                        Cursor="Hand"
                        Padding="15,8"/>
            </Grid>
        </Border>

        <!-- Room Grid -->
        <ScrollViewer Grid.Row="1" Padding="20">
            <ItemsControl x:Name="RoomItemsControl">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel Orientation="Horizontal" />
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Width="200" Height="250" Margin="10"
                                Background="White" CornerRadius="10"
                                BorderThickness="1" BorderBrush="#CCCCCC"
                                MouseDown="RoomOrb_MouseDown"
                                Cursor="Hand">
                            <StackPanel Padding="20" VerticalAlignment="Center" HorizontalAlignment="Center">
                                <Ellipse Width="100" Height="100"
                                         Fill="#007ACC" Margin="0,0,0,15"/>
                                <TextBlock Text="{Binding SubjectName}"
                                           FontSize="14" FontWeight="Bold"
                                           TextAlignment="Center" Margin="0,0,0,10"
                                           TextWrapping="Wrap"/>
                                <TextBlock Text="{Binding Status}"
                                           FontSize="11" Foreground="#666666"
                                           TextAlignment="Center" Margin="0,0,0,10"/>
                                <Button Content="Join Room"
                                        Height="35"
                                        Background="#28A745"
                                        Foreground="White"
                                        FontWeight="Bold"
                                        Tag="{Binding Id}"
                                        Click="JoinButton_Click"
                                        Cursor="Hand"/>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
    </Grid>
</Window>
```

**File:** `UI/Windows/RoomDashboardWindow.xaml.cs`

```csharp
using SecureAssessmentClient.Services;
using SecureAssessmentClient.Models.Room;
using SecureAssessmentClient.Utilities;
using System.Windows;

namespace SecureAssessmentClient.UI.Windows
{
    public partial class RoomDashboardWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly RoomService _roomService;

        public RoomDashboardWindow(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            _roomService = new RoomService(apiService);
            LoadRooms();
        }

        private async void LoadRooms()
        {
            var rooms = await _roomService.GetMyRoomsAsync();
            RoomItemsControl.ItemsSource = rooms;
        }

        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string roomId)
            {
                bool success = await _roomService.JoinRoomAsync(roomId);
                if (success)
                {
                    MonitoringWindow monitoring = new MonitoringWindow(_apiService, roomId);
                    monitoring.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to join room", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            TokenManager.ClearToken();
            LoginWindow login = new LoginWindow(
                new AuthService(_apiService),
                _apiService);
            login.Show();
            this.Close();
        }

        private void RoomOrb_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Room details popup could go here
        }
    }
}
```

---

## PHASE 5: SIGNALR CONNECTION

### STEP 5.1: SIGNALR SERVICE

**File:** `Services/SignalRService.cs`

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using SecureAssessmentClient.Models.Room;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services
{
    public class SignalRService
    {
        private HubConnection _hubConnection;
        private string _hubUrl;
        private string _sessionId;

        public event Action<string> OnSessionCountdownStarted;
        public event Action<string> OnSessionStarted;
        public event Action<string> OnSessionEnded;
        public event Action<string> OnDisconnected;
        public event Action<string> OnReconnected;

        public SignalRService(string hubUrl)
        {
            _hubUrl = hubUrl;
        }

        public async Task ConnectAsync(string token, string sessionId)
        {
            _sessionId = sessionId;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.AccessTokenProvider = async () => token;
                })
                .WithAutomaticReconnect(new[] {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                })
                .Build();

            // Server to Client Methods
            _hubConnection.On<string>("SessionCountdownStarted", (countdown) =>
            {
                Logger.Info($"Countdown started: {countdown}");
                OnSessionCountdownStarted?.Invoke(countdown);
            });

            _hubConnection.On<string>("SessionStarted", (status) =>
            {
                Logger.Info("Session started");
                OnSessionStarted?.Invoke(status);
            });

            _hubConnection.On<string>("SessionEnded", (status) =>
            {
                Logger.Info("Session ended");
                OnSessionEnded?.Invoke(status);
            });

            _hubConnection.Closed += async (ex) =>
            {
                Logger.Warn("Disconnected from server");
                OnDisconnected?.Invoke(ex?.Message ?? "Disconnected");
                await Task.Delay(2000);
            };

            _hubConnection.Reconnected += async (connectionId) =>
            {
                Logger.Info("Reconnected to server");
                OnReconnected?.Invoke(connectionId);
            };

            try
            {
                await _hubConnection.StartAsync();
                Logger.Info("Connected to SignalR hub");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to connect to SignalR", ex);
                throw;
            }
        }

        public async Task SendMonitoringEventAsync(MonitoringEvent monitoringEvent)
        {
            try
            {
                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    await _hubConnection.SendAsync("SendMonitoringEvent", monitoringEvent);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to send monitoring event", ex);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error disconnecting from SignalR", ex);
            }
        }

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    }
}
```

---

## PHASE 6: ENVIRONMENT INTEGRITY DETECTION

### STEP 6.1: ENVIRONMENT INTEGRITY SERVICE

**File:** `Services/DetectionService/EnvironmentIntegrityService.cs`

```csharp
using SecureAssessmentClient.Utilities;
using System.Diagnostics;
using Microsoft.Win32;

namespace SecureAssessmentClient.Services.DetectionService
{
    public class EnvironmentIntegrityService
    {
        // Virtualization Artifact Check (VAC)
        public (bool IsVirtual, string Details) CheckVirtualizationArtifacts()
        {
            var issues = new List<string>();

            // Check VM indicators from registry
            var vmIndicators = new[] {
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\VBoxGuest",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\VBoxMouse",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\VBoxSF",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\VBoxVideo",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\vmicheartbeat",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\vmicvss",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\vmicshutdown",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\vmicrdv",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\vmickvpexchange"
            };

            foreach (var indicator in vmIndicators)
            {
                try
                {
                    var hive = indicator.Split('\\')[0];
                    var path = string.Join("\\", indicator.Split('\\').Skip(1));

                    using (var key = Registry.LocalMachine.OpenSubKey(path.Substring(hive.Length + 1)))
                    {
                        if (key != null)
                        {
                            issues.Add($"VM Artifact Detected: {indicator}");
                        }
                    }
                }
                catch { }
            }

            // Check processes
            var vmProcesses = new[] { "VBoxService", "VBoxTray", "vmtoolsd" };
            foreach (var process in vmProcesses)
            {
                try
                {
                    if (Process.GetProcessesByName(process).Length > 0)
                    {
                        issues.Add($"VM Process Detected: {process}");
                    }
                }
                catch { }
            }

            return (issues.Count > 0, string.Join("; ", issues));
        }

        // Hardware-Software Artifact Scan (HAS)
        public (bool HasAnomalies, string Details) ScanHardwareSoftwareArtifacts()
        {
            var issues = new List<string>();

            // Check for Remote Desktop
            try
            {
                var rdpKey = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Terminal Server");
                if (rdpKey?.GetValue("fDenyTSConnections") is int val && val == 0)
                {
                    issues.Add("Remote Desktop is enabled");
                }
            }
            catch { }

            // Check for debugging tools
            var debuggingTools = new[] { "OllyDbg", "WinDbg", "x64dbg", "IDA" };
            foreach (var tool in debuggingTools)
            {
                try
                {
                    if (Process.GetProcessesByName(tool.ToLower()).Length > 0)
                    {
                        issues.Add($"Debugging Tool Detected: {tool}");
                    }
                }
                catch { }
            }

            // Check for screen recording
            var screenRecorderProcesses = new[] { "ffmpeg", "OBS", "ScreenFlow", "Camtasia" };
            foreach (var recorder in screenRecorderProcesses)
            {
                try
                {
                    if (Process.GetProcessesByName(recorder.ToLower()).Length > 0)
                    {
                        issues.Add($"Screen Recorder Detected: {recorder}");
                    }
                }
                catch { }
            }

            return (issues.Count > 0, string.Join("; ", issues));
        }

        public bool PerformInitialEnvironmentCheck()
        {
            Logger.Info("Performing environment integrity check...");

            var (isVirtual, vmDetails) = CheckVirtualizationArtifacts();
            if (isVirtual)
            {
                Logger.Error($"Virtualization detected: {vmDetails}");
                return false;
            }

            var (hasAnomalies, hasDetails) = ScanHardwareSoftwareArtifacts();
            if (hasAnomalies)
            {
                Logger.Warn($"Hardware/Software anomalies detected: {hasDetails}");
            }

            Logger.Info("Environment check completed");
            return !isVirtual;
        }
    }
}
```

---

## PHASE 7: BEHAVIORAL MONITORING MODULES

### STEP 7.1: BEHAVIORAL MONITORING SERVICE

**File:** `Services/DetectionService/BehavioralMonitoringService.cs`

```csharp
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;
using System.Diagnostics;
using System.Windows.Input;

namespace SecureAssessmentClient.Services.DetectionService
{
    public class BehavioralMonitoringService
    {
        private IntPtr _lastWindowHandle;
        private DateTime _lastKeyPressTime;
        private DateTime _lastMouseMoveTime;
        private int _windowSwitchCount;
        private List<string> _blacklistedProcesses;
        private bool _isMonitoring;

        public event Action<MonitoringEvent> OnViolationDetected;

        public BehavioralMonitoringService()
        {
            _lastKeyPressTime = DateTime.Now;
            _lastMouseMoveTime = DateTime.Now;
            _windowSwitchCount = 0;
            InitializeBlacklist();
        }

        private void InitializeBlacklist()
        {
            _blacklistedProcesses = new List<string>
            {
                "chrome", "firefox", "msedge", "opera",  // Alternative browsers
                "telegram", "discord", "whatsapp", "slack", // Communication apps
                "chatgpt", "copilot", "claude", // AI tools
                "snipping tool", "screenshot", "vlc"  // Capture/media tools
            };
        }

        // Real-Time Focus Monitoring (RTFM)
        public MonitoringEvent DetectWindowFocus()
        {
            var currentWindow = GetActiveWindowHandle();

            if (currentWindow != _lastWindowHandle)
            {
                _windowSwitchCount++;
                _lastWindowHandle = currentWindow;

                int switchCount = _windowSwitchCount;
                var severity = switchCount > 3 ? 3 : 1;
                var violationType = switchCount > 3 ? ViolationType.Aggressive : ViolationType.Passive;

                return new MonitoringEvent
                {
                    EventType = "WINDOW_SWITCH",
                    ViolationType = violationType,
                    SeverityScore = severity,
                    Timestamp = DateTime.Now,
                    Details = $"Window switched {switchCount} times"
                };
            }

            return null;
        }

        private IntPtr GetActiveWindowHandle()
        {
            try
            {
                var foreground = GetForegroundWindow();
                return foreground;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // Process Blacklisting Detection (PBD)
        public MonitoringEvent DetectBlacklistedProcesses()
        {
            try
            {
                var processes = Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        var processName = process.ProcessName.ToLower();
                        if (_blacklistedProcesses.Any(b => processName.Contains(b)))
                        {
                            return new MonitoringEvent
                            {
                                EventType = "BLACKLISTED_PROCESS",
                                ViolationType = ViolationType.Aggressive,
                                SeverityScore = 3,
                                Timestamp = DateTime.Now,
                                Details = $"Blacklisted process detected: {process.ProcessName}"
                            };
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        // Clipboard and Screenshot Activity Detection (CSAD)
        public MonitoringEvent DetectClipboardActivity()
        {
            try
            {
                if (System.Windows.Forms.Clipboard.ContainsText() ||
                    System.Windows.Forms.Clipboard.ContainsImage())
                {
                    return new MonitoringEvent
                    {
                        EventType = "CLIPBOARD_ACTIVITY",
                        ViolationType = ViolationType.Passive,
                        SeverityScore = 2,
                        Timestamp = DateTime.Now,
                        Details = "Clipboard access detected"
                    };
                }
            }
            catch { }

            return null;
        }

        // Idle Detection
        public MonitoringEvent DetectIdleActivity(int idleThresholdSeconds = 300)
        {
            var currentTime = DateTime.Now;
            var keyIdle = (int)(currentTime - _lastKeyPressTime).TotalSeconds;
            var mouseIdle = (int)(currentTime - _lastMouseMoveTime).TotalSeconds;

            int maxIdle = Math.Max(keyIdle, mouseIdle);

            if (maxIdle > idleThresholdSeconds)
            {
                return new MonitoringEvent
                {
                    EventType = "IDLE_DETECTED",
                    ViolationType = ViolationType.Passive,
                    SeverityScore = 1,
                    Timestamp = DateTime.Now,
                    Details = $"Idle for {maxIdle} seconds"
                };
            }

            return null;
        }

        public void UpdateActivity()
        {
            _lastKeyPressTime = DateTime.Now;
            _lastMouseMoveTime = DateTime.Now;
        }

        public void StartMonitoring()
        {
            _isMonitoring = true;
            Logger.Info("Behavioral monitoring started");
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            Logger.Info("Behavioral monitoring stopped");
        }
    }
}
```

---

## PHASE 8: DECISION ENGINE & RISK CLASSIFICATION

### STEP 8.1: DECISION ENGINE SERVICE

**File:** `Services/DetectionService/DecisionEngineService.cs`

```csharp
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services.DetectionService
{
    public class DecisionEngineService
    {
        private int _totalSeverityScore;
        private int _aggressiveViolationCount;
        private int _passiveViolationCount;
        private bool _strictMode;

        public DecisionEngineService(bool strictMode = false)
        {
            _strictMode = strictMode;
            Reset();
        }

        public void UpdateSettings(bool strictMode)
        {
            _strictMode = strictMode;
        }

        public void ProcessEvent(MonitoringEvent monitoringEvent)
        {
            if (monitoringEvent == null)
                return;

            int severity = monitoringEvent.SeverityScore;

            // Apply strict mode multiplier
            if (_strictMode)
            {
                severity = (int)(severity * 1.5);
            }

            _totalSeverityScore += severity;

            if (monitoringEvent.ViolationType == ViolationType.Aggressive)
            {
                _aggressiveViolationCount++;
            }
            else
            {
                _passiveViolationCount++;
            }

            Logger.Info($"Event processed: {monitoringEvent.EventType}, Total Score: {_totalSeverityScore}");
        }

        public RiskLevel ClassifyRisk()
        {
            // Decision Rules
            if (_aggressiveViolationCount >= 1)
            {
                return RiskLevel.Cheating;
            }

            if (_totalSeverityScore >= 10 || _passiveViolationCount >= 3)
            {
                return RiskLevel.Suspicious;
            }

            return RiskLevel.Safe;
        }

        public void Reset()
        {
            _totalSeverityScore = 0;
            _aggressiveViolationCount = 0;
            _passiveViolationCount = 0;
        }

        public int GetTotalScore() => _totalSeverityScore;
        public int GetAggressiveCount() => _aggressiveViolationCount;
        public int GetPassiveCount() => _passiveViolationCount;
    }
}
```

---

## PHASE 9: EVENT LOGGING & TRANSMISSION

### STEP 9.1: EVENT LOGGER SERVICE

**File:** `Services/DetectionService/EventLoggerService.cs`

```csharp
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services.DetectionService
{
    public class EventLoggerService
    {
        private List<MonitoringEvent> _localEvents;
        private readonly string _logFilePath;

        public EventLoggerService()
        {
            _localEvents = new List<MonitoringEvent>();
            _logFilePath = "Logs/MonitoringEvents.csv";
            EnsureLogDirectory();
        }

        private void EnsureLogDirectory()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
        }

        public void LogEvent(MonitoringEvent monitoringEvent)
        {
            _localEvents.Add(monitoringEvent);
            WriteToFile(monitoringEvent);
            Logger.Info($"Event logged: {monitoringEvent.EventType}");
        }

        private void WriteToFile(MonitoringEvent monitoringEvent)
        {
            try
            {
                var line = $"{monitoringEvent.Timestamp:O}|{monitoringEvent.EventType}|{monitoringEvent.ViolationType}|{monitoringEvent.SeverityScore}|{monitoringEvent.Details}";
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to write event to log file", ex);
            }
        }

        public List<MonitoringEvent> GetAllEvents() => _localEvents.ToList();

        public void ExportEventsSummary(string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("Timestamp,EventType,ViolationType,SeverityScore,Details");
                    foreach (var evt in _localEvents)
                    {
                        writer.WriteLine($"{evt.Timestamp:O},{evt.EventType},{evt.ViolationType},{evt.SeverityScore},\"{evt.Details}\"");
                    }
                }
                Logger.Info($"Events exported to {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to export events", ex);
            }
        }

        public void Clear()
        {
            _localEvents.Clear();
        }
    }
}
```

---

## PHASE 10: UI INTEGRATION & FINAL TESTING

### STEP 10.1: MONITORING WINDOW

**File:** `UI/Windows/MonitoringWindow.xaml`

```xml
<Window x:Class="SecureAssessmentClient.UI.Windows.MonitoringWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Secure Assessment Client - Monitoring"
        Height="500" Width="700"
        Background="#F5F5F5"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="70"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="50"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#007ACC" Padding="20,0">
            <Grid>
                <StackPanel VerticalAlignment="Center">
                    <TextBlock Text="Assessment Active"
                               FontSize="18" FontWeight="Bold"
                               Foreground="White"/>
                    <TextBlock x:Name="RoomNameText"
                               FontSize="12" Foreground="#E0E0E0"/>
                </StackPanel>
                <StackPanel HorizontalAlignment="Right" VerticalAlignment="Center">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="15"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <Ellipse Width="12" Height="12"
                                 Fill="#28A745" Grid.Column="0"/>
                        <TextBlock Text="{Binding ConnectionStatus}"
                                   Foreground="White"
                                   VerticalAlignment="Center"
                                   Grid.Column="2"/>
                    </Grid>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Monitoring Status Panel -->
        <StackPanel Grid.Row="1" Padding="20">
            <TextBlock Text="Monitoring Status"
                       FontSize="14" FontWeight="Bold"
                       Foreground="#333333" Margin="0,0,0,15"/>

            <Border Background="White" Padding="15" CornerRadius="5" Margin="0,0,0,15">
                <StackPanel>
                    <TextBlock Text="Environment Integrity" FontSize="12" FontWeight="Bold" Foreground="#555555"/>
                    <StackPanel Margin="0,10,0,0">
                        <CheckBox Content="Virtualization Check" IsEnabled="False" IsChecked="True" Margin="0,3"/>
                        <CheckBox Content="Hardware Scan" IsEnabled="False" IsChecked="True" Margin="0,3"/>
                    </StackPanel>
                </StackPanel>
            </Border>

            <Border Background="White" Padding="15" CornerRadius="5">
                <StackPanel>
                    <TextBlock Text="Behavioral Monitoring" FontSize="12" FontWeight="Bold" Foreground="#555555"/>
                    <StackPanel Margin="0,10,0,0">
                        <CheckBox Content="Window Focus Monitoring" IsEnabled="False" IsChecked="True" Margin="0,3"/>
                        <CheckBox Content="Process Detection" IsEnabled="False" IsChecked="True" Margin="0,3"/>
                        <CheckBox Content="Clipboard Activity" IsEnabled="False" IsChecked="True" Margin="0,3"/>
                        <CheckBox Content="Idle Detection" IsEnabled="False" IsChecked="True" Margin="0,3"/>
                        <TextBlock x:Name="StatusText"
                                   Foreground="#28A745"
                                   FontSize="11"
                                   Margin="0,10,0,0"/>
                    </StackPanel>
                </StackPanel>
            </Border>
        </StackPanel>

        <!-- Footer -->
        <Border Grid.Row="2" Background="#EEEEEE" BorderThickness="0,1,0,0" BorderBrush="#CCCCCC">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center" Padding="20,0">
                <TextBlock Text="Do not close this window during assessment"
                           Foreground="#666666"
                           VerticalAlignment="Center"
                           Margin="0,0,20,0"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

**File:** `UI/Windows/MonitoringWindow.xaml.cs`

```csharp
using SecureAssessmentClient.Services;
using SecureAssessmentClient.Services.DetectionService;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;
using System.Windows;
using System.Windows.Threading;

namespace SecureAssessmentClient.UI.Windows
{
    public partial class MonitoringWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;
        private readonly EnvironmentIntegrityService _environmentService;
        private readonly BehavioralMonitoringService _behavioralService;
        private readonly DecisionEngineService _decisionEngine;
        private readonly EventLoggerService _eventLogger;
        private readonly string _roomId;
        private readonly string _serverUrl;

        private DispatcherTimer _monitoringTimer;
        private bool _isMonitoring = false;

        public MonitoringWindow(ApiService apiService, string roomId)
        {
            InitializeComponent();
            _apiService = apiService;
            _roomId = roomId;
            _serverUrl = "https://localhost:5001/hubs/room";

            _environmentService = new EnvironmentIntegrityService();
            _behavioralService = new BehavioralMonitoringService();
            _decisionEngine = new DecisionEngineService();
            _eventLogger = new EventLoggerService();

            _signalRService = new SignalRService(_serverUrl);
            _signalRService.OnSessionStarted += OnSessionStarted;
            _signalRService.OnSessionEnded += OnSessionEnded;
            _signalRService.OnDisconnected += OnDisconnected;

            InitializeMonitoringTimer();
            Task.Run(() => InitializeSignalR());
        }

        private async Task InitializeSignalR()
        {
            try
            {
                string token = TokenManager.GetToken();
                await _signalRService.ConnectAsync(token, _roomId);

                Dispatcher.Invoke(() =>
                {
                    RoomNameText.Text = $"Room ID: {_roomId}";
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize SignalR", ex);
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Failed to connect to monitoring server", "Connection Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                });
            }
        }

        private void InitializeMonitoringTimer()
        {
            _monitoringTimer = new DispatcherTimer();
            _monitoringTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _monitoringTimer.Tick += MonitoringTick;
        }

        private void OnSessionStarted(string status)
        {
            Dispatcher.Invoke(() =>
            {
                _isMonitoring = true;
                _monitoringTimer.Start();
                _behavioralService.StartMonitoring();
                StatusText.Text = "✓ Monitoring Active";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69));
                Logger.Info("Monitoring session started");
            });
        }

        private void OnSessionEnded(string status)
        {
            Dispatcher.Invoke(() =>
            {
                _isMonitoring = false;
                _monitoringTimer.Stop();
                _behavioralService.StopMonitoring();
                StatusText.Text = "⚠ Assessment Ended";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 47, 47));
                MessageBox.Show("Assessment session has ended", "Session Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            });
        }

        private void OnDisconnected(string reason)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"⚠ Disconnected: {reason}";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
            });
        }

        private void MonitoringTick(object sender, EventArgs e)
        {
            if (!_isMonitoring) return;

            try
            {
                // Detect behavioral issues
                var windowEvent = _behavioralService.DetectWindowFocus();
                var processEvent = _behavioralService.DetectBlacklistedProcesses();
                var clipboardEvent = _behavioralService.DetectClipboardActivity();
                var idleEvent = _behavioralService.DetectIdleActivity();

                var events = new[] { windowEvent, processEvent, clipboardEvent, idleEvent }
                    .Where(e => e != null)
                    .ToList();

                foreach (var evt in events)
                {
                    _decisionEngine.ProcessEvent(evt);
                    _eventLogger.LogEvent(evt);
                    Task.Run(() => _signalRService.SendMonitoringEventAsync(evt));
                }

                _behavioralService.UpdateActivity();
            }
            catch (Exception ex)
            {
                Logger.Error("Error during monitoring tick", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _monitoringTimer?.Stop();
            _behavioralService.StopMonitoring();
            Task.Run(() => _signalRService.DisconnectAsync());
            base.OnClosed(e);
        }
    }
}
```

---

## SUMMARY OF TOOLS & PROCESSES

### TOOLS TO USE:
1. **Visual Studio 2026** - IDE for development
2. **NuGet Package Manager** - Install dependencies
3. **Git** - Version control
4. **Windows Event Viewer** - Debug system events
5. **Visual Studio Debugger** - Debug application

### BUILD SEQUENCE:
1. Create Solution & Project Structure
2. Install NuGet Packages
3. Create Models & DTOs
4. Implement Authentication
5. Implement Room Management
6. Connect SignalR
7. Implement Detection Services
8. Build UI Components
9. Test Integration
10. Deploy

### KEY FILES CHECKLIST:
- [ ] App.xaml and App.xaml.cs
- [ ] MainWindow.xaml (startup)
- [ ] LoginWindow (XAML + Code-behind)
- [ ] RoomDashboardWindow (XAML + Code-behind)
- [ ] MonitoringWindow (XAML + Code-behind)
- [ ] AppSettings.json
- [ ] log4net.config
- [ ] All Services
- [ ] All Models
- [ ] All Utilities

---

**Next Steps:** Wait for confirmation to proceed with Phase 1 implementation, or ask if you need clarification on any section.

