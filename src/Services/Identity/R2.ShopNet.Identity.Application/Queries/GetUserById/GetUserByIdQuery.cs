using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.DTOs;

namespace R2.ShopNet.Identity.Application.Queries.GetUserById;

/// <summary>
/// Query to get a user by ID.
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IQuery<Result<UserDto>>;
