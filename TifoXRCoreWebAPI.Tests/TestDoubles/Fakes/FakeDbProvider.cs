// <copyright file="FakeDbProvider.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/15/2025</date>
// <summary>Minimal IDbProvider test double that supplies fresh DbDataReader instances for repository layer testing.</summary>


using System.Data;
using System.Data.Common;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

namespace TifoXRCoreWebAPI.Tests.TestDoubles.Fakes
{
    /// <summary>
    /// Minimal test double for <see cref="IDbProvider"/> that uses a reader factory to avoid reusing
    /// a consumed <see cref="DbDataReader"/> across multiple calls. Each ExecuteReader/ExecuteReaderAsync
    /// on the command fetches a NEW reader by calling the provided factory.
    /// </summary>
    internal sealed class FakeDbProvider : IDbProvider
    {
        private readonly Func<DbDataReader> _readerFactory;

        /// <param name="readerFactory">
        /// Factory that returns a NEW, unconsumed DbDataReader on every invocation.
        /// In tests, pass () => dataTable.CreateDataReader().
        /// </param>
        public FakeDbProvider(Func<DbDataReader> readerFactory) => _readerFactory = readerFactory;

        public Task<DbConnection> OpenConnectionAsync()
            => Task.FromResult<DbConnection>(new FakeConnection());

        // Signature matches interface with optional transaction (= null).
        public DbCommand CreateCommand(DbConnection connection, string commandText, DbTransaction? transaction = null)
            => new FakeCommand(_readerFactory) { CommandText = commandText, Transaction = transaction };

        public DbParameter CreateParameter(string name, object? value)
            => new FakeParameter { ParameterName = name, Value = value };

        // ---- minimal fakes below ----

        private sealed class FakeConnection : DbConnection
        {
            public override string ConnectionString { get; set; } = "";
            public override string Database => "test";
            public override string DataSource => "fake";
            public override string ServerVersion => "0";
            public override ConnectionState State => ConnectionState.Open;

            // No-ops: this is a test double; disposal is safe no-op.
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
                => throw new NotImplementedException();
            protected override DbCommand CreateDbCommand()
                => throw new NotImplementedException();
        }

        private sealed class FakeCommand : DbCommand
        {
            private readonly Func<DbDataReader> _readerFactory;

            public FakeCommand(Func<DbDataReader> readerFactory) => _readerFactory = readerFactory;

            public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; } = 30;
            public override CommandType CommandType { get; set; } = CommandType.Text;
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

            protected override DbConnection DbConnection { get; set; } = new FakeConnection();
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeParamCollection();
            protected override DbTransaction? DbTransaction { get; set; }

            public override void Cancel() { }
            public override int ExecuteNonQuery() => throw new NotImplementedException();
            public override object? ExecuteScalar() => throw new NotImplementedException();
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeParameter();

            /// <summary>
            /// IMPORTANT: ExecuteReaderAsync IS virtual in DbCommand. We rely on the base implementation,
            /// which calls our override of ExecuteDbDataReader(CommandBehavior), where we return
            /// a NEW reader via the factory on each call. This avoids "reader already in use" issues.
            /// </summary>
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
                => _readerFactory();

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
        }

        private sealed class FakeParameter : DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
            public override bool IsNullable { get; set; }
            public override string ParameterName { get; set; } = "";
            public override string SourceColumn { get; set; } = "";
            public override object? Value { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override int Size { get; set; }
            public override void ResetDbType() { }
        }
    }
}
