using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Extracting schema....");

List<KeyValuePair<string, string>> rows = new();
List<TableSchema> dbSchema = new();

using (NpgsqlConnection connection = new NpgsqlConnection("Host=localhost;Port=5432;Database=spyne;Username=postgres;Password=postgres"))
{
    connection.Open();

    // Get the schema
    string sql = @"
        SELECT 
            (CASE WHEN table_schema = 'public' THEN '' ELSE table_schema || '.' END) || table_name AS ""TableName"",
            column_name AS ""ColumnName""
        FROM 
            information_schema.columns
        WHERE 
            table_schema NOT IN ('pg_catalog', 'information_schema')
        ORDER BY 
            table_schema, table_name, ordinal_position;";

    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
    {
        using (NpgsqlDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                rows.Add(new KeyValuePair<string, string>(reader["TableName"].ToString(), reader["ColumnName"].ToString()));
            }
        }
    }
}

var groups = rows.GroupBy(x => x.Key);

foreach (var group in groups)
{
    dbSchema.Add(new TableSchema() { TableName = group.Key, Columns = group.Select(x => x.Value).ToList() });
}

Console.WriteLine("Copy the schema below into the Index.cshtml.cs file of the YourOwnData project:");
Console.WriteLine();

var textLines = new List<string>();

foreach (var table in dbSchema)
{
    var schemaLine = $"- {table.TableName} (";

    foreach (var column in table.Columns)
    {
        schemaLine += column + ", ";
    }

    schemaLine = schemaLine.Replace(", )", ")");

    Console.WriteLine(schemaLine);
    textLines.Add(schemaLine);
}

File.WriteAllText(@"Schema.txt", JsonSerializer.Serialize(dbSchema));

public class TableSchema
{
    public string TableName { get; set; }
    public List<string> Columns { get; set; }
}