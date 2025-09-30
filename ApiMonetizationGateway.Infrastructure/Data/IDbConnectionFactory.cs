using Microsoft.Data.SqlClient;
using System.Data;

namespace ApiMonetizationGateway.Infrastructure.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
    
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        
        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }
        
        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}