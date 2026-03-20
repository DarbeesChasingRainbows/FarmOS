using MediatR;

namespace FarmOS.SharedKernel.CQRS;

/// <summary>
/// Marker for commands. Commands mutate state and return Result&lt;TResponse, DomainError&gt;.
/// Per CQRS rules: commands NEVER return domain data, only success/failure or an ID.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse, DomainError>>;

/// <summary>
/// Handler for commands.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse, DomainError>>
    where TCommand : ICommand<TResponse>;

/// <summary>
/// Marker for queries. Queries read flattened projection models, never the write-side aggregates.
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse?>;

/// <summary>
/// Handler for queries.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse?>
    where TQuery : IQuery<TResponse>;
