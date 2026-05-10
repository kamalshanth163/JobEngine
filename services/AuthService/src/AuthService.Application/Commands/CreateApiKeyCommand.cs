using MediatR;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;

namespace AuthService.Application.Commands;

public sealed record CreateApiKeyCommand(Guid TenantId, string? Name) : IRequest<CreateApiKeyResult>;

public sealed record CreateApiKeyResult(Guid Id, string KeyPrefix, string RawKey, string? Name);

public sealed class CreateApiKeyHandler(
    IApiKeyRepository _keys,
    IUnitOfWork _uow)
    : IRequestHandler<CreateApiKeyCommand, CreateApiKeyResult>
{
    public async Task<CreateApiKeyResult> Handle(CreateApiKeyCommand cmd, CancellationToken ct)
    {
        var (entity, raw) = ApiKey.Create(cmd.TenantId, cmd.Name);
        await _keys.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return new CreateApiKeyResult(entity.Id, entity.KeyPrefix, raw, entity.Name);
    }
}
