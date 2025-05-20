namespace Fast_Down.Core
{
    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default
        );
    }
}
