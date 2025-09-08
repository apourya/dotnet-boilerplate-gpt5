using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;

namespace EnterpriseBoilerplate.Application.Common.Behaviors
{
    public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IUnitOfWork _uow;
        public UnitOfWorkBehavior(IUnitOfWork uow) { _uow = uow; }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            var response = await next();
            if (request is ITransactionalRequest)
            {
                await _uow.SaveChangesAsync(ct);
            }
            return response;
        }
    }
}
