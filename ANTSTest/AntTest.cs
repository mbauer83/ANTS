using System.Collections.Concurrent;
using ANTS.Definitions;
using ANTS.Implementations;


namespace ANTSTest;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class AntTests
{
    private SimulationArena _arena;
    private Ant _ant;
    private Random _rnd;

    [SetUp]
    public void Setup()
    {
        _rnd = new Random();
        _ant = new Ant("test_ant", new AntState(0f, 0f, MathF.PI, 130f, 0f, 0f, 120), (0f, 0f), _rnd);
        
        _arena = new SimulationArena(100, 100, new ConcurrentDictionary<string, ISimulationResource>(),
            new List<ISimulationAgent>{_ant}, new SimulationCanvas(100, 100), new PheromoneResourcePool(1, 1),
            new PheromoneResourceReturnPool(1, 1));
    }

    [Test]
    public void Ant_Id_IsSet()
    {
        Assert.That(_ant.Id, Is.EqualTo("test_ant"));
    }

    [Test]
    public void Ant_State_IsSet()
    {
        Assert.That(_ant.State, Is.Not.Null);
    }

    [Test]
    public void Ant_State_IsAntState()
    {
        Assert.That(_ant.State, Is.InstanceOf<AntState>());
    }

    [Test]
    public void Ant_Act_DoesNotThrowException()
    {
        Assert.DoesNotThrowAsync(async () => await _ant.Act(_arena, 0.33f));
    }
}
