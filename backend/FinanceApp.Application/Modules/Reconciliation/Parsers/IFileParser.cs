namespace FinanceApp.Application.Modules.Reconciliation.Parsers;

public interface IFileParser
{
    List<ParsedBankRow> Parse(Stream stream);
}
