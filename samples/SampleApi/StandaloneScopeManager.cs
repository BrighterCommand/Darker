using System.Threading;
using LightInject;

namespace SampleApi
{
    internal class StandaloneScopeManager : IScopeManager
    {
        private readonly ThreadLocal<Scope> _currentScope = new ThreadLocal<Scope>();

        public StandaloneScopeManager(IServiceFactory serviceFactory)
        {
            ServiceFactory = serviceFactory;
        }

        public Scope BeginScope()
        {
            var scope = new Scope(this, null);
            _currentScope.Value = scope;
            return scope;
        }

        public void EndScope(Scope scope)
        {
            _currentScope.Value = null;
        }

        public Scope CurrentScope
        {
            get { return _currentScope.Value; }
            set { _currentScope.Value = value; }
        }

        public IServiceFactory ServiceFactory { get; }
    }

    internal class StandaloneScopeManagerProvider : ScopeManagerProvider
    {
        protected override IScopeManager CreateScopeManager(IServiceFactory serviceFactory)
        {
            return new StandaloneScopeManager(serviceFactory);
        }
    }
}