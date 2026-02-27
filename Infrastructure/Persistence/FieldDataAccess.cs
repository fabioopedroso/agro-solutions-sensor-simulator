using Application.DTOs;
using Application.Services.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Persistence;

public class FieldDataAccess : IFieldDataAccess
{
    private readonly string _connectionString;
    private readonly ILogger<FieldDataAccess> _logger;
    private const int ActiveStatus = 1;

    public FieldDataAccess(string connectionString, ILogger<FieldDataAccess> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IEnumerable<FieldInfoDto>> GetActiveFieldsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Latitude", "Longitude"
            FROM "Field"
            WHERE "Status" = @Status
            """;

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var fields = await connection.QueryAsync<FieldInfoDto>(
                new CommandDefinition(sql, new { Status = ActiveStatus }, cancellationToken: cancellationToken));

            var result = fields.ToList();
            _logger.LogDebug("Buscados {Count} talhões ativos do banco de dados", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar talhões ativos do banco de dados");
            throw;
        }
    }
}
