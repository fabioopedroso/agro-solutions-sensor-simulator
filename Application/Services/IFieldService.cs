using Application.DTOs;

namespace Application.Services;

public interface IFieldService
{
    Task<IEnumerable<FieldInfoDto>> GetActiveFieldsAsync(CancellationToken cancellationToken = default);
    Task RefreshFieldsCacheAsync(CancellationToken cancellationToken = default);
}
