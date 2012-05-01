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
    public class SqlCommand : global::System.Data.Common.DbCommand
    {
        public SqlCommand(global::System.Data.SqlClient.SqlCommand command, IExecuteReaderProvider executeReaderProvider)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (executeReaderProvider == null) throw new ArgumentNullException("executeReaderProvider");

            this.command = command;
            this.executeReaderProvider = executeReaderProvider;
        }

        public SqlCommand(IExecuteReaderProvider cache)
            : this(new global::System.Data.SqlClient.SqlCommand(), cache)
        {
        }

        public SqlCommand(string commandText, IExecuteReaderProvider executeReaderProvider)
            : this(new global::System.Data.SqlClient.SqlCommand(commandText), executeReaderProvider)
        {
        }

        public SqlCommand(string commandText, global::System.Data.SqlClient.SqlConnection connection, IExecuteReaderProvider executeReaderProvider)
            : this(new global::System.Data.SqlClient.SqlCommand(commandText, connection), executeReaderProvider)
        {
        }

        public SqlCommand(string commandText, global::System.Data.SqlClient.SqlConnection connection, global::System.Data.SqlClient.SqlTransaction transaction, IExecuteReaderProvider executeReaderProvider)
            : this(new global::System.Data.SqlClient.SqlCommand(commandText, connection, transaction), executeReaderProvider)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.command.Dispose();
            }

            base.Dispose(disposing);
        }

        private IExecuteReaderProvider executeReaderProvider;

        private global::System.Data.SqlClient.SqlCommand command;

        public override void Cancel()
        {
            this.command.Cancel();
        }

        public override string CommandText
        {
            get { return this.command.CommandText; }
            set { this.command.CommandText = value; }
        }

        public override int CommandTimeout
        {
            get { return this.command.CommandTimeout; }
            set { this.command.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return this.command.CommandType; }
            set { this.command.CommandType = value; }
        }

        protected override DbParameter CreateDbParameter()
        {
            return this.command.CreateParameter();
        }

        protected override DbConnection DbConnection
        {
            get { return this.command.Connection; }
            set { this.command.Connection = (global::System.Data.SqlClient.SqlConnection)value; }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return this.command.Parameters; }
        }

        protected override DbTransaction DbTransaction
        {
            get { return this.command.Transaction; }
            set { this.command.Transaction = (global::System.Data.SqlClient.SqlTransaction)value; }
        }

        public override bool DesignTimeVisible
        {
            get { return this.command.DesignTimeVisible; }
            set { this.command.DesignTimeVisible = value; }
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return this.executeReaderProvider.ExecuteReader(this.command, behavior);
        }

        public override int ExecuteNonQuery()
        {
            return this.command.ExecuteNonQuery();
        }

        public override object ExecuteScalar()
        {
            var reader = this.ExecuteDbDataReader(CommandBehavior.Default);

            reader.Read();

            return reader.GetValue(0);
        }

        public override void Prepare()
        {
            this.command.Prepare();
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return this.command.UpdatedRowSource; }
            set { this.command.UpdatedRowSource = value; }
        }
    }
}
