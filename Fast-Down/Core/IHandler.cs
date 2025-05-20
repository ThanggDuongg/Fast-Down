namespace Fast_Down.Core
{
    public interface IHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> HandleAsync(
            TRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
