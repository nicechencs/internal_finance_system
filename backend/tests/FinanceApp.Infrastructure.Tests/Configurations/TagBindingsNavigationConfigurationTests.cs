using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Tests.Helpers;

namespace FinanceApp.Infrastructure.Tests.Configurations;

public class TagBindingsNavigationConfigurationTests : IDisposable
{
    private static readonly Type[] TaggedOwnerTypes =
    [
        typeof(Customer),
        typeof(Supplier),
        typeof(Person),
        typeof(Project),
        typeof(Receivable),
        typeof(Payable),
        typeof(Transaction)
    ];

    private readonly AppDbContext _context;

    public TagBindingsNavigationConfigurationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
    }

    [Theory]
    [MemberData(nameof(GetTaggedOwnerTypes))]
    public void TaggedOwner_ShouldIgnoreTagBindingsNavigation(Type ownerType)
    {
        var entityType = _context.Model.FindEntityType(ownerType);

        entityType.Should().NotBeNull();
        entityType!.FindNavigation(nameof(Customer.TagBindings)).Should().BeNull();
    }

    public static IEnumerable<object[]> GetTaggedOwnerTypes()
    {
        return TaggedOwnerTypes.Select(type => new object[] { type });
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
