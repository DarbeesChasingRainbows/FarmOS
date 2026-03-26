using FarmOS.Codex.Domain.Aggregates;

namespace FarmOS.Codex.Application;

public interface ICodexEventStore
{
    Task<Procedure> LoadProcedureAsync(string procedureId, CancellationToken ct);
    Task SaveProcedureAsync(Procedure procedure, string userId, CancellationToken ct);

    Task<Playbook> LoadPlaybookAsync(string playbookId, CancellationToken ct);
    Task SavePlaybookAsync(Playbook playbook, string userId, CancellationToken ct);

    Task<DecisionTree> LoadDecisionTreeAsync(string decisionTreeId, CancellationToken ct);
    Task SaveDecisionTreeAsync(DecisionTree decisionTree, string userId, CancellationToken ct);
}
