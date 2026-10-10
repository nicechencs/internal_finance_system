using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Person;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class PersonDeliveryServiceTests : TestBase
{
    private readonly Mock<IRepository<PersonDelivery>> _deliveryRepositoryMock;
    private readonly Mock<IRepository<Person>> _personRepositoryMock;
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<ILogger<PersonDeliveryService>> _loggerMock;
    private readonly List<PersonDelivery> _deliveries = new();
    private readonly PersonDeliveryService _service;

    public PersonDeliveryServiceTests()
    {
        _deliveryRepositoryMock = new Mock<IRepository<PersonDelivery>>();
        _personRepositoryMock = new Mock<IRepository<Person>>();
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _loggerMock = new Mock<ILogger<PersonDeliveryService>>();

        _personRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Person> { new() { Id = 1, Name = "张三", IsDeleted = false } }.AsQueryable().BuildMock().Object);
        _projectRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Project> { new() { Id = 7, Name = "项目A", IsDeleted = false } }.AsQueryable().BuildMock().Object);

        _deliveryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PersonDelivery>()))
            .ReturnsAsync((PersonDelivery delivery) =>
            {
                delivery.Id = _deliveries.Count + 1;
                delivery.Person = new Person { Id = delivery.PersonId, Name = "张三" };
                if (delivery.ProjectId == 7)
                {
                    delivery.Project = new Project { Id = 7, Name = "项目A" };
                }
                _deliveries.Add(delivery);
                return delivery;
            });
        _deliveryRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => _deliveries.AsQueryable().BuildMock().Object);
        _deliveryRepositoryMock.Setup(r => r.Update(It.IsAny<PersonDelivery>()));
        _deliveryRepositoryMock.Setup(r => r.Delete(It.IsAny<PersonDelivery>()));

        _service = new PersonDeliveryService(
            _deliveryRepositoryMock.Object,
            _personRepositoryMock.Object,
            _projectRepositoryMock.Object,
            AuditLogServiceMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ThrowsValidationException()
    {
        var act = () => _service.CreateAsync(1, new CreatePersonDeliveryRequest { Title = "  " });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("交付物名称不能为空");
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ThrowsValidationException()
    {
        var act = () => _service.CreateAsync(1, new CreatePersonDeliveryRequest
        {
            Title = "月报",
            Status = "Done"
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("无效的验收状态");
    }

    [Fact]
    public async Task CreateAsync_MissingPerson_ThrowsNotFound()
    {
        var act = () => _service.CreateAsync(99, new CreatePersonDeliveryRequest { Title = "月报" });

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("人员不存在");
    }

    [Fact]
    public async Task CreateAsync_MissingProject_ThrowsValidationException()
    {
        var act = () => _service.CreateAsync(1, new CreatePersonDeliveryRequest
        {
            Title = "月报",
            ProjectId = 404
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("关联项目不存在");
    }

    [Fact]
    public async Task CreateAsync_AcceptedWithoutActor_StampsCurrentUser()
    {
        var result = await _service.CreateAsync(1, new CreatePersonDeliveryRequest
        {
            Title = "  阶段交付  ",
            Status = "accepted",
            ProjectId = 7,
            Description = " 说明 "
        });

        result.Title.Should().Be("阶段交付");
        result.Description.Should().Be("说明");
        result.Status.Should().Be(nameof(PersonDeliveryStatus.Accepted));
        result.ProjectName.Should().Be("项目A");
        result.AcceptedBy.Should().Be("admin");
        result.AcceptedAt.Should().NotBeNull();
        AuditLogServiceMock.Verify(a => a.LogAsync(
            "Create", "PersonDelivery", result.Id, null, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_LeavingAccepted_ClearsAcceptance()
    {
        var existing = new PersonDelivery
        {
            Id = 5,
            PersonId = 1,
            Person = new Person { Id = 1, Name = "张三" },
            Title = "阶段交付",
            Status = PersonDeliveryStatus.Accepted,
            AcceptedAt = DateTime.UtcNow.AddDays(-1),
            AcceptedBy = "admin"
        };
        _deliveries.Add(existing);

        var result = await _service.UpdateAsync(1, 5, new UpdatePersonDeliveryRequest
        {
            Title = "阶段交付",
            Status = "Rejected",
            Notes = "需返工"
        });

        result.Status.Should().Be(nameof(PersonDeliveryStatus.Rejected));
        result.AcceptedAt.Should().BeNull();
        result.AcceptedBy.Should().BeNull();
        result.Notes.Should().Be("需返工");
        existing.Status.Should().Be(PersonDeliveryStatus.Rejected);
    }

    [Fact]
    public async Task UpdateAsync_WrongPerson_ThrowsNotFound()
    {
        _deliveries.Add(new PersonDelivery
        {
            Id = 5,
            PersonId = 2,
            Title = "别人的交付",
            Status = PersonDeliveryStatus.Pending
        });

        var act = () => _service.UpdateAsync(1, 5, new UpdatePersonDeliveryRequest
        {
            Title = "别人的交付",
            Status = "Pending"
        });

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("交付记录不存在");
    }

    [Fact]
    public async Task GetByPersonAsync_ReturnsOnlyThatPersonsDeliveries()
    {
        _deliveries.Add(new PersonDelivery
        {
            Id = 1,
            PersonId = 1,
            Title = "可见",
            Status = PersonDeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        _deliveries.Add(new PersonDelivery
        {
            Id = 2,
            PersonId = 2,
            Title = "不可见",
            Status = PersonDeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        var result = await _service.GetByPersonAsync(1);

        result.Should().ContainSingle(d => d.Title == "可见");
    }
}
