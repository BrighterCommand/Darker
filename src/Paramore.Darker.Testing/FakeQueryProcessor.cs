using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Testing
{
    public class FakeQueryProcessor : IQueryProcessor
    {
        private readonly IList<IQuery> _executedQueries;
        private readonly IDictionary<Type, IDictionary<Predicate<IQuery>, Func<IQuery, object>>> _results;
        private readonly IDictionary<Type, IDictionary<Predicate<IQuery>, Exception>> _exceptions;

        public FakeQueryProcessor()
        {
            _executedQueries = new List<IQuery>();
            _results = new Dictionary<Type, IDictionary<Predicate<IQuery>, Func<IQuery, object>>>();
            _exceptions = new Dictionary<Type, IDictionary<Predicate<IQuery>, Exception>>();
        }

        public IEnumerable<IQuery> GetExecutedQueries() => _executedQueries;

        public IEnumerable<T> GetExecutedQueries<T>() => _executedQueries.Where(q => q is T).Cast<T>();

        public TResponse Execute<TResponse>(IQuery<TResponse> query)
        {
            _executedQueries.Add(query);

            var queryType = query.GetType();
            if (_exceptions.ContainsKey(queryType))
            {
                var exception = _exceptions[queryType].Where(r => r.Key(query)).Select(r => r.Value).FirstOrDefault();
                if (exception != null)
                    throw exception;
            }

            if (!_results.ContainsKey(queryType))
                return default(TResponse);

            var result = _results[queryType].Where(r => r.Key(query)).Select(r => r.Value).FirstOrDefault();
            if (result == null)
                return default(TResponse);

            return (TResponse)result(query);
        }

        public Task<TResponse> ExecuteAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(Execute(query));
        }

        public void SetupResultFor<TQuery>(Predicate<TQuery> predicate, object result)
        {
            var queryType = typeof(TQuery);
            if (!_results.ContainsKey(queryType))
                _results.Add(queryType, new Dictionary<Predicate<IQuery>, Func<IQuery, object>>());

            Predicate<IQuery> untypedPredicate = r => predicate((TQuery)r);
            _results[queryType].Add(untypedPredicate, r => result);
        }

        public void SetupResultFor<TQuery>(Predicate<TQuery> predicate, Func<TQuery, object> result)
            where TQuery : IQuery
        {
            var queryType = typeof(TQuery);
            if (!_results.ContainsKey(queryType))
                _results.Add(queryType, new Dictionary<Predicate<IQuery>, Func<IQuery, object>>());

            Predicate<IQuery> untypedPredicate = r => predicate((TQuery)r);
            _results[queryType].Add(untypedPredicate, r => result((TQuery)r));
        }

        public void SetupResultFor<TQuery>(object result)
            where TQuery : IQuery
        {
            var queryType = typeof(TQuery);
            if (!_results.ContainsKey(queryType))
                _results.Add(queryType, new Dictionary<Predicate<IQuery>, Func<IQuery, object>>());

            _results[queryType].Add(_ => true, r => result);
        }

        public void SetupResultFor<TQuery>(Func<TQuery, object> result)
            where TQuery : IQuery
        {
            var queryType = typeof(TQuery);
            if (!_results.ContainsKey(queryType))
                _results.Add(queryType, new Dictionary<Predicate<IQuery>, Func<IQuery, object>>());

            _results[queryType].Add(_ => true, r => result((TQuery)r));
        }

        public void SetupExceptionFor<TQuery>(Predicate<TQuery> predicate, Exception exception)
            where TQuery : IQuery
        {
            var queryType = typeof(TQuery);
            if (!_exceptions.ContainsKey(queryType))
                _exceptions.Add(queryType, new Dictionary<Predicate<IQuery>, Exception>());

            Predicate<IQuery> untypedPredicate = r => predicate((TQuery)r);
            _exceptions[queryType].Add(untypedPredicate, exception);
        }

        public void SetupExceptionFor<TQuery>(Exception exception)
            where TQuery : IQuery
        {
            var queryType = typeof(TQuery);
            if (!_exceptions.ContainsKey(queryType))
                _exceptions.Add(queryType, new Dictionary<Predicate<IQuery>, Exception>());

            _exceptions[queryType].Add(_ => true, exception);
        }
    }
}
