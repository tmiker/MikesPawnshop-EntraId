using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data.Common;

namespace Products.Read.API.Health
{
    public class SqlServerHealthCheck : IHealthCheck
    {
        private const string DefaultTestQuery = "Select 1";

        private string _connectionString { get; }

        public string TestQuery { get; }

        public SqlServerHealthCheck(string connectionString) : this(connectionString, testQuery: DefaultTestQuery)
        {
        }

        public SqlServerHealthCheck(string connectionString, string testQuery)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            TestQuery = testQuery;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync(cancellationToken);

                    if (TestQuery != null)
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = TestQuery;

                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch (DbException ex)
                {
                    return new HealthCheckResult(status: context.Registration.FailureStatus, exception: ex);
                }
            }

            return HealthCheckResult.Healthy();
        }
    }
}
