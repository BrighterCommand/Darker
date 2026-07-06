namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class QueryWithNameProperty : IQuery<QueryWithNameProperty.Result>
    {
        public string Name { get; }

        public QueryWithNameProperty(string name)
        {
            Name = name;
        }

        internal class Result { }
    }
}
