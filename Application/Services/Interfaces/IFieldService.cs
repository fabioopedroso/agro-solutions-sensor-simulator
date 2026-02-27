using Application.DTOs;

namespace Application.Services.Interfaces;

public interface IFieldService
{
    Task<IEnumerable<FieldInfoDto>> GetActiveFieldsAsync(CancellationToken cancellationToken = default);
    Task RefreshFieldsCacheAsync(CancellationToken cancellationToken = default);
}
