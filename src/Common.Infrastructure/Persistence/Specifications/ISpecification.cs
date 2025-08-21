using System.Linq.Expressions;

namespace Common.Infrastructure.Persistence.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Filter { get; }
}

public interface ISpecification2<T>
{
    IQueryable<T> Query(IReadOnlyList<T> values);
}