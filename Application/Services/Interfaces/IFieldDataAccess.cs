using Application.DTOs;

namespace Application.Services.Interfaces;

public interface IFieldDataAccess
{
    Task<IEnumerable<FieldInfoDto>> GetActiveFieldsAsync(CancellationToken cancellationToken = default);
}
