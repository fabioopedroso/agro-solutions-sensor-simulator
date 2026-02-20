using Application.DTOs;

namespace Application.Services;

public interface IFieldDataAccess
{
    Task<IEnumerable<FieldInfoDto>> GetActiveFieldsAsync(CancellationToken cancellationToken = default);
}
