using Npgsql;
using System.Text.Json;

namespace YourOwnData
{
    public static class DataService
    {
        public static List<List<string>> GetDataTable(string sqlQuery)
        {
            var rows = new List<List<string>>();
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=localhost;Port=5432;Database=spyne;Username=postgres;Password=postgres"))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        int count = 0;
                        bool headersAdded = false;
                        while (reader.Read())
                        {
                            count += 1;
                            var cols = new List<string>();
                            var headerCols = new List<string>();
                            if (!headersAdded)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    headerCols.Add(reader.GetName(i).ToString());
                                }
                                headersAdded = true;
                                rows.Add(headerCols);
                            }

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                try
                                {
                                    cols.Add(reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString());
                                }
                                catch
                                {
                                    cols.Add("DataTypeConversionError");
                                }
                            }
                            rows.Add(cols);
                        }
                    }
                }
            }

            return rows;
        }
    }

    public class TableSchema
    {
        public string TableName { get; set; }
        public List<string> Columns { get; set; }
    }
}
