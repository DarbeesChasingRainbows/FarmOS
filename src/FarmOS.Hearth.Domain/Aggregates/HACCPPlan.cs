using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Hearth.Domain.Aggregates;

public sealed class HACCPPlan : AggregateRoot<HACCPPlanId>
{
    public string PlanName { get; private set; } = "";
    public string FacilityName { get; private set; } = "";
    private readonly List<CCPDefinition> _ccpDefinitions = [];
    public IReadOnlyList<CCPDefinition> CCPDefinitions => _ccpDefinitions;

    public static HACCPPlan Create(string planName, string facilityName)
    {
        var plan = new HACCPPlan();
        plan.RaiseEvent(new HACCPPlanCreated(HACCPPlanId.New(), planName, facilityName, DateTimeOffset.UtcNow));
        return plan;
    }

    public Result<HACCPPlanId, DomainError> AddCCPDefinition(CCPDefinition definition)
    {
        if (_ccpDefinitions.Any(d => d.Product == definition.Product && d.CCPName == definition.CCPName))
            return DomainError.Conflict($"CCP '{definition.CCPName}' for product '{definition.Product}' already exists.");

        RaiseEvent(new CCPDefinitionAdded(Id, definition, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<HACCPPlanId, DomainError> RemoveCCPDefinition(string product, string ccpName)
    {
        if (!_ccpDefinitions.Any(d => d.Product == product && d.CCPName == ccpName))
            return DomainError.NotFound("CCPDefinition", $"{product}/{ccpName}");

        RaiseEvent(new CCPDefinitionRemoved(Id, product, ccpName, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case HACCPPlanCreated e: Id = e.Id; PlanName = e.PlanName; FacilityName = e.FacilityName; break;
            case CCPDefinitionAdded e: _ccpDefinitions.Add(e.Definition); break;
            case CCPDefinitionRemoved e: _ccpDefinitions.RemoveAll(d => d.Product == e.Product && d.CCPName == e.CCPName); break;
        }
    }
}
