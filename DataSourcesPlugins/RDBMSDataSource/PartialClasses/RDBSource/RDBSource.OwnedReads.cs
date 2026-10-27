using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource
    {
        private readonly AsyncLocal<NativeEntityReadScope?> _entityReadContext = new();
        private readonly object _entityMetadataGate = new();
        private sealed class NativeEntityReadScope
        {
            public RDBDataConnection Connection { get; }
            public IDbTransaction? Transaction { get; }
            public ErrorsInfo Outcome { get; }
            public NativeEntityReadScope(RDBDataConnection connection, IDbTransaction? transaction, ErrorsInfo outcome)
            { Connection = connection; Transaction = transaction; Outcome = outcome; }
        }
        protected bool HasDefaultNativeReadHooks =>
            GetType().GetMethod(nameof(GetEntityAsync), new[] { typeof(string), typeof(List<AppFilter>) })?.DeclaringType == typeof(RDBSource) &&
            GetType().GetMethod(nameof(GetDataCommand), Type.EmptyTypes)?.DeclaringType == typeof(RDBSource) &&
            GetType().GetMethod(nameof(Openconnection), Type.EmptyTypes)?.DeclaringType == typeof(RDBSource) &&
            DMEEditor is IScopedEditorErrors;

        protected Task<EntityReadResult> ReadOwnedEntityAsync(string entityName, List<AppFilter> filters,
            CancellationToken cancellationToken)
        {
            // No early-cancellation task abandonment: completion means reader/command/owned
            // connection cleanup has physically finished. The UoW decides whether to publish.
            return Task.Run(() =>
            {
                var prior = _entityReadContext.Value;
                var outcome = new ErrorsInfo { Flag = Errors.Ok };
                var hadFailure = false;
                string? firstFailureMessage = null;
                void ObserveFailure(object? sender, PropertyChangedEventArgs args)
                {
                    if (outcome.Flag != Errors.Ok && outcome.Flag != Errors.Warning && outcome.Flag != Errors.Information)
                    {
                        hadFailure = true;
                        if (args.PropertyName == nameof(ErrorsInfo.Message) && firstFailureMessage == null)
                            firstFailureMessage = outcome.Message;
                    }
                }
                outcome.PropertyChanged += ObserveFailure;
                IDisposable? editorScope = null;
                DbConnection? ownedConnection = null;
                object? transactionReadOwner = null;
                var rows = new List<object>();
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var editor = DMEEditor ?? throw new NotSupportedException("Owned entity reads require an editor outcome owner.");
                    editorScope = (editor as IScopedEditorErrors)?.BeginOperationErrors(outcome)
                        ?? throw new NotSupportedException("The editor cannot isolate an entity read outcome.");
                    var template = _sharedConnection as RDBDataConnection
                        ?? throw new NotSupportedException("An RDB connection is required for an owned entity read.");
                    IDbTransaction? transaction;
                    lock (_transactionOwnerGate)
                    {
                        var sameOwner = _ownedTransactionOwner != null && ReferenceEquals(_ownedTransactionOwner, _ownedTransactionFlow.Value);
                        if (sameOwner && _ownedTransactionUnresolved)
                            throw new InvalidOperationException("An unresolved transaction cannot admit another read.");
                        transaction = _ownedTransactionOwner == null || sameOwner ? OpenPhysicalTransaction() : null;
                        if (sameOwner && transaction != null)
                        { transactionReadOwner = _ownedTransactionOwner; _ownedTransactionReaders++; }
                    }
                    var connection = template;
                    if (transaction == null)
                    {
                        var native = template.DbConn as DbConnection
                            ?? throw new NotSupportedException("The native provider cannot create an independent entity read connection.");
                        var connectionString = template.ReplaceValueFromConnectionString();
                        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("The owned entity read connection is not configured.");
                        if (DatasourceType == Utilities.DataSourceType.SqlLite && connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
                            throw new NotSupportedException("An isolated in-memory database cannot be cloned for an entity read.");
                        var factory = DbProviderFactories.GetFactory(native)
                            ?? throw new NotSupportedException("The native provider has no connection factory.");
                        ownedConnection = factory.CreateConnection()
                            ?? throw new NotSupportedException("The native provider cannot create an owned entity read connection.");
                        ownedConnection.ConnectionString = connectionString;
                        ownedConnection.Open();
                        cancellationToken.ThrowIfCancellationRequested();
                        connection = new RDBDataConnection(editor) { DbConn = ownedConnection,
                            ConnectionStatus = ConnectionState.Open, ConnectionProp = template.ConnectionProp,
                            DataSourceDriver = template.DataSourceDriver, Logger = Logger, ErrorObject = outcome };
                    }
                    _entityReadContext.Value = new NativeEntityReadScope(connection, transaction, outcome);
                    // Call the virtual provider stream: adapters/instrumentation retain their semantics.
                    var stream = GetEntity(entityName, filters)
                        ?? throw new InvalidOperationException("The provider returned no entity stream.");
                    foreach (var row in stream)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        rows.Add(row);
                    }
                }
                catch (Exception ex) { RecordEntityReadFailure(outcome, ex); }
                finally
                {
                    try { ownedConnection?.Dispose(); }
                    catch (Exception ex) { RecordEntityReadFailure(outcome, ex); }
                    if (transactionReadOwner != null)
                    {
                        lock (_transactionOwnerGate) _ownedTransactionReaders--;
                    }
                    try { editorScope?.Dispose(); }
                    catch (Exception ex) { RecordEntityReadFailure(outcome, ex); }
                    _entityReadContext.Value = prior;
                    outcome.PropertyChanged -= ObserveFailure;
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (hadFailure)
                {
                    outcome.Flag = Errors.Failed;
                    outcome.Message = firstFailureMessage ?? "Entity read was not acknowledged.";
                }
                return new EntityReadResult(outcome, rows);
            });
        }
        private void RecordEntityReadFailure(ErrorsInfo outcome, Exception exception)
        {
            outcome.Flag = Errors.Failed; outcome.Message = "Entity read was not acknowledged.";
            if (outcome.Ex == null) outcome.Ex = exception;
            else if (!ReferenceEquals(outcome.Ex, exception))
                outcome.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = "Additional entity read/cleanup failure.", Ex = exception });
            try
            {
                if (DMEEditor != null) DMEEditor.AddLogMessage("EntityRead", exception.ToString(), DateTime.Now, 0, DatasourceName, Errors.Failed);
                else Logger?.WriteLog("Entity read failed on " + DatasourceName + ": " + exception);
            }
            catch (Exception reportingError)
            {
                outcome.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = "Entity read diagnostics could not be recorded.", Ex = reportingError });
            }
        }
    }
}
