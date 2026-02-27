using Application.DTOs;
using Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class FieldService : IFieldService
{
    private readonly IFieldDataAccess _fieldDataAccess;
    private readonly ILogger<FieldService> _logger;
    private List<FieldInfoDto>? _cachedFields;
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public FieldService(IFieldDataAccess fieldDataAccess, ILogger<FieldService> logger)
    {
        _fieldDataAccess = fieldDataAccess;
        _logger = logger;
    }

    public async Task<IEnumerable<FieldInfoDto>> GetActiveFieldsAsync(CancellationToken cancellationToken = default)
    {
        // Verifica se o cache ainda é válido
        if (_cachedFields != null && DateTime.UtcNow - _lastCacheUpdate < _cacheExpiration)
        {
            _logger.LogDebug("Retornando {Count} talhões do cache", _cachedFields.Count);
            return _cachedFields;
        }

        // Atualiza o cache
        await RefreshFieldsCacheAsync(cancellationToken);
        return _cachedFields ?? Enumerable.Empty<FieldInfoDto>();
    }

    public async Task RefreshFieldsCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var fields = (await _fieldDataAccess.GetActiveFieldsAsync(cancellationToken)).ToList();

            var previousCount = _cachedFields?.Count ?? 0;
            var previousFieldIds = _cachedFields?.Select(f => f.Id).ToHashSet() ?? new HashSet<int>();
            
            _cachedFields = fields;
            _lastCacheUpdate = DateTime.UtcNow;

            _logger.LogInformation(
                "Cache de talhões atualizado. Total de talhões ativos: {Count} (anterior: {PreviousCount})",
                fields.Count, previousCount);

            // Detecta novos talhões
            if (previousCount > 0 && fields.Count > previousCount)
            {
                var newFields = fields.Where(f => !previousFieldIds.Contains(f.Id)).ToList();
                if (newFields.Any())
                {
                    _logger.LogInformation(
                        "Novos talhões detectados: {NewFieldsCount}. IDs: {Ids}",
                        newFields.Count,
                        string.Join(", ", newFields.Select(f => f.Id)));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar cache de talhões");
            throw;
        }
    }
}
