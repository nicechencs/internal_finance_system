using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;

namespace FinanceApp.Application.Tests.Helpers;

public static class MockExtensions
{
    public static Mock<IQueryable<T>> BuildMock<T>(this IQueryable<T> data) where T : class
    {
        var mock = new Mock<IQueryable<T>>();
        var asyncEnumerable = new TestAsyncEnumerable<T>(data);

        mock.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mock.As<IEnumerable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mock.Setup(m => m.Provider).Returns(((IQueryable<T>)asyncEnumerable).Provider);
        mock.Setup(m => m.Expression).Returns(data.Expression);
        mock.Setup(m => m.ElementType).Returns(data.ElementType);
        mock.Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mock;
    }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult);

        // Handle Task<T> results from EF Core async methods (e.g., CountAsync returns Task<int>)
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = resultType.GetGenericArguments()[0];
            var executeMethod = typeof(IQueryProvider).GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })!;
            var result = executeMethod.MakeGenericMethod(innerType).Invoke(_inner, new object[] { expression });

            // Use reflection to create Task.FromResult<InnerType>(result) with correct generic type
            var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(innerType);
            return (TResult)fromResultMethod.Invoke(null, new[] { result })!;
        }

        return _inner.Execute<TResult>(expression);
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public T Current => _inner.Current;
}
