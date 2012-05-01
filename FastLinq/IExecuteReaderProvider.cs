using System.Data;
using System.Data.Common;

namespace Mjollnir.Data.SqlClient
{
    public interface IExecuteReaderProvider
    {
        DbDataReader ExecuteReader(DbCommand command, CommandBehavior behavior);
    }
}
