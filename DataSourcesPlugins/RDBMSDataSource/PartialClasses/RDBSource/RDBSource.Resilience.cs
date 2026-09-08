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

                // Retry OUTER, circuit breaker INNER.
                //
                // Polly executes strategies in registration order, so the previous order — breaker
                // first — put the breaker outside the retry: one Execute performed up to four
                // attempts but registered as a SINGLE outcome to the breaker. With
                // MinimumThroughput at 5 over a 30-second sampling window, the breaker needed five
                // separate top-level calls before it would even evaluate the failure ratio, so in
                // practice it never opened.
                //
                // This way round, each individual attempt is seen by the breaker, and once it opens
                // the retry stops hammering a server that is already known to be failing.
                return new ResiliencePipelineBuilder()
                    .AddPipeline(RetryPipeline)
                    .AddPipeline(CircuitBreakerPipeline)
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
        /// <remarks>
        /// Deliberately does NOT treat every provider exception as transient.
        ///
        /// This used to end with an unconditional <c>return true</c> for any exception whose type
        /// name contained "sqlexception" or "dbexception" — which is most provider exceptions — so
        /// primary-key violations, syntax errors, "invalid object name" and permission-denied were
        /// all classified transient and retried three times with 1/2/4-second delays. The error-code
        /// table alongside it never changed the outcome and could not have: it reads
        /// <c>DbException.ErrorCode</c>, which is <c>Exception.HResult</c>, while 4060, 40197,
        /// 40501, 40613 and 49918-49920 are SQL Server *error numbers* exposed as
        /// <c>SqlException.Number</c>. Those two are never equal.
        ///
        /// Anything unrecognised is now treated as permanent. Retrying a deterministic failure only
        /// delays it, and retrying a non-idempotent write duplicates work.
        ///
        /// Note the message tests are inherently locale-dependent — a server returning localised
        /// messages will not match them. The exception-type test is the reliable half.
        /// </remarks>
        private bool IsTransientException(Exception ex)
        {
            // Walk the inner-exception chain iteratively. The previous version recursed into
            // InnerException with no depth bound and no cycle guard.
            const int MaxDepth = 8;

            for (int depth = 0; ex != null && depth < MaxDepth; ex = ex.InnerException, depth++)
            {
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

                // A timeout exception TYPE is transient whatever the message wording, which matters
                // given the message tests above are locale-dependent.
                if (ex.GetType().Name.ToLowerInvariant().Contains("timeout"))
                    return true;
            }

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
                return await OpenconnectionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Was Task.Run(() => Openconnection(), ct): a pool thread parked on a blocking open,
                // which the token could not interrupt once it had started. OpenconnectionAsync goes
                // through DbConnection.OpenAsync where the provider supports it.
                return await ResilientPipeline.ExecuteAsync(async ct =>
                {
                    return await OpenconnectionAsync(ct).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
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
                            var result = await dbCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                            return result != null;
                        }
                        else
                        {
                            // Fallback for providers whose command is not a DbCommand and so has
                            // no async execute. Task.Run is the honest offload here -- there is no
                            // awaitable operation to reach for.
                            return await Task.Run(() =>
                            {
                                var result = cmd.ExecuteScalar();
                                return result != null;
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                else if (Dataconnection.ConnectionStatus == ConnectionState.Closed)
                {
                    var state = await OpenConnectionResilientAsync(cancellationToken).ConfigureAwait(false);
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
                DataSourceType.AzureSQL => "SELECT 1",
                DataSourceType.SqlCompact => "SELECT 1",
                DataSourceType.Mysql => "SELECT 1",
                DataSourceType.MariaDB => "SELECT 1",
                DataSourceType.Postgre => "SELECT 1",
                DataSourceType.SqlLite => "SELECT 1",

                // Engines that require a FROM. Firebird and Hana previously fell through to the bare
                // "SELECT 1" default, which is a syntax error on both — so the health check reported
                // an otherwise-healthy connection as unhealthy. Both ship as drivers here.
                DataSourceType.Oracle => "SELECT 1 FROM DUAL",
                DataSourceType.DB2 => "SELECT 1 FROM SYSIBM.SYSDUMMY1",
                DataSourceType.FireBird => "SELECT 1 FROM RDB$DATABASE",
                DataSourceType.Hana => "SELECT 1 FROM DUMMY",

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
