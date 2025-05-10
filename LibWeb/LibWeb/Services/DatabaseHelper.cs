using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

public class DatabaseHelper
{
    private readonly string _connectionString;

    public DatabaseHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Dictionary<string, List<Dictionary<string, object>>>> GetAllTableDataAsync()
    {
        var allTableData = new Dictionary<string, List<Dictionary<string, object>>>();
        List<string> tableNames = await GetTableNamesAsync();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            foreach (var tableName in tableNames)
            {
                string query = $"SELECT * FROM {tableName}";
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    var tableRows = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        tableRows.Add(row);
                    }
                    allTableData[tableName] = tableRows;
                }
            }
        }

        return allTableData;
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        List<string> tableNames = new List<string>();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';";
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }
        }
        return tableNames;
    }
}