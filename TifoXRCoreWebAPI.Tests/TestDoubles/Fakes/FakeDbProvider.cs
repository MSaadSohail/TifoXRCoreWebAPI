// <copyright file="FakeDbProvider.cs" company="Global Mobile Software LLC">
// © 2025 Global Mobile Software LLC. All rights reserved.
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/19/2025</date>
// <summary>
// Test double for IDbProvider. It returns a fake DbConnection/DbCommand stack that:
//  - can queue DbDataReaders, scalars, and non-query row counts
//  - returns a NEW DbDataReader each time from queued factories (fallback to ctor factory)
//  - records executed SQL (normalized)
//  - supports ExecuteNonQuery (optionally throwing based on SQL predicate)
//  - supports transactions and counts Commit/Rollback
// </summary>

using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

namespace GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Fakes
{
    internal sealed class FakeDbProvider : IDbProvider
    {
        // Base reader factory used when no queued readers exist
        private readonly Func<DbDataReader> _readerFactory;

        // Scriptable queues
        private readonly Queue<Func<DbDataReader>> _readerResults = new();
        private readonly Queue<int> _nonQueryResults = new();

        /// <summary>Queue of values that ExecuteScalar() will dequeue from (nullable).</summary>
        public Queue<object?> ScalarResults { get; }

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

        public FakeDbProvider(Func<DbDataReader> readerFactory, Queue<object?>? scalarResults = null)
        {
            _readerFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
            ScalarResults = scalarResults ?? new Queue<object?>();
        }

        // ----------------------- scripting helpers ------------------------

        public void EnqueueReader(Func<DbDataReader> factory) => _readerResults.Enqueue(factory);
        public void EnqueueScalar(object? value) => ScalarResults.Enqueue(value);
        public void EnqueueNonQuery(int affectedRows) => _nonQueryResults.Enqueue(affectedRows);

        internal DbDataReader NextReader()
            => _readerResults.Count > 0 ? _readerResults.Dequeue().Invoke() : _readerFactory();

        internal int NextNonQuery()
            => _nonQueryResults.Count > 0 ? _nonQueryResults.Dequeue() : 1;

        // --------------------------- IDbProvider ---------------------------

        public Task<DbConnection> OpenConnectionAsync()
            => Task.FromResult<DbConnection>(new FakeConnection(this));

        public DbCommand CreateCommand(DbConnection connection, string commandText, DbTransaction? transaction = null)
        {
            var fakeConn = connection as FakeConnection ?? new FakeConnection(this);
            return new FakeCommand(this, fakeConn)
            {
                CommandText = commandText,
                Transaction = transaction
            };
        }

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

            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
                => new FakeTransaction(_owner, this, isolationLevel);

            protected override ValueTask<DbTransaction> BeginDbTransactionAsync(
                IsolationLevel isolationLevel,
                CancellationToken cancellationToken = default)
                => new ValueTask<DbTransaction>(new FakeTransaction(_owner, this, isolationLevel));

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
        }

        private sealed class FakeCommand : DbCommand
        {
            private readonly FakeDbProvider _owner;
            private readonly FakeConnection _conn;

            public FakeCommand(FakeDbProvider owner, FakeConnection conn)
            {
                _owner = owner;
                _conn = conn;
            }

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

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                RecordSql();
                return _owner.NextReader();
            }

            public override int ExecuteNonQuery()
            {
                RecordSql();

                if (_owner.ShouldThrowOnNonQuery?.Invoke(Normalize(CommandText)) == true)
                    throw _owner.NonQueryException;

                return _owner.NextNonQuery();
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
                => (sql ?? string.Empty).Trim()
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("\t", " ");
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
