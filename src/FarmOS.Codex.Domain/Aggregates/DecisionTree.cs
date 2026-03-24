using FarmOS.Codex.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain.Aggregates;

public sealed class DecisionTree : AggregateRoot<DecisionTreeId>
{
    public string Title { get; private set; } = string.Empty;
    public ProcedureCategory Category { get; private set; }
    public string? Description { get; private set; }
    private readonly Dictionary<string, DecisionNode> _nodes = [];
    public IReadOnlyDictionary<string, DecisionNode> Nodes => _nodes;

    public static DecisionTree Create(string title, ProcedureCategory category, string? description)
    {
        var tree = new DecisionTree();
        tree.RaiseEvent(new DecisionTreeCreated(DecisionTreeId.New(), title, category, description, DateTimeOffset.UtcNow));
        return tree;
    }

    public void AddNode(DecisionNode node) =>
        RaiseEvent(new DecisionNodeAdded(Id, node, DateTimeOffset.UtcNow));

    public Result<DecisionTreeId, DomainError> UpdateNode(DecisionNode node)
    {
        if (!_nodes.ContainsKey(node.NodeId))
            return DomainError.NotFound("DecisionNode", node.NodeId);
        RaiseEvent(new DecisionNodeUpdated(Id, node, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case DecisionTreeCreated e: Id = e.Id; Title = e.Title; Category = e.Category; Description = e.Description; break;
            case DecisionNodeAdded e: _nodes[e.Node.NodeId] = e.Node; break;
            case DecisionNodeUpdated e: _nodes[e.Node.NodeId] = e.Node; break;
        }
    }
}
