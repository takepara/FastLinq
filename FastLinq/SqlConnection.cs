#region LICENSE
// This software is licensed under the New BSD License.
// 
// Copyright (c) 2011, Hiroaki SHIBUKI
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// 
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// 
// * Neither the name of http://hidori.jp/ nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Data;
using System.Data.Common;

namespace Mjollnir.Data.SqlClient
{
    public class SqlConnection : global::System.Data.Common.DbConnection
    {
        public SqlConnection(global::System.Data.SqlClient.SqlConnection connection, IExecuteReaderProvider executeReaderProvider)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (executeReaderProvider == null) throw new ArgumentNullException("executeReaderProvider");

            this.connection = connection;
            this.executeReaderProvider = executeReaderProvider;
        }

        public SqlConnection(IExecuteReaderProvider cache)
            : this(new global::System.Data.SqlClient.SqlConnection(), cache)
        {
        }

        public SqlConnection(string connectionString, IExecuteReaderProvider cache)
            : this(new global::System.Data.SqlClient.SqlConnection(connectionString), cache)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.connection.Dispose();
            }

            base.Dispose(disposing);
        }

        private global::System.Data.SqlClient.SqlConnection connection;

        private IExecuteReaderProvider executeReaderProvider;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return this.connection.BeginTransaction(isolationLevel);
        }

        public override void ChangeDatabase(string databaseName)
        {
            this.connection.BeginTransaction(databaseName);
        }

        public override void Close()
        {
            this.connection.Close();
        }

        public override string ConnectionString
        {
            get { return this.connection.ConnectionString; }
            set { this.connection.ConnectionString = value; }
        }

        protected override DbCommand CreateDbCommand()
        {
            var command = this.connection.CreateCommand();

            return new SqlCommand(command, this.executeReaderProvider);
        }

        public override string DataSource
        {
            get { return this.connection.DataSource; }
        }

        public override string Database
        {
            get { return this.connection.Database; }
        }

        public override void Open()
        {
            this.connection.Open();
        }

        public override string ServerVersion
        {
            get { return this.connection.ServerVersion; }
        }

        public override ConnectionState State
        {
            get { return this.connection.State; }
        }
    }
}
