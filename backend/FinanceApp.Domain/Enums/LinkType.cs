namespace FinanceApp.Domain.Enums;

public enum LinkType
{
    Customer = 1,
    Supplier = 2,
    Person = 3,
    Project = 4,
    Account = 5
}

public enum RuleRerunStrategy
{
    Conservative = 1,
    Overwrite = 2
}
