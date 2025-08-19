// <copyright file="FakeDbProvider.cs" company="Global Mobile Software LLC">
// © 2025 Global Mobile Software LLC. All rights reserved.
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/19/2025</date>
// <summary>
// Test double for IDbProvider. It returns a fake DbConnection/DbCommand stack that:
//  - returns a NEW DbDataReader each time via an injected factory
//  - records executed SQL strings
//  - supports ExecuteNonQuery (optionally throwing based on SQL)
//  - supports ExecuteScalar via a queued result sequence
//  - supports transactions and counts Commit/Rollback

using System.Data;
using System.Data.Common;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

namespace GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Fakes
{
    internal sealed class FakeDbProvider : IDbProvider
    {
<<<<<<< Updated upstream
<<<<<<< Updated upstream
        private enum StepKind { Scalar, NonQuery, Reader, ReaderFromFactory }
        private sealed class Step
        {
            public StepKind Kind { get; init; }
            public object? ScalarResult { get; init; }
            public int NonQueryResult { get; init; }
            public Func<DbDataReader>? ReaderFactory { get; init; }
        }
        private readonly Queue<Step> _steps = new();
        private readonly Func<DbDataReader> _readerFactory;

        /// <param name="readerFactory">
        /// Factory that returns a NEW, unconsumed DbDataReader on every invocation.
        /// In tests, pass () => dataTable.CreateDataReader().
        /// </param>
        public FakeDbProvider(Func<DbDataReader> readerFactory) => _readerFactory = readerFactory;
        public void EnqueueScalar(object? value) => _steps.Enqueue(new Step { Kind = StepKind.Scalar, ScalarResult = value });
        public void EnqueueNonQuery(int count) => _steps.Enqueue(new Step { Kind = StepKind.NonQuery, NonQueryResult = count });
        public void EnqueueReader(Func<DbDataReader> factory) =>
            _steps.Enqueue(new Step { Kind = StepKind.Reader, ReaderFactory = factory });
=======
        // --------- scriptable behavior/state exposed to tests ---------
        private readonly Func<DbDataReader> _readerFactory;

=======
        // --------- scriptable behavior/state exposed to tests ---------
        private readonly Func<DbDataReader> _readerFactory;

>>>>>>> Stashed changes
        /// <summary>Queue of values that ExecuteScalar() will dequeue from.</summary>
        public Queue<object> ScalarResults { get; }

        /// <summary>Optional predicate: when true for a SQL, ExecuteNonQuery() throws <see cref="NonQueryException"/>.</summary>
        public Func<string, bool>? ShouldThrowOnNonQuery { get; set; }

        /// <summary>Exception thrown by ExecuteNonQuery() when <see cref="ShouldThrowOnNonQuery"/> returns true.</summary>
        public Exception NonQueryException { get; set; } = new InvalidOperationException("Configured failure in ExecuteNonQuery");

        /// <summary>All normalized SQL strings executed by commands created by this provider.</summary>
        public List<string> ExecutedSql { get; } = new();

        /// <summary>Transaction commit count.</summary>
        public int Commits { get; internal set; }

        /// <summary>Transaction rollback count.</summary>
        public int Rollbacks { get; internal set; }

        // ------------------------------ ctor ------------------------------

        public FakeDbProvider(Func<DbDataReader> readerFactory, Queue<object>? scalarResults = null)
        {
            _readerFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
            ScalarResults = scalarResults ?? new Queue<object>();
        }

        // --------------------------- IDbProvider ---------------------------
<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes

        public Task<DbConnection> OpenConnectionAsync()
            => Task.FromResult<DbConnection>(new FakeConnection(this));

        public DbCommand CreateCommand(DbConnection connection, string commandText, DbTransaction? transaction = null)
<<<<<<< Updated upstream
<<<<<<< Updated upstream
    => new FakeCommand(_readerFactory, _steps) { CommandText = commandText, Transaction = transaction };
=======
=======
>>>>>>> Stashed changes
        {
            // Accept any DbConnection for compatibility; we use our own fake in tests.
            var fakeConn = connection as FakeConnection ?? new FakeConnection(this);
            return new FakeCommand(this, fakeConn)
            {
                CommandText = commandText,
                Transaction = transaction
            };
        }
<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes

        public DbParameter CreateParameter(string name, object? value)
            => new FakeParameter { ParameterName = name, Value = value };

        // =====================================================================
        //                             Inner types
        // =====================================================================

        private sealed class FakeConnection : DbConnection
        {
            private readonly FakeDbProvider _owner;

            public FakeConnection(FakeDbProvider owner) => _owner = owner;

            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => "test";
            public override string DataSource => "fake";
            public override string ServerVersion => "0";
            public override ConnectionState State => ConnectionState.Open;

<<<<<<< Updated upstream
<<<<<<< Updated upstream
            public FakeTransaction? CurrentTx { get; private set; }
            public bool BeganTx { get; private set; }

=======
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }

            // PUBLIC BeginTransaction/BeginTransactionAsync route into these overrides.

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
<<<<<<< Updated upstream
<<<<<<< Updated upstream
            {
                BeganTx = true;
                CurrentTx = new FakeTransaction(this);
                return CurrentTx;
            }


=======
=======
>>>>>>> Stashed changes
                => new FakeTransaction(_owner, this, isolationLevel);

            // *** IMPORTANT: Your TFM expects ValueTask<DbTransaction> here. ***
            protected override ValueTask<DbTransaction> BeginDbTransactionAsync(
                IsolationLevel isolationLevel,
                CancellationToken cancellationToken = default)
                => new ValueTask<DbTransaction>(new FakeTransaction(_owner, this, isolationLevel));
<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes

            protected override DbCommand CreateDbCommand()
                => new FakeCommand(_owner, this);
        }

        private sealed class FakeTransaction : DbTransaction
        {
            private readonly FakeDbProvider _owner;
            private readonly FakeConnection _conn;

            public FakeTransaction(FakeDbProvider owner, FakeConnection conn, IsolationLevel isolation)
            {
                _owner = owner;
                _conn = conn;
                IsolationLevel = isolation;
            }

            public override IsolationLevel IsolationLevel { get; }
            protected override DbConnection DbConnection => _conn;

            public override void Commit() => _owner.Commits++;
            public override void Rollback() => _owner.Rollbacks++;
<<<<<<< Updated upstream
        }
        private sealed class FakeTransaction : DbTransaction
        {
            private readonly FakeConnection _conn;
            public bool Committed { get; private set; }
            public bool RolledBack { get; private set; }

            public FakeTransaction(FakeConnection conn) => _conn = conn;
            public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
            protected override DbConnection DbConnection => _conn;

            public override void Commit() { Committed = true; }
            public override void Rollback() { RolledBack = true; }
            public override Task CommitAsync(CancellationToken cancellationToken = default)
            { Committed = true; return Task.CompletedTask; }
            public override Task RollbackAsync(CancellationToken cancellationToken = default)
            { RolledBack = true; return Task.CompletedTask; }
=======
>>>>>>> Stashed changes
        }

        private sealed class FakeCommand : DbCommand
        {
<<<<<<< Updated upstream
<<<<<<< Updated upstream
            private readonly Func<DbDataReader> _defaultReaderFactory; // your existing factory
            private readonly Queue<Step> _steps; // NEW: consume scripted steps

            public FakeCommand(Func<DbDataReader> readerFactory, Queue<Step> steps)
            {
                _defaultReaderFactory = readerFactory;
                _steps = steps;
=======
            private readonly FakeDbProvider _owner;
            private readonly FakeConnection _conn;

            public FakeCommand(FakeDbProvider owner, FakeConnection conn)
            {
                _owner = owner;
                _conn = conn;
>>>>>>> Stashed changes
            }

=======
            private readonly FakeDbProvider _owner;
            private readonly FakeConnection _conn;

            public FakeCommand(FakeDbProvider owner, FakeConnection conn)
            {
                _owner = owner;
                _conn = conn;
            }

>>>>>>> Stashed changes
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; } = 30;
            public override CommandType CommandType { get; set; } = CommandType.Text;
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

            protected override DbConnection DbConnection { get => _conn; set { } }
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeParamCollection();
            protected override DbTransaction? DbTransaction { get; set; }

            public override void Cancel() { }
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeParameter();

<<<<<<< Updated upstream
<<<<<<< Updated upstream
            public override object? ExecuteScalar()
            {
                var step = NextOrDefault();
                if (step == null) throw new InvalidOperationException("No scripted step for ExecuteScalar.");
                if (step.Kind != StepKind.Scalar)
                    throw new InvalidOperationException($"Expected Scalar step, got {step.Kind} for SQL: {CommandText}");
                return step.ScalarResult;
            }
            public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
                => Task.FromResult(ExecuteScalar());

            public override int ExecuteNonQuery()
            {
                var step = NextOrDefault();
                if (step == null) throw new InvalidOperationException("No scripted step for ExecuteNonQuery.");
                if (step.Kind != StepKind.NonQuery)
                    throw new InvalidOperationException($"Expected NonQuery step, got {step.Kind} for SQL: {CommandText}");
                return step.NonQueryResult;
            }
            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
                => Task.FromResult(ExecuteNonQuery());

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                var step = NextOrDefault();
                if (step == null)
                {
                    // Fallback: maintain your original behavior for simple cases
                    return _defaultReaderFactory();
                }
                if (step.Kind == StepKind.Reader)
                    return step.ReaderFactory!();
                throw new InvalidOperationException($"Expected Reader step, got {step.Kind} for SQL: {CommandText}");
            }

            private Step? NextOrDefault() => _steps.Count > 0 ? _steps.Dequeue() : null;


            private sealed class FakeParamCollection : DbParameterCollection
=======
            /// <summary>Base ExecuteReader/ExecuteReaderAsync delegate here.</summary>
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
>>>>>>> Stashed changes
=======
            /// <summary>Base ExecuteReader/ExecuteReaderAsync delegate here.</summary>
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
>>>>>>> Stashed changes
            {
                RecordSql();
                return _owner._readerFactory();
            }

            public override int ExecuteNonQuery()
            {
                RecordSql();

                if (_owner.ShouldThrowOnNonQuery?.Invoke(Normalize(CommandText)) == true)
                    throw _owner.NonQueryException;

                // Pretend one row affected for DML.
                return 1;
            }

            public override object? ExecuteScalar()
            {
                RecordSql();

                if (_owner.ScalarResults.Count == 0)
                    throw new InvalidOperationException("No scalar results configured for ExecuteScalar.");

                return _owner.ScalarResults.Dequeue();
            }

            private void RecordSql()
            {
                _owner.ExecutedSql.Add(Normalize(CommandText));
            }

            private static string Normalize(string sql)
                => (sql ?? string.Empty).Trim().Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        }

        private sealed class FakeParamCollection : DbParameterCollection
        {
            private readonly List<DbParameter> _list = new();

            public override int Add(object value) { _list.Add((DbParameter)value); return _list.Count - 1; }
            public override void AddRange(Array values) { foreach (var v in values) Add(v!); }
            public override void Clear() => _list.Clear();
            public override bool Contains(object value) => _list.Contains((DbParameter)value);
            public override bool Contains(string value) => _list.Any(p => p.ParameterName == value);
            public override void CopyTo(Array array, int index) => _list.ToArray().CopyTo(array, index);
            public override int Count => _list.Count;
            public override System.Collections.IEnumerator GetEnumerator() => _list.GetEnumerator();
            protected override DbParameter GetParameter(int index) => _list[index];
            protected override DbParameter GetParameter(string parameterName) => _list.First(p => p.ParameterName == parameterName);
            public override int IndexOf(object value) => _list.IndexOf((DbParameter)value);
            public override int IndexOf(string parameterName) => _list.FindIndex(p => p.ParameterName == parameterName);
            public override void Insert(int index, object value) => _list.Insert(index, (DbParameter)value);
            public override bool IsFixedSize => false;
            public override bool IsReadOnly => false;
            public override bool IsSynchronized => false;
            public override void Remove(object value) => _list.Remove((DbParameter)value);
            public override void RemoveAt(int index) => _list.RemoveAt(index);
            public override void RemoveAt(string parameterName)
            {
                var i = IndexOf(parameterName);
                if (i >= 0) _list.RemoveAt(i);
            }
            protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
            protected override void SetParameter(string parameterName, DbParameter value)
            {
                var i = IndexOf(parameterName);
                if (i >= 0) _list[i] = value; else _list.Add(value);
            }
            public override object SyncRoot => this;
        }

        private sealed class FakeParameter : DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
            public override bool IsNullable { get; set; }
            public override string ParameterName { get; set; } = string.Empty;
            public override string SourceColumn { get; set; } = string.Empty;
            public override object? Value { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override int Size { get; set; }
            public override void ResetDbType() { }
        }
    }
}
