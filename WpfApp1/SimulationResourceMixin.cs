using WpfApp1.utils.fn;

namespace WpfApp1;

public interface SimulationResourceMixin: SimulationObjectMixin
{
    public IOption<int> LockedByAgentId { get; set; }
}