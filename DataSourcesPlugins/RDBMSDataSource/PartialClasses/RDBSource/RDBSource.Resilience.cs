using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Partial class containing connection resilience infrastructure.
    /// Provides retry policies, circuit breaker, and health checks for database connections.
    /// </summary>
    public partial class RDBSource
    {
        #region "Resilience Configuration"

        /// <summary>
        /// Maximum number of retry attempts for transient failures.
        /// Default: 3 retries.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay for exponential backoff between retries.
        /// Default: 1 second.
        /// </summary>
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether connection resilience features are enabled.
        /// Default: true.
        /// </summary>
        public bool EnableResilience { get; set; } = true;

        /// <summary>
        /// Number of consecutive failures before circuit breaker opens.
        /// Default: 5 failures.
        /// </summary>
        public int CircuitBreakerThreshold { get; set; } = 5;

        /// <summary>
        /// Duration the circuit breaker stays open before attempting to close.
        /// Default: 30 seconds.
        /// </summary>
        public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout for connection health checks.
        /// Default: 5 seconds.
        /// </summary>
        public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);

        #endregion

        #region "Resilience Policies"

        /// <summary>
        /// Lazy-initialized retry policy with exponential backoff.
        /// </summary>
        private ResiliencePipeline? _retryPipeline;

        /// <summary>
        /// Gets the retry pipeline for transient failure handling.
        /// </summary>
        private ResiliencePipeline RetryPipeline
        {
            get
            {
                if (_retryPipeline == null && EnableResilience)
                {
                    _retryPipeline = new ResiliencePipelineBuilder()
                        .AddRetry(new RetryStrategyOptions
                        {
                            MaxRetryAttempts = MaxRetryAttempts,
                            BackoffType = DelayBackoffType.Exponential,
                            Delay = RetryBaseDelay,
                            OnRetry = args =>
                            {
                                var exception = args.Outcome.Exception;
                                var attemptNumber = args.AttemptNumber;
                                var delay = args.RetryDelay;
                                
                                DMEEditor?.AddLogMessage("Beep", 
                                    $"Retry attempt {attemptNumber}/{MaxRetryAttempts} after {delay.TotalSeconds:F1}s due to: {exception?.Message}", 
                                    DateTime.Now, 0, null, Errors.Failed);
                                
                                return ValueTask.CompletedTask;
                            },
                            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => 
                                IsTransientException(ex))
                        })
                        .Build();
                }
                return _retryPipeline ?? ResiliencePipeline.Empty;
            }
        }

        /// <summary>
        /// Lazy-initialized circuit breaker for preventing cascade failures.
        /// </summary>
        private ResiliencePipeline? _circuitBreakerPipeline;

        /// <summary>
        /// Gets the circuit breaker pipeline.
        /// </summary>
        private ResiliencePipeline CircuitBreakerPipeline
        {
            get
            {
                if (_circuitBreakerPipeline == null && EnableResilience)
                {
                    _circuitBreakerPipeline = new ResiliencePipelineBuilder()
                        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                        {
                            FailureRatio = 0.5, // Open if 50% of requests fail
                            MinimumThroughput = CircuitBreakerThreshold,
                            BreakDuration = CircuitBreakerDuration,
                            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => 
                                IsTransientException(ex)),
                            OnOpened = args =>
                            {
                                DMEEditor?.AddLogMessage("Beep", 
                                    $"Circuit breaker OPENED. Will retry after {CircuitBreakerDuration.TotalSeconds}s", 
                                    DateTime.Now, 0, null, Errors.Failed);
                                return ValueTask.CompletedTask;
                            },
                            OnClosed = args =>
                            {
                                DMEEditor?.AddLogMessage("Beep", 
                                    "Circuit breaker CLOSED. Connection restored.", 
                                    DateTime.Now, 0, null, Errors.Ok);
                                return ValueTask.CompletedTask;
                            },
                            OnHalfOpened = args =>
                            {
                                DMEEditor?.AddLogMessage("Beep", 
                                    "Circuit breaker HALF-OPEN. Testing connection...", 
                                    DateTime.Now, 0, null, Errors.Ok);
                                return ValueTask.CompletedTask;
                            }
                        })
                        .Build();
                }
                return _circuitBreakerPipeline ?? ResiliencePipeline.Empty;
            }
        }

        /// <summary>
        /// Combined resilience pipeline with retry and circuit breaker.
        /// </summary>
        private ResiliencePipeline ResilientPipeline
        {
            get
            {
                if (!EnableResilience)
                    return ResiliencePipeline.Empty;

                return new ResiliencePipelineBuilder()
                    .AddPipeline(CircuitBreakerPipeline)
                    .AddPipeline(RetryPipeline)
                    .Build();
            }
        }

        #endregion

        #region "Transient Exception Detection"

        /// <summary>
        /// Determines if an exception is transient and should trigger a retry.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns>True if the exception is transient, false otherwise.</returns>
        private bool IsTransientException(Exception ex)
        {
            if (ex == null)
                return false;

            // Check exception message for common transient error patterns
            string message = ex.Message?.ToLowerInvariant() ?? string.Empty;

            // Network-related errors
            if (message.Contains("timeout") || 
                message.Contains("timed out") ||
                message.Contains("network") ||
                message.Contains("connection was lost") ||
                message.Contains("transport-level error") ||
                message.Contains("connection reset"))
                return true;

            // Database-specific transient errors
            if (message.Contains("deadlock") ||
                message.Contains("lock timeout") ||
                message.Contains("too many connections") ||
                message.Contains("max_connections") ||
                message.Contains("tempdb is full") ||
                message.Contains("log file is full"))
                return true;

            // Check for specific exception types
            var exceptionType = ex.GetType().Name.ToLowerInvariant();
            if (exceptionType.Contains("timeout") ||
                exceptionType.Contains("sqlexception") ||
                exceptionType.Contains("dbexception"))
            {
                // For SQL-specific exceptions, check error codes if available
                if (ex is System.Data.Common.DbException dbEx)
                {
                    // Common transient SQL Server error codes
                    // -2: Timeout, 4060: Cannot open database, 40197: Service error, etc.
                    var errorCode = dbEx.ErrorCode;
                    if (errorCode == -2 || errorCode == 4060 || errorCode == 40197 || 
                        errorCode == 40501 || errorCode == 40613 || errorCode == 49918 ||
                        errorCode == 49919 || errorCode == 49920)
                        return true;
                }
                return true;
            }

            // Check inner exceptions
            if (ex.InnerException != null)
                return IsTransientException(ex.InnerException);

            return false;
        }

        #endregion

        #region "Resilient Connection Methods"

        /// <summary>
        /// Opens a connection with retry policy and circuit breaker protection.
        /// </summary>
        /// <returns>The connection state after attempting to open.</returns>
        public virtual ConnectionState OpenConnectionResilient()
        {
            if (!EnableResilience)
                return Openconnection();

            try
            {
                return ResilientPipeline.Execute(() =>
                {
                    return Openconnection();
                });
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", 
                    $"Failed to open connection after {MaxRetryAttempts} retries: {ex.Message}", 
                    DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ConnectionState.Broken;
            }
        }

        /// <summary>
        /// Asynchronously opens a connection with retry policy and circuit breaker protection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The connection state after attempting to open.</returns>
        public virtual async Task<ConnectionState> OpenConnectionResilientAsync(CancellationToken cancellationToken = default)
        {
            if (!EnableResilience)
                return Openconnection();

            try
            {
                return await ResilientPipeline.ExecuteAsync(async ct =>
                {
                    // Since Openconnection is synchronous, wrap in Task.Run
                    return await Task.Run(() => Openconnection(), ct);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", 
                    $"Failed to open connection asynchronously after {MaxRetryAttempts} retries: {ex.Message}", 
                    DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ConnectionState.Broken;
            }
        }

        #endregion

        #region "Connection Health Checks"

        /// <summary>
        /// Performs a health check on the current database connection.
        /// </summary>
        /// <returns>True if the connection is healthy, false otherwise.</returns>
        public virtual bool CheckConnectionHealth()
        {
            if (Dataconnection == null)
                return false;

            try
            {
                // Check current connection state
                if (Dataconnection.ConnectionStatus == ConnectionState.Open)
                {
                    // Verify connection is actually responsive with a simple query
                    using (var cmd = GetDataCommand())
                    {
                        cmd.CommandText = GetHealthCheckQuery();
                        cmd.CommandTimeout = (int)HealthCheckTimeout.TotalSeconds;
                        
                        var result = cmd.ExecuteScalar();
                        return result != null;
                    }
                }
                else if (Dataconnection.ConnectionStatus == ConnectionState.Closed)
                {
                    // Try to reopen
                    var state = EnableResilience ? OpenConnectionResilient() : Openconnection();
                    return state == ConnectionState.Open;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", 
                    $"Connection health check failed: {ex.Message}", 
                    DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Asynchronously performs a health check on the current database connection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the connection is healthy, false otherwise.</returns>
        public virtual async Task<bool> CheckConnectionHealthAsync(CancellationToken cancellationToken = default)
        {
            if (Dataconnection == null)
                return false;

            try
            {
                if (Dataconnection.ConnectionStatus == ConnectionState.Open)
                {
                    using (var cmd = GetDataCommand())
                    {
                        cmd.CommandText = GetHealthCheckQuery();
                        cmd.CommandTimeout = (int)HealthCheckTimeout.TotalSeconds;

                        if (cmd is System.Data.Common.DbCommand dbCommand)
                        {
                            var result = await dbCommand.ExecuteScalarAsync(cancellationToken);
                            return result != null;
                        }
                        else
                        {
                            // Fallback to synchronous for IDbCommand
                            return await Task.Run(() =>
                            {
                                var result = cmd.ExecuteScalar();
                                return result != null;
                            }, cancellationToken);
                        }
                    }
                }
                else if (Dataconnection.ConnectionStatus == ConnectionState.Closed)
                {
                    var state = await OpenConnectionResilientAsync(cancellationToken);
                    return state == ConnectionState.Open;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", 
                    $"Async connection health check failed: {ex.Message}", 
                    DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Gets the appropriate health check query for the current database type.
        /// </summary>
        /// <returns>A simple query to verify database connectivity.</returns>
        private string GetHealthCheckQuery()
        {
            return DatasourceType switch
            {
                DataSourceType.SqlServer => "SELECT 1",
                DataSourceType.Mysql => "SELECT 1",
                DataSourceType.Postgre => "SELECT 1",
                DataSourceType.Oracle => "SELECT 1 FROM DUAL",
                DataSourceType.SqlLite => "SELECT 1",
                DataSourceType.DB2 => "SELECT 1 FROM SYSIBM.SYSDUMMY1",
                _ => "SELECT 1"
            };
        }

        /// <summary>
        /// Resets the circuit breaker and retry policies.
        /// Useful after manual intervention to restore service.
        /// </summary>
        public void ResetResiliencePolicies()
        {
            _retryPipeline = null;
            _circuitBreakerPipeline = null;
            
            DMEEditor?.AddLogMessage("Beep", 
                "Resilience policies reset. Circuit breaker and retry counters cleared.", 
                DateTime.Now, 0, null, Errors.Ok);
        }

        #endregion
    }
}
