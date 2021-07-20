// ====================================================================
// Description: MySQL Schema Provider for CodeSmith 5.x
// Author: David Neal -- www.ChristianASP.NET
// Modified By: Blake Niemyjski -- http://windowscoding.com
// ====================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace SchemaExplorer
{
    /// <summary>
    /// MySQL Schema Provider
    /// </summary>
    public class MySQLSchemaProvider : IDbSchemaProvider
    {
        #region Properties

        #region IDbSchemaProvider Members

        /// <summary>
        /// Gets the name of this schema provider.
        /// </summary>
        /// <value></value>
        public string Name
        {
            get { return "MySQLSchemaProvider"; }
        }


        /// <summary>
        /// Gets the description for this schema provider.
        /// </summary>
        /// <value></value>
        public string Description
        {
            get { return "MySQL Schema Provider"; }
        }

        #endregion

        #endregion

        #region public string GetDatabaseName(string connectionString)

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <returns>The name of the database</returns>
        public string GetDatabaseName(string connectionString)
        {
            // Problem is, INFORMATION_SCHEMA appears to retrieve information for
            // every database instead of just the current database
            // So, we'll need to parse the connection string instead of pulling
            // the current database directly

            Regex databaseNameRegex = new Regex(@"Database\W*=\W*(?<database>[^;]*)", RegexOptions.IgnoreCase);
            Match databaseNameMatch = databaseNameRegex.Match(connectionString);

            if (databaseNameMatch.Success)
            {
                return databaseNameMatch.Groups["database"].ToString();
            }
            return connectionString;
        }

        #endregion

        #region public string GetViewText(string connectionString, ViewSchema view)

        /// <summary>
        /// Gets the definition for a given view.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="view"></param>
        /// <returns></returns>
        public string GetViewText(string connectionString, ViewSchema view)
        {
            StringBuilder sb = new StringBuilder();
            string commandText = string.Format("SELECT VIEW_DEFINITION FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'", view.Database.Name, view.Name);

            using (DbConnection connection = CreateConnection(connectionString))
            {
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        sb.Append(reader.GetString(0));
                    }

                    if (!reader.IsClosed)
                        reader.Close();
                }


                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            return sb.ToString();
        }

        #endregion

        #region public TableSchema[] GetTables(string connectionString, DatabaseSchema database)

        /// <summary>
        /// Gets all of the tables available in the database.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="database"></param>
        /// <returns></returns>
        public TableSchema[] GetTables(string connectionString, DatabaseSchema database)
        {
            string commandText = string.Format("SELECT TABLE_NAME, '' OWNER, CREATE_TIME,TABLE_COMMENT FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND TABLE_TYPE = 'BASE TABLE' ORDER BY 1", database.Name);
            var tableSchema = new List<TableSchema>();

            using (DbConnection connection = CreateConnection(connectionString))
            {
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        DateTime createTime = !reader.IsDBNull(2) ? reader.GetDateTime(2) : DateTime.MinValue;
                        var tb = new TableSchema(database, reader.GetString(0), reader.GetString(1), createTime);
                        try
                        {
                            tb.Description = reader.GetString(3);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        tableSchema.Add(tb);
                    }

                    if (!reader.IsClosed)
                        reader.Close();
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            return tableSchema.ToArray();
        }

        #endregion

        #region public ViewSchema[] GetViews(string connectionString, DatabaseSchema database)

        /// <summary>
        /// Gets all the views available for a given database.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="database"></param>
        /// <returns></returns>
        public ViewSchema[] GetViews(string connectionString, DatabaseSchema database)
        {
            string commandText = string.Format("SELECT TABLE_NAME, '' OWNER, CREATE_TIME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND TABLE_TYPE = 'VIEW' ORDER BY 1", database.Name);
            List<ViewSchema> viewSchema = new List<ViewSchema>();

            using (DbConnection connection = CreateConnection(connectionString))
            {
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        // MySQL views don't have a create date?
                        DateTime dateCreated = (reader.IsDBNull(2) == false) ? reader.GetDateTime(2) : DateTime.MinValue;

                        viewSchema.Add(new ViewSchema(database, reader.GetString(0), reader.GetString(1), dateCreated));
                    }

                    if (!reader.IsClosed)
                        reader.Close();
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            return viewSchema.ToArray();
        }

        #endregion

        #region public CommandSchema[] GetCommands(string connectionString, DatabaseSchema database)

        /// <summary>
        /// Gets all commands (stored procedures) for the given database.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="database"></param>
        /// <returns></returns>
        public CommandSchema[] GetCommands(string connectionString, DatabaseSchema database)
        {
            string commandText = string.Format("SELECT ROUTINE_NAME, '' OWNER, CREATED FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA = '{0}' AND ROUTINE_TYPE = 'PROCEDURE' ORDER BY 1", database.Name);
            List<CommandSchema> commandSchema = new List<CommandSchema>();

            using (DbConnection connection = CreateConnection(connectionString))
            {
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        commandSchema.Add(new CommandSchema(database, reader.GetString(0), reader.GetString(1), reader.GetDateTime(2)));
                    }

                    if (!reader.IsClosed)
                        reader.Close();
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            return commandSchema.ToArray();
        }

        #endregion

        #region public DataTable GetTableData(string connectionString, TableSchema table)

        /// <summary>
        /// Gets the data from the given table.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="table"></param>
        /// <returns></returns>
        public DataTable GetTableData(string connectionString, TableSchema table)
        {
            string commandText = string.Format("SELECT * FROM {0}", table.Name);
            DataSet dataSet;

            DbProviderFactory factory = CreateDbProviderFactory();
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                dataSet = ConvertDataReaderToDataSet(command.ExecuteReader());

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            if (dataSet.Tables.Count > 0)
                return dataSet.Tables[0];

            return new DataTable(table.Name);
        }

        #endregion

        #region public DataTable GetViewData(string connectionString, ViewSchema view)

        /// <summary>
        /// Gets the data from a given view.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="view"></param>
        /// <returns></returns>
        public DataTable GetViewData(string connectionString, ViewSchema view)
        {
            string commandText = string.Format("SELECT * FROM {0}", view.Name);
            DataSet dataSet;

            DbProviderFactory factory = CreateDbProviderFactory();
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                dataSet = ConvertDataReaderToDataSet(command.ExecuteReader());

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            if (dataSet.Tables.Count > 0)
                return dataSet.Tables[0];

            return new DataTable(view.Name);
        }

        #endregion

        #region public ColumnSchema[] GetTableColumns(string connectionString, TableSchema table)

        /// <summary>
        /// Gets all columns for a given table.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="table"></param>
        /// <returns></returns>
        public ColumnSchema[] GetTableColumns(string connectionString, TableSchema table)
        {
            string commandText = string.Format("SELECT COLUMN_NAME, DATA_TYPE,CHARACTER_MAXIMUM_LENGTH AS CHARACTER_OCTET_LENGTH, NUMERIC_PRECISION,"
                                                + " NUMERIC_SCALE, CASE IS_NULLABLE WHEN 'NO' THEN 0 ELSE 1 END IS_NULLABLE, COLUMN_TYPE"
                                                + " FROM INFORMATION_SCHEMA.COLUMNS"
                                                + " WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'"
                                                + " ORDER BY ORDINAL_POSITION", table.Database.Name, table.Name);

            List<ColumnSchema> columnSchema = new List<ColumnSchema>();

            using (DbConnection connection = CreateConnection(connectionString))
            {
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        string nativeType = reader.GetString(1);
                        long longSize = (reader.IsDBNull(2) == false) ? reader.GetInt64(2) : 0;
                        byte precision = (byte)((reader.IsDBNull(3) == false) ? reader.GetInt32(3) : 0);
                        int scale = (reader.IsDBNull(4) == false) ? reader.GetInt32(4) : 0;
                        bool isNullable = (reader.IsDBNull(5) == false) && reader.GetBoolean(5);
                        string columnType = reader.GetString(6);

                        int size = (longSize < int.MaxValue) ? (int)longSize : int.MaxValue;

                        bool isUnsigned = (columnType.IndexOf("unsigned") > -1);
                        DbType type = GetDbType(nativeType, isUnsigned);

                        columnSchema.Add(new ColumnSchema(table, name, type, nativeType, size, precision, scale, isNullable));
                    }

                    if (!reader.IsClosed)
                        reader.Close();
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            return columnSchema.ToArray();
        }

        public ColumnSchema[] GetTableColumns_bak(string connectionString, TableSchema table)
        {
            string commandText = string.Format("SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_OCTET_LENGTH, NUMERIC_PRECISION,"
                                                + " NUMERIC_SCALE, CASE IS_NULLABLE WHEN 'NO' THEN 0 ELSE 1 END IS_NULLABLE, COLUMN_TYPE"
                                                + " FROM INFORMATION_SCHEMA.COLUMNS"
                                                + " WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'"
                                                + " ORDER BY ORDINAL_POSITION", table.Database.Name, table.Name);

            List<ColumnSchema> columnSchema = new List<ColumnSchema>();

            using (DbConnection connection = CreateConnection(connectionString))
            {
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        string nativeType = reader.GetString(1);
                        long longSize = (reader.IsDBNull(2) == false) ? reader.GetInt64(2) : 0;
                        byte precision = (byte)((reader.IsDBNull(3) == false) ? reader.GetInt32(3) : 0);
                        int scale = (reader.IsDBNull(4) == false) ? reader.GetInt32(4) : 0;
                        bool isNullable = (reader.IsDBNull(5) == false) && reader.GetBoolean(5);
                        string columnType = reader.GetString(6);

                        int size = (longSize < int.MaxValue) ? (int)longSize : int.MaxValue;

                        bool isUnsigned = (columnType.IndexOf("unsigned") > -1);
                        DbType type = GetDbType(nativeType, isUnsigned);

                        columnSchema.Add(new ColumnSchema(table, name, type, nativeType, size, precision, scale, isNullable));
                    }

                    if (!reader.IsClosed)
                        reader.Close();
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            return columnSchema.ToArray();
        }
        #endregion

        #region public ViewColumnSchema[] GetViewColumns(string connectionString, ViewSchema view)

        /// <summary>
        /// Gets the columns for a given view.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="view"></param>
        /// <returns></returns>
        public ViewColumnSchema[] GetViewColumns(string connectionString, ViewSchema view)
        {
            string commandText = string.Format("SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_OCTET_LENGTH, NUMERIC_PRECISION,"
                                                + " NUMERIC_SCALE, CASE IS_NULLABLE WHEN 'NO' THEN 0 ELSE 1 END IS_NULLABLE, COLUMN_TYPE"
                                                + " FROM INFORMATION_SCHEMA.COLUMNS "
                                                + "WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'"
                                                + "ORDER BY ORDINAL_POSITION", view.Database.Name, view.Name);

            List<ViewColumnSchema> viewColumnSchema = new List<ViewColumnSchema>();

            using (DbConnection connection = CreateConnection(connectionString))
            {
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        string nativeType = reader.GetString(1);
                        long longSize = (reader.IsDBNull(2) == false) ? reader.GetInt64(2) : 0;
                        byte precision = (byte)((reader.IsDBNull(3) == false) ? reader.GetInt32(3) : 0);
                        int scale = (reader.IsDBNull(4) == false) ? reader.GetInt32(4) : 0;
                        bool isNullable = (reader.IsDBNull(5) == false) && reader.GetBoolean(5);
                        string columnType = reader.GetString(6);

                        int size = (longSize < int.MaxValue) ? (int)longSize : int.MaxValue;
                        bool isUnsigned = (columnType.IndexOf("unsigned") > -1);
                        DbType type = GetDbType(nativeType, isUnsigned);

                        viewColumnSchema.Add(new ViewColumnSchema(view, name, type, nativeType, size, precision, scale, isNullable));
                    }

                    if (!reader.IsClosed)
                        reader.Close();
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            return viewColumnSchema.ToArray();
        }

        #endregion

        #region public PrimaryKeySchema GetTablePrimaryKey(string connectionString, TableSchema table)

        /// <summary>
        /// Gets the primary key for a given table.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="table"></param>
        /// <returns></returns>
        public PrimaryKeySchema GetTablePrimaryKey(string connectionString, TableSchema table)
        {
            string commandText = string.Format("SELECT t1.CONSTRAINT_NAME, t1.COLUMN_NAME"
                                                + " FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE t1"
                                                + "  INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS t2"
                                                + "  ON t2.TABLE_SCHEMA = t1.TABLE_SCHEMA"
                                                + "  AND t2.TABLE_NAME = t1.TABLE_NAME"
                                                + "  AND t2.CONSTRAINT_NAME = t1.CONSTRAINT_NAME"
                                                + " WHERE t1.TABLE_SCHEMA = '{0}' AND t1.TABLE_NAME = '{1}'"
                                                + " AND t2.CONSTRAINT_TYPE = 'PRIMARY KEY'"
                                                + " ORDER BY t1.ORDINAL_POSITION", table.Database.Name, table.Name);

            DataSet dataSet;

            DbProviderFactory factory = CreateDbProviderFactory();
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                dataSet = ConvertDataReaderToDataSet(command.ExecuteReader());

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                string constraintName = dataSet.Tables[0].Rows[0]["CONSTRAINT_NAME"].ToString();
                string[] members = new string[dataSet.Tables[0].Rows.Count];

                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    members[i] = dataSet.Tables[0].Rows[i]["COLUMN_NAME"].ToString();
                }

                return new PrimaryKeySchema(table, constraintName, members);
            }

            return null;
        }

        #endregion

        #region public TableKeySchema[] GetTableKeys(string connectionString, TableSchema table)

        /// <summary>
        /// Gets all of the table keys for a given table.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="table"></param>
        /// <returns></returns>
        public TableKeySchema[] GetTableKeys(string connectionString, TableSchema table)
        {
            List<TableKeySchema> tableKeySchema = new List<TableKeySchema>();
            tableKeySchema.AddRange(GetMyTableKeys(connectionString, table));
            tableKeySchema.AddRange(GetOthersTableKeys(connectionString, table));

            return tableKeySchema.ToArray();
        }

        private IEnumerable<TableKeySchema> GetMyTableKeys(string connectionString, SchemaObjectBase table)
        {
            string commandText = string.Format("SELECT CONSTRAINT_NAME"
                                                + " FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t1"
                                                + " WHERE t1.TABLE_SCHEMA = '{0}' AND t1.TABLE_NAME = '{1}'"
                                                + "  AND CONSTRAINT_TYPE = 'FOREIGN KEY'", table.Database.Name, table.Name);

            string commandText2 = string.Format("SELECT t1.CONSTRAINT_NAME, t1.COLUMN_NAME, t1.POSITION_IN_UNIQUE_CONSTRAINT,"
                                                + "  t1.REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME"
                                                + " FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE t1"
                                                + "  INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS t2"
                                                + "  ON t2.TABLE_SCHEMA = t1.TABLE_SCHEMA"
                                                + "  AND t2.TABLE_NAME = t1.TABLE_NAME"
                                                + "  AND t2.CONSTRAINT_NAME = t1.CONSTRAINT_NAME"
                                                + " WHERE t1.TABLE_SCHEMA = '{0}' AND t1.TABLE_NAME = '{1}'"
                                                + "  AND t2.CONSTRAINT_TYPE = 'FOREIGN KEY'"
                                                + " ORDER BY t1.CONSTRAINT_NAME, t1.POSITION_IN_UNIQUE_CONSTRAINT", table.Database.Name, table.Name);

            DataSet dataSet;

            DbProviderFactory factory = CreateDbProviderFactory();
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.Connection = connection;

                    dataSet = ConvertDataReaderToDataSet(command.ExecuteReader());
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = commandText2;
                    command.Connection = connection;

                    dataSet.Tables.Add(ConvertDataReaderToDataTable(command.ExecuteReader()));
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            List<TableKeySchema> tableKeySchema = new List<TableKeySchema>();
            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                // Add constraint to keys relationship
                dataSet.Relations.Add("Contraint_to_Keys", dataSet.Tables[0].Columns["CONSTRAINT_NAME"], dataSet.Tables[1].Columns["CONSTRAINT_NAME"]);

                foreach (DataRow constraintRow in dataSet.Tables[0].Rows)
                {
                    string name = constraintRow["CONSTRAINT_NAME"].ToString();

                    // Get the keys
                    List<DataRow> keys = new List<DataRow>(constraintRow.GetChildRows("Contraint_to_Keys"));

                    List<string> primaryKeys = new List<string>(keys.Count);
                    List<string> foreignKeys = new List<string>(keys.Count);

                    string fkTable = table.Name;
                    string pkTable = keys[0]["REFERENCED_TABLE_NAME"].ToString();

                    foreach (DataRow key in keys)
                    {
                        foreignKeys.Add(key["COLUMN_NAME"].ToString());
                        primaryKeys.Add(key["REFERENCED_COLUMN_NAME"].ToString());
                    }

                    tableKeySchema.Add(new TableKeySchema(table.Database, name, foreignKeys.ToArray(), fkTable, primaryKeys.ToArray(), pkTable));
                }
            }

            if (tableKeySchema.Count > 0)
                return tableKeySchema;

            return new List<TableKeySchema>();
        }

        /// <summary>
        /// DRS 2006-01-24 : GetTableKeys must return both the foreign keys contained within the
        ///		given table AND the foreign keys that point at the given table from other tables.  I've
        ///		added this method to find those keys.  I tried as best I could to stick to the same
        ///		coding structure as was used in the original GetTableKeys (which can now be found in
        ///		GetMyTableKeys).
        /// </summary>
        private IEnumerable<TableKeySchema> GetOthersTableKeys(string connectionString, SchemaObjectBase table)
        {
            string commandText = string.Format("SELECT DISTINCT CONSTRAINT_NAME"
                                                + " FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE t1"
                                                + " WHERE t1.TABLE_SCHEMA = '{0}' AND t1.REFERENCED_TABLE_NAME = '{1}'", table.Database.Name, table.Name);
            string commandText2 = string.Format("SELECT t1.CONSTRAINT_NAME, t1.TABLE_NAME, t1.COLUMN_NAME, t1.POSITION_IN_UNIQUE_CONSTRAINT,"
                                                + "  t1.REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME"
                                                + " FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE t1"
                                                + "  INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS t2"
                                                + "  ON t2.TABLE_SCHEMA = t1.TABLE_SCHEMA"
                                                + "  AND t2.TABLE_NAME = t1.TABLE_NAME"
                                                + "  AND t2.CONSTRAINT_NAME = t1.CONSTRAINT_NAME"
                                                + " WHERE t1.TABLE_SCHEMA = '{0}' AND t1.REFERENCED_TABLE_NAME = '{1}'"
                                                + "  AND t2.CONSTRAINT_TYPE = 'FOREIGN KEY'"
                                                + " ORDER BY t1.CONSTRAINT_NAME, t1.POSITION_IN_UNIQUE_CONSTRAINT", table.Database.Name, table.Name);

            DataSet dataSet;

            DbProviderFactory factory = CreateDbProviderFactory();
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.Connection = connection;

                    dataSet = ConvertDataReaderToDataSet(command.ExecuteReader());
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = commandText2;
                    command.Connection = connection;

                    dataSet.Tables.Add(ConvertDataReaderToDataTable(command.ExecuteReader()));
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            List<TableKeySchema> tableKeySchema = new List<TableKeySchema>();
            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                // Add constraint to keys relationship
                dataSet.Relations.Add("Contraint_to_Keys", dataSet.Tables[0].Columns["CONSTRAINT_NAME"], dataSet.Tables[1].Columns["CONSTRAINT_NAME"]);

                foreach (DataRow constraintRow in dataSet.Tables[0].Rows)
                {
                    string name = constraintRow["CONSTRAINT_NAME"].ToString();

                    // Get the keys
                    List<DataRow> keys = new List<DataRow>(constraintRow.GetChildRows("Contraint_to_Keys"));

                    List<string> primaryKeys = new List<string>(keys.Count);
                    List<string> foreignKeys = new List<string>(keys.Count);

                    string fkTable = keys[0]["TABLE_NAME"].ToString();
                    string pkTable = keys[0]["REFERENCED_TABLE_NAME"].ToString();

                    foreach (DataRow key in keys)
                    {
                        foreignKeys.Add(key["COLUMN_NAME"].ToString());
                        primaryKeys.Add(key["REFERENCED_COLUMN_NAME"].ToString());
                    }

                    tableKeySchema.Add(new TableKeySchema(table.Database, name, foreignKeys.ToArray(), fkTable, primaryKeys.ToArray(), pkTable));
                }
            }

            if (tableKeySchema.Count > 0)
                return tableKeySchema;

            return new List<TableKeySchema>();
        }

        /// <summary>
        /// This is a helper method that fixed an issue where the DbDataAdapter.Fill(DataSet) would take 10 seconds per call to populate.
        /// </summary>
        /// <param name="reader">The DataReader.</param>
        /// <returns>A DataSet from a DataReader.</returns>
        private DataSet ConvertDataReaderToDataSet(IDataReader reader)
        {
            DataSet dataSet = new DataSet();
            dataSet.Tables.Add(ConvertDataReaderToDataTable(reader));

            return dataSet;
        }

        /// <summary>
        /// This is a helper method that fixed an issue where the DbDataAdapter.Fill(DataSet) would take 10 seconds per call to populate.
        /// </summary>
        /// <param name="reader">The DataReader.</param>
        /// <returns>A DataTable from a DataReader.</returns>
        private DataTable ConvertDataReaderToDataTable(IDataReader reader)
        {
            DataTable schemaTable = reader.GetSchemaTable();
            DataTable dataTable = new DataTable();

            foreach (DataRow row in schemaTable.Rows)
            {
                string columnName = (string)row["ColumnName"];
                DataColumn column = new DataColumn(columnName, (Type)row["DataType"]);
                dataTable.Columns.Add(column);
            }

            while (reader.Read())
            {
                DataRow dataRow = dataTable.NewRow();
                for (int index = 0; index <= reader.FieldCount - 1; index++)
                {
                    dataRow[index] = reader.GetValue(index);
                }

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        #endregion

        #region public IndexSchema[] GetTableIndexes(string connectionString, TableSchema table)

        /// <summary>
        /// Gats all of the indexes for a given table.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="table"></param>
        /// <returns></returns>
        public IndexSchema[] GetTableIndexes(string connectionString, TableSchema table)
        {
            string commandText = string.Format("SELECT INDEX_NAME, COUNT(*) AS COLUMN_COUNT, MAX(NON_UNIQUE) NON_UNIQUE,"
                                                + " CASE INDEX_NAME WHEN 'PRIMARY' THEN 1 ELSE 0 END IS_PRIMARY"
                                                + " FROM INFORMATION_SCHEMA.STATISTICS"
                                                + " WHERE  TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'"
                                                + " GROUP BY INDEX_NAME"
                                                + " ORDER BY INDEX_NAME;", table.Database.Name, table.Name);
            string commandText2 = string.Format("SELECT INDEX_NAME, COLUMN_NAME"
                                                + " FROM INFORMATION_SCHEMA.STATISTICS"
                                                + " WHERE  TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'"
                                                + " ORDER BY INDEX_NAME, SEQ_IN_INDEX;", table.Database.Name, table.Name);

            DataSet dataSet;

            DbProviderFactory factory = CreateDbProviderFactory();
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.Connection = connection;

                    dataSet = ConvertDataReaderToDataSet(command.ExecuteReader());
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = commandText2;
                    command.Connection = connection;

                    dataSet.Tables.Add(ConvertDataReaderToDataTable(command.ExecuteReader()));
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            List<IndexSchema> indexSchema = new List<IndexSchema>();
            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                // Add constraint to keys relationship
                dataSet.Relations.Add("INDEX_to_COLUMNS", dataSet.Tables[0].Columns["INDEX_NAME"], dataSet.Tables[1].Columns["INDEX_NAME"]);

                foreach (DataRow dataRow in dataSet.Tables[0].Rows)
                {
                    string name = dataRow["INDEX_NAME"].ToString();
                    bool isPrimaryKey = (((int)dataRow["IS_PRIMARY"]) == 1);
                    bool isUnique = ((long)dataRow["NON_UNIQUE"]) != 1;
                    bool isClustered = isPrimaryKey; // Primary Key indexes are automatically clustered in MySQL

                    // Get the columns
                    List<DataRow> cols = new List<DataRow>(dataRow.GetChildRows("INDEX_to_COLUMNS"));
                    List<string> memberColumns = new List<string>(cols.Count);

                    foreach (DataRow row in cols)
                    {
                        memberColumns.Add(row["COLUMN_NAME"].ToString());
                    }

                    indexSchema.Add(new IndexSchema(table, name, isPrimaryKey, isUnique, isClustered, memberColumns.ToArray()));
                }
            }

            if (indexSchema.Count > 0)
                return indexSchema.ToArray();

            return new List<IndexSchema>().ToArray();
        }

        #endregion

        #region public string GetCommandText(string connectionString, CommandSchema command)

        /// <summary>
        /// Gets the definition for a given command.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="commandSchema"></param>
        /// <returns></returns>
        public string GetCommandText(string connectionString, CommandSchema commandSchema)
        {
            StringBuilder sb = new StringBuilder();
            string commandText = string.Format("SELECT ROUTINE_DEFINITION FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA = '{0}' AND ROUTINE_NAME = '{1}'", commandSchema.Database.Name, commandSchema.Name);

            using (DbConnection connection = CreateConnection(connectionString))
            {
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.Connection = connection;

                using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        sb.Append(reader.GetString(0));
                    }

                    if (!reader.IsClosed)
                        reader.Close();
                }

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            return sb.ToString();
        }

        #endregion

        #region public ParameterSchema[] GetCommandParameters(string connectionString, CommandSchema command)

        /// <summary>
        /// Gets the parameters for a given command.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="command"></param>
        /// <returns></returns>
        public ParameterSchema[] GetCommandParameters(string connectionString, CommandSchema command)
        {
            // MySQL does not yet implement INFORMATION_SCHEMA.PARAMETERS
            // MySQL Connector/Net 1.0.7 is supposed to support DeriveParameters()
            // However, in my testing there appears to be a bug (throws a NULL reference exception)
            // This method will be unsupported until a subsequent release of DeriverParameters()
            // is working.

            throw new NotSupportedException("GetCommandParameters() is not supported in this release.");

            /*
            ArrayList a = new ArrayList();
            ParameterSchema ps;
            DbConnection cnx = null;
            DbCommand cmd = null;
            try
            {
                cnx = new DbConnection(connectionString);

                cmd = new DbCommand(command.Name, cnx);
                cmd.CommandType = CommandType.StoredProcedure;

                cnx.Open();
                IDbCommandBuilder.DeriveParameters(cmd);
                cnx.Close();

                foreach(MySqlParameter param in cmd.Parameters)
                {
                    ps = new ParameterSchema(command, param.ParameterName, param.Direction, param.DbType, 
                        param.MySqlDbType.ToString(), param.Size, param.Precision, param.Scale, param.IsNullable);

                    a.Add(ps);
                }

            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnx != null)
                    cnx.Close();
            }

            return (ParameterSchema[]) a.ToArray(typeof (ParameterSchema));
            */
        }

        /// <summary>
        /// Gets schema information about the results of a given command.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="command"></param>
        /// <returns></returns>
        public CommandResultSchema[] GetCommandResultSchemas(string connectionString, CommandSchema command)
        {
            // TODO:  Add MySQLSchemaProvider.GetCommandResultSchemas implementation
            throw new NotSupportedException("GetCommandResultSchemas() is not supported in this release.");
        }

        /// <summary>
        /// Gets the extended properties for a given schema object.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the target database.</param>
        /// <param name="schemaObject"></param>
        /// <returns></returns>
        public ExtendedProperty[] GetExtendedProperties(string connectionString, SchemaObjectBase schemaObject)
        {
            List<ExtendedProperty> extendedProperties = new List<ExtendedProperty>();

            if (schemaObject is ColumnSchema)
            {
                ColumnSchema columnSchema = schemaObject as ColumnSchema;

                string commandText = string.Format(@"SELECT EXTRA, COLUMN_DEFAULT, COLUMN_TYPE, COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = '{0}'
  AND TABLE_NAME = '{1}'
  AND COLUMN_NAME = '{2}'", columnSchema.Table.Database.Name, columnSchema.Table.Name, columnSchema.Name);

                using (DbConnection connection = CreateConnection(connectionString))
                {
                    connection.Open();

                    DbCommand command = connection.CreateCommand();
                    command.CommandText = commandText;
                    command.Connection = connection;

                    using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            string extra = reader.GetString(0).ToLower();
                            bool columndefaultisnull = reader.IsDBNull(1);
                            string columndefault = "";
                            if (!columndefaultisnull)
                            {
                                columndefault = reader.GetString(1).ToUpper();
                            }
                            string columntype = reader.GetString(2).ToUpper();
                            string columncomment = reader.GetString(3);

                            bool isIdentity = (extra.IndexOf("auto_increment") > -1);
                            extendedProperties.Add(new ExtendedProperty("CS_IsIdentity", isIdentity, columnSchema.DataType));

                            if (isIdentity)
                            {
                                /*
                                MySQL auto_increment doesn't work exactly like SQL Server's IDENTITY
                                I believe that auto_increment is equivalent to IDENTITY(1, 1)
                                However, auto_increment behaves differently from IDENTITY when used
                                with multi-column primary keys.  See the MySQL Reference Manual for details.
                                */
                                extendedProperties.Add(new ExtendedProperty("CS_IdentitySeed", 1, columnSchema.DataType));
                                extendedProperties.Add(new ExtendedProperty("CS_IdentityIncrement", 1, columnSchema.DataType));
                            }

                            extendedProperties.Add(new ExtendedProperty("CS_ColumnDefaultIsNull", columndefaultisnull, DbType.Boolean));
                            extendedProperties.Add(new ExtendedProperty("CS_ColumnDefault", columndefault, DbType.String));
                            extendedProperties.Add(new ExtendedProperty("CS_ColumnType", columntype, DbType.String));
                            extendedProperties.Add(new ExtendedProperty("CS_ColumnExtra", extra.ToUpper(), DbType.String));
                            extendedProperties.Add(new ExtendedProperty("CS_Description", columncomment, DbType.String));
                        }

                        if (!reader.IsClosed)
                            reader.Close();
                    }

                    if (connection.State != ConnectionState.Closed)
                        connection.Close();
                }
            }
            if (schemaObject is TableSchema)
            {
                TableSchema tableSchema = schemaObject as TableSchema;
                string commandText = string.Format(@"SHOW CREATE TABLE `{0}`.`{1}`", tableSchema.Database.Name, tableSchema.Name);

                using (DbConnection connection = CreateConnection(connectionString))
                {
                    connection.Open();

                    DbCommand command = connection.CreateCommand();
                    command.CommandText = commandText;
                    command.Connection = connection;

                    using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            string createtable = reader.GetString(1);
                            extendedProperties.Add(new ExtendedProperty("CS_CreateTableScript", createtable, DbType.String));
                        }

                        if (!reader.IsClosed)
                            reader.Close();
                    }

                    if (connection.State != ConnectionState.Closed)
                        connection.Close();
                }
            }

            return extendedProperties.ToArray();
        }

        public ExtendedProperty[] GetExtendedProperties_bak(string connectionString, SchemaObjectBase schemaObject)
        {
            List<ExtendedProperty> extendedProperties = new List<ExtendedProperty>();

            if (schemaObject is ColumnSchema)
            {
                ColumnSchema columnSchema = schemaObject as ColumnSchema;

                string commandText = string.Format(@"SELECT EXTRA, COLUMN_DEFAULT, COLUMN_TYPE
                                                      FROM INFORMATION_SCHEMA.COLUMNS
                                                      WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}'",
                                                      columnSchema.Table.Database.Name, columnSchema.Table.Name, columnSchema.Name);

                using (DbConnection connection = CreateConnection(connectionString))
                {
                    connection.Open();

                    DbCommand command = connection.CreateCommand();
                    command.CommandText = commandText;
                    command.Connection = connection;

                    using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            string extra = reader.GetString(0).ToLower();
                            bool columndefaultisnull = reader.IsDBNull(1);
                            string columndefault = "";
                            if (!columndefaultisnull)
                            {
                                columndefault = reader.GetString(1).ToUpper();
                            }
                            string columntype = reader.GetString(2).ToUpper();

                            bool isIdentity = (extra.IndexOf("auto_increment") > -1);
                            extendedProperties.Add(new ExtendedProperty(ExtendedPropertyNames.IsIdentity, isIdentity, columnSchema.DataType));

                            if (isIdentity)
                            {
                                /*
                                MySQL auto_increment doesn't work exactly like SQL Server's IDENTITY
                                I believe that auto_increment is equivalent to IDENTITY(1, 1)
                                However, auto_increment behaves differently from IDENTITY when used
                                with multi-column primary keys.  See the MySQL Reference Manual for details.
                                */
                                extendedProperties.Add(new ExtendedProperty(ExtendedPropertyNames.IdentitySeed, 1, columnSchema.DataType));
                                extendedProperties.Add(new ExtendedProperty(ExtendedPropertyNames.IdentityIncrement, 1, columnSchema.DataType));
                            }

                            extendedProperties.Add(new ExtendedProperty("CS_ColumnDefaultIsNull", columndefaultisnull, DbType.Boolean)); // Added for Backwards Compatibility.
                            extendedProperties.Add(new ExtendedProperty(ExtendedPropertyNames.DefaultValue, columndefault, DbType.String));
                            extendedProperties.Add(new ExtendedProperty("CS_ColumnDefault", columndefault, DbType.String)); // Added for Backwards Compatibility.
                            extendedProperties.Add(new ExtendedProperty(ExtendedPropertyNames.SystemType, columntype, DbType.String));
                            extendedProperties.Add(new ExtendedProperty("CS_ColumnType", columntype, DbType.String)); // Added for Backwards Compatibility.
                            extendedProperties.Add(new ExtendedProperty("CS_ColumnExtra", extra.ToUpper(), DbType.String));
                        }

                        if (!reader.IsClosed)
                            reader.Close();
                    }

                    if (connection.State != ConnectionState.Closed)
                        connection.Close();
                }
            }
            if (schemaObject is TableSchema)
            {
                TableSchema tableSchema = schemaObject as TableSchema;
                string commandText = string.Format(@"SHOW CREATE TABLE `{0}`.`{1}`", tableSchema.Database.Name, tableSchema.Name);

                using (DbConnection connection = CreateConnection(connectionString))
                {
                    connection.Open();

                    DbCommand command = connection.CreateCommand();
                    command.CommandText = commandText;
                    command.Connection = connection;

                    using (IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            string createtable = reader.GetString(1);
                            extendedProperties.Add(new ExtendedProperty("CS_CreateTableScript", createtable, DbType.String));
                        }

                        if (!reader.IsClosed)
                            reader.Close();
                    }

                    if (connection.State != ConnectionState.Closed)
                        connection.Close();
                }
            }

            return extendedProperties.ToArray();
        }

        public void SetExtendedProperties(string connectionString, SchemaObjectBase schemaObject)
        {
            throw new NotImplementedException("This method has not been implemented");
        }

        #endregion

        #region DbProvider Helpers

        private static DbProviderFactory CreateDbProviderFactory()
        {
            return DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
        }

        private static DbConnection CreateConnection(string connectionString)
        {
            DbProviderFactory factory = CreateDbProviderFactory();
            DbConnection connection = factory.CreateConnection();
            connection.ConnectionString = connectionString;

            return connection;
        }

        #endregion

        #region private DbType GetDbType(string type, bool isUnsigned)

        /// <summary>
        /// Gets the data type of the given native type.
        /// </summary>
        /// <param name="type">The native data type.</param>
        /// <param name="isUnsigned">if set to <c>true</c> [is unsigned].</param>
        /// <returns></returns>
        private static DbType GetDbType(string type, bool isUnsigned)
        {
            //http://dev.mysql.com/doc/refman/5.1/en/data-types.html
            string t = type.ToLower();
            switch (t)
            {
                case "bit":
                    return DbType.UInt16;
                case "tinyint":
                    return (isUnsigned) ? DbType.Byte : DbType.SByte;
                case "smallint":
                    return (isUnsigned) ? DbType.UInt16 : DbType.Int16;

                //A year in two-digit or four-digit format.
                case "year":
                case "mediumint":
                case "int":
                    return (isUnsigned) ? DbType.UInt32 : DbType.Int32;
                case "bigint":
                    return (isUnsigned) ? DbType.UInt64 : DbType.Int64;
                case "float":
                    return DbType.Single;
                case "double":
                    return DbType.Double;
                case "decimal":
                    return DbType.Decimal;
                case "date":
                    return DbType.Date;
                case "datetime":
                    return DbType.DateTime;
                case "timestamp":
                    return DbType.DateTime;
                case "time":
                    return DbType.Time;

                case "enum":
                case "set":
                case "tinytext":
                case "mediumtext":
                case "longtext":
                case "text":
                case "char":
                case "varchar":
                    return DbType.String;

                case "binary":
                case "varbinary":
                case "tinyblob":
                case "blob":
                case "longblob":
                    return DbType.Binary;

                default:
                    return DbType.Object;
            }
        }

        #endregion
    }
}