using Application.DTOs;
using Application.Services;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class FieldDataAccess : IFieldDataAccess
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FieldDataAccess> _logger;
    private const int ActiveStatus = 1; // FieldStatus.Active = 1

    public FieldDataAccess(ApplicationDbContext context, ILogger<FieldDataAccess> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<FieldInfoDto>> GetActiveFieldsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var fields = await _context.Fields
                .Where(f => f.Status == ActiveStatus)
                .Select(f => new FieldInfoDto
                {
                    Id = f.Id,
                    Latitude = f.Latitude,
                    Longitude = f.Longitude
                })
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Buscados {Count} talhões ativos do banco de dados", fields.Count);
            return fields;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar talhões ativos do banco de dados");
            throw;
        }
    }
}
