namespace FastDown.Core
{
    public class Mediator(IServiceProvider serviceProvider) : IMediator
    {
        public async Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default
        )
        {
            var handlerType = typeof(IHandler<,>).MakeGenericType(
                request.GetType(),
                typeof(TResponse)
            );

            var handler =
                serviceProvider.GetService(handlerType)
                ?? throw new InvalidOperationException(
                    $"Handler not found for {request.GetType().Name}"
                );

            var method =
                handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException("No HandleAsync method");

            return await (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
        }
    }
}
