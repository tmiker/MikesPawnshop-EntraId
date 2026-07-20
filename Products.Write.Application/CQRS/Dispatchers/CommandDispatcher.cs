using Microsoft.Extensions.DependencyInjection;
using Products.Write.Application.Abstractions;

namespace Products.Write.Application.CQRS.Dispatchers
{
    // Initial Dev Only - replaced with Mediatr
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public CommandDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResult> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            
            ICommandHandler<TCommand, TResult>? handler = _serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();

            if (handler == null)
            {
                throw new InvalidOperationException($"No handler registered for command type {typeof(TCommand).FullName}");
            }

            return await handler.HandleAsync(command, cancellationToken);
        }
    }
}
