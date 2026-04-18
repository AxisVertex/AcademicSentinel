using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services
{
    /// <summary>
    /// Event logger service for batching risk assessments and transmitting to server
    /// Handles buffering, batching, transmission, and persistence of assessment events
    /// Implements retry logic, priority queuing, and local storage fallback
    /// </summary>
    public class EventLoggerService
    {
        private readonly SignalRService _signalRService;
        private readonly string _sessionId;
        
        // Event buffering
        private Queue<RiskAssessment> _pendingAssessments;
        private List<EventBatch> _batchHistory;
        private List<EventBatch> _failedBatches;  // For retry
        
        // Batch configuration (tunable)
        private int _maxBatchSize = 10;           // Max assessments per batch
        private int _batchFlushIntervalMs = 5000; // Flush every 5 seconds
        private string _storageDirectory;
        
        // Transmission state
        private bool _isTransmitting = false;
        private Timer _flushTimer;
        
        // Statistics
        private int _totalAssessmentsLogged = 0;
        private int _totalBatchesCreated = 0;
        private int _totalBatchesTransmitted = 0;
        private DateTime _sessionStartTime;

        public EventLoggerService(SignalRService signalRService, string sessionId)
        {
            _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
            _sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            
            _pendingAssessments = new Queue<RiskAssessment>();
            _batchHistory = new List<EventBatch>();
            _failedBatches = new List<EventBatch>();
            _sessionStartTime = DateTime.UtcNow;
            
            // Setup local storage directory
            _storageDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SecureAssessmentClient",
                "EventLogs",
                _sessionId
            );
            
            try
            {
                Directory.CreateDirectory(_storageDirectory);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create event log storage directory", ex);
            }
            
            Logger.Info($"Event Logger Service initialized for session {sessionId}");
        }

        /// <summary>
        /// Starts the event logger with automatic batch flushing
        /// Should be called when exam session begins
        /// </summary>
        public void Start()
        {
            if (_flushTimer == null)
            {
                _flushTimer = new Timer(
                    callback: async _ => await FlushPendingBatchesAsync(),
                    state: null,
                    dueTime: _batchFlushIntervalMs,
                    period: _batchFlushIntervalMs
                );
                Logger.Info($"Event Logger started with flush interval {_batchFlushIntervalMs}ms");
            }
        }

        /// <summary>
        /// Stops the event logger and flushes any remaining batches
        /// Should be called when exam session ends
        /// </summary>
        public async Task StopAsync()
        {
            _flushTimer?.Dispose();
            _flushTimer = null;
            
            // Final flush of any pending assessments
            await FlushPendingBatchesAsync();
            
            Logger.Info("Event Logger stopped");
        }

        /// <summary>
        /// Logs a risk assessment for later transmission
        /// Assessment added to buffer and will be batched/transmitted
        /// </summary>
        public void LogAssessment(RiskAssessment assessment)
        {
            if (assessment == null)
            {
                Logger.Warn("Attempted to log null assessment");
                return;
            }

            try
            {
                _pendingAssessments.Enqueue(assessment);
                _totalAssessmentsLogged++;
                
                Logger.Debug($"Assessment logged: {assessment.RiskLevel} (Score: {assessment.RiskScore})");
                
                // Immediately flush if critical assessment
                if (assessment.RiskLevel == RiskLevel.Cheating)
                {
                    Logger.Info("Critical assessment detected, initiating immediate flush");
                    _ = FlushPendingBatchesAsync();  // Fire and forget
                }
                
                // Flush if buffer reaches max size
                if (_pendingAssessments.Count >= _maxBatchSize)
                {
                    Logger.Debug($"Batch buffer full ({_pendingAssessments.Count}/{_maxBatchSize}), flushing");
                    _ = FlushPendingBatchesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error logging assessment", ex);
            }
        }

        /// <summary>
        /// Logs multiple assessments at once
        /// Useful for batch processing from decision engine
        /// </summary>
        public void LogAssessments(List<RiskAssessment> assessments)
        {
            if (assessments == null || assessments.Count == 0)
            {
                return;
            }

            foreach (var assessment in assessments)
            {
                LogAssessment(assessment);
            }
        }

        /// <summary>
        /// Creates a batch from pending assessments and transmits it
        /// Called periodically by timer or when buffer fills
        /// </summary>
        private async Task FlushPendingBatchesAsync()
        {
            if (_isTransmitting || _pendingAssessments.Count == 0)
            {
                return;
            }

            try
            {
                _isTransmitting = true;

                // Create batches from pending assessments
                while (_pendingAssessments.Count > 0)
                {
                    var batch = CreateBatchFromQueue();
                    if (batch != null)
                    {
                        await TransmitBatchAsync(batch);
                    }
                }

                // Retry failed batches with exponential backoff
                await RetryFailedBatchesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Error during batch flush", ex);
            }
            finally
            {
                _isTransmitting = false;
            }
        }

        /// <summary>
        /// Creates a single batch from queued assessments
        /// Takes up to _maxBatchSize assessments
        /// </summary>
        private EventBatch CreateBatchFromQueue()
        {
            if (_pendingAssessments.Count == 0)
            {
                return null;
            }

            var batch = new EventBatch
            {
                SessionId = _sessionId
            };

            int batchSize = Math.Min(_maxBatchSize, _pendingAssessments.Count);
            
            // Check if any assessment is critical (cheating)
            var assessmentList = _pendingAssessments.ToList();
            if (assessmentList.Any(a => a.RiskLevel == RiskLevel.Cheating))
            {
                batch.Priority = 1;  // High priority
            }

            // Dequeue assessments into batch
            for (int i = 0; i < batchSize; i++)
            {
                batch.Assessments.Add(_pendingAssessments.Dequeue());
            }

            _totalBatchesCreated++;
            _batchHistory.Add(batch);

            Logger.Info($"Batch created: {batch.BatchId} with {batch.Assessments.Count} assessments (Priority: {batch.Priority})");

            return batch;
        }

        /// <summary>
        /// Transmits a batch to the server via SignalR
        /// Implements retry logic and persistence
        /// </summary>
        private async Task TransmitBatchAsync(EventBatch batch)
        {
            if (batch == null || !batch.IsReadyForTransmission())
            {
                return;
            }

            try
            {
                batch.TransmissionAttempts++;
                Logger.Info($"Transmitting batch {batch.BatchId} (Attempt {batch.TransmissionAttempts})");

                // Convert RiskAssessments to MonitoringEvents for transmission
                var monitoringEvents = ConvertAssessmentsToEvents(batch.Assessments);

                // Send via SignalR
                bool success = await _signalRService.SendBatchMonitoringEventsAsync(monitoringEvents);

                if (success)
                {
                    batch.Status = "transmitted";
                    batch.TransmittedAt = DateTime.UtcNow;
                    _totalBatchesTransmitted++;
                    
                    Logger.Info($"Batch {batch.BatchId} transmitted successfully");
                    
                    // Save to local storage as acknowledgment backup
                    await SaveBatchToStorageAsync(batch);
                }
                else
                {
                    // Transmission failed - add to retry queue
                    batch.Status = "failed";
                    _failedBatches.Add(batch);
                    
                    Logger.Warn($"Batch {batch.BatchId} transmission failed, queued for retry");
                    
                    // Don't retry immediately if first attempt just failed
                    if (batch.TransmissionAttempts == 1)
                    {
                        // Will be retried in next retry cycle
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error transmitting batch {batch.BatchId}", ex);
                batch.Status = "failed";
                _failedBatches.Add(batch);
            }
        }

        /// <summary>
        /// Retries transmission of previously failed batches
        /// Implements exponential backoff and max retry attempts
        /// </summary>
        private async Task RetryFailedBatchesAsync()
        {
            if (_failedBatches.Count == 0)
            {
                return;
            }

            var toRetry = _failedBatches
                .Where(b => b.TransmissionAttempts < 5)  // Max 5 attempts
                .OrderByDescending(b => b.Priority)     // High priority first
                .ToList();

            foreach (var batch in toRetry)
            {
                // Exponential backoff: 2^attempt seconds
                int delaySeconds = (int)Math.Pow(2, batch.TransmissionAttempts - 1);
                await Task.Delay(delaySeconds * 1000);

                await TransmitBatchAsync(batch);

                // Remove from retry queue if successful
                if (batch.Status == "transmitted")
                {
                    _failedBatches.Remove(batch);
                }
                else if (batch.TransmissionAttempts >= 5)
                {
                    // Max retries exceeded - save to persistent storage and give up
                    batch.Status = "abandoned";
                    await SaveBatchToStorageAsync(batch);
                    Logger.Error($"Batch {batch.BatchId} abandoned after {batch.TransmissionAttempts} attempts");
                }
            }
        }

        /// <summary>
        /// Converts RiskAssessments to MonitoringEvents for transmission
        /// Each assessment becomes a RISK_ASSESSMENT event
        /// </summary>
        private List<MonitoringEvent> ConvertAssessmentsToEvents(List<RiskAssessment> assessments)
        {
            return assessments.Select(a => new MonitoringEvent
            {
                EventType = "RISK_ASSESSMENT",
                ViolationType = a.RiskLevel == RiskLevel.Cheating ? ViolationType.Aggressive : ViolationType.Passive,
                SeverityScore = a.RiskLevel == RiskLevel.Cheating ? 3 : (a.RiskLevel == RiskLevel.Suspicious ? 2 : 1),
                Timestamp = a.Timestamp,
                Details = $"RiskScore={a.RiskScore}|Level={a.RiskLevel}|Action={a.RecommendedAction}|{a.RationaleDescription}",
                SessionId = a.SessionId
            }).ToList();
        }

        /// <summary>
        /// Saves batch to local storage for persistence
        /// Allows recovery if app crashes or connection lost
        /// </summary>
        private async Task SaveBatchToStorageAsync(EventBatch batch)
        {
            try
            {
                string filename = Path.Combine(
                    _storageDirectory,
                    $"batch_{batch.BatchId}_{batch.Status}.json"
                );

                var json = JsonSerializer.Serialize(batch, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filename, json);

                Logger.Debug($"Batch {batch.BatchId} saved to storage: {filename}");
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving batch to storage", ex);
            }
        }

        /// <summary>
        /// Loads previously saved batches from local storage (e.g., after app restart)
        /// Returns list of batches that failed to transmit
        /// </summary>
        public async Task<List<EventBatch>> LoadPersistentBatchesAsync()
        {
            var batches = new List<EventBatch>();

            try
            {
                if (!Directory.Exists(_storageDirectory))
                {
                    return batches;
                }

                var jsonFiles = Directory.GetFiles(_storageDirectory, "batch_*.json");

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var batch = JsonSerializer.Deserialize<EventBatch>(json);
                        
                        if (batch != null && batch.Status == "failed")
                        {
                            batches.Add(batch);
                            _failedBatches.Add(batch);
                            Logger.Info($"Loaded persistent batch: {batch.BatchId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error loading batch from {file}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading persistent batches", ex);
            }

            return batches;
        }

        /// <summary>
        /// Updates batch configuration at runtime
        /// </summary>
        public void UpdateConfiguration(int maxBatchSize, int flushIntervalMs)
        {
            if (maxBatchSize > 0)
            {
                _maxBatchSize = maxBatchSize;
            }

            if (flushIntervalMs > 0)
            {
                _batchFlushIntervalMs = flushIntervalMs;
                
                // Restart timer with new interval
                _flushTimer?.Dispose();
                _flushTimer = new Timer(
                    callback: async _ => await FlushPendingBatchesAsync(),
                    state: null,
                    dueTime: _batchFlushIntervalMs,
                    period: _batchFlushIntervalMs
                );
            }

            Logger.Info($"Event logger configuration updated: BatchSize={_maxBatchSize}, FlushInterval={_batchFlushIntervalMs}ms");
        }

        /// <summary>
        /// Gets comprehensive session statistics
        /// </summary>
        public Dictionary<string, int> GetStatistics()
        {
            return new Dictionary<string, int>
            {
                { "total_assessments_logged", _totalAssessmentsLogged },
                { "pending_assessments", _pendingAssessments.Count },
                { "total_batches_created", _totalBatchesCreated },
                { "total_batches_transmitted", _totalBatchesTransmitted },
                { "failed_batches_in_queue", _failedBatches.Count },
                { "total_batches_in_history", _batchHistory.Count },
                { "session_duration_seconds", (int)(DateTime.UtcNow - _sessionStartTime).TotalSeconds }
            };
        }

        /// <summary>
        /// Gets current batch history for review
        /// </summary>
        public List<EventBatch> GetBatchHistory()
        {
            return new List<EventBatch>(_batchHistory);
        }

        /// <summary>
        /// Gets list of currently failed batches awaiting retry
        /// </summary>
        public List<EventBatch> GetFailedBatches()
        {
            return new List<EventBatch>(_failedBatches);
        }

        /// <summary>
        /// Gets number of pending assessments in buffer
        /// </summary>
        public int PendingAssessmentCount
        {
            get { return _pendingAssessments.Count; }
        }

        /// <summary>
        /// Gets number of failed batches
        /// </summary>
        public int FailedBatchCount
        {
            get { return _failedBatches.Count; }
        }

        /// <summary>
        /// Gets total assessments logged in session
        /// </summary>
        public int TotalAssessmentsLogged
        {
            get { return _totalAssessmentsLogged; }
        }

        /// <summary>
        /// Gets session ID
        /// </summary>
        public string SessionId
        {
            get { return _sessionId; }
        }

        /// <summary>
        /// Gets session duration
        /// </summary>
        public TimeSpan SessionDuration
        {
            get { return DateTime.UtcNow - _sessionStartTime; }
        }
    }
}
