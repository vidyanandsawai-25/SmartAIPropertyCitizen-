using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using SmartAIPropertyCitizen.Api.Core.Interfaces;

namespace SmartAIPropertyCitizen.Api.Infrastructure.Data.Repositories
{
    public class SqlRepository : ISqlRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlRepository> _logger;

        public SqlRepository(IConfiguration configuration, ILogger<SqlRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("DefaultConnection string is missing.");
            _logger = logger;
        }

        public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(string spName, object parameters)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                return await connection.QueryAsync<T>(spName, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing stored procedure {SpName}", spName);
                throw;
            }
        }

        public async Task<int> ExecuteAsync(string sql, object parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<T>(sql, parameters);
        }
    }
}
