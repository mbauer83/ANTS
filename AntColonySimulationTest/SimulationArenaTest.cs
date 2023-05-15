using System.Collections.Concurrent;
using AntColonySimulation.Definitions;
using AntColonySimulation.Implementations;

namespace AntColonySimulationTest;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class SimulationArenaTests
{
    private SimulationArena _simulationArena;
    private ConcurrentDictionary<string, ISimulationResource> _resources;
    private List<ISimulationAgent> _agents;
    private SimulationCanvas _canvas;
    private PheromoneResourcePool _pheromoneResourcePool;
    private PheromoneResourceReturnPool _pheromoneResourceReturnPool;

    [SetUp]
    public void Setup()
    {
        _resources = new ConcurrentDictionary<string, ISimulationResource>();
        _agents = new List<ISimulationAgent>();
        _canvas = new SimulationCanvas(100, 100);
        _pheromoneResourcePool = new PheromoneResourcePool(1, 1);
        _pheromoneResourceReturnPool = new PheromoneResourceReturnPool(1, 1);
        _simulationArena = new SimulationArena(
            100,
            100,
            _resources,
            _agents,
            _canvas,
            _pheromoneResourcePool,
            _pheromoneResourceReturnPool
        );
    }

    [Test]
    public void TestSimulationArenaWidth()
    {
        Assert.That(_simulationArena.Width, Is.EqualTo(100));
    }

    [Test]
    public void TestSimulationArenaHeight()
    {
        Assert.That(_simulationArena.Height, Is.EqualTo(100));
    }

    [Test]
    public void TestSimulationArenaHome()
    {
        Assert.That(_simulationArena.Home.X, Is.EqualTo(25));
        Assert.That(_simulationArena.Home.Y, Is.EqualTo(50));
    }

    [Test]
    public void TestSimulationArenaResources()
    {
        var resource = new PheromoneResource(new System.Windows.Point(10, 10), 0.2f, 0.2f);
        _resources.TryAdd("test", resource);
        Assert.That(_simulationArena.Resources.Count, Is.EqualTo(1));
        Assert.That(_simulationArena.Resources.First().Value, Is.EqualTo(resource));
    }

    [Test]
    public void TestSimulationArenaTogglePause()
    {
        // toggle
        _simulationArena.TogglePause();
        // Check state of _run with reflection
        var runField = typeof(SimulationArena).GetField("_run",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var runValueRaw = runField.GetValue(_simulationArena);
        var runValue = (bool)runValueRaw;
        Assert.That(runValue, Is.EqualTo(true));
        _simulationArena.TogglePause();
        runField = typeof(SimulationArena).GetField("_run",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        runValueRaw = runField.GetValue(_simulationArena);
        runValue = (bool)runValueRaw;
        Assert.That(runValue, Is.EqualTo(false));
    }

    [Test]
    public void TestSimulationArenaResourcesInSensoryField()
    {
        var resource = new FoodResource(new System.Windows.Point(10, 10), 0.2f, 0.2f);
        _resources.TryAdd("test", resource);
        var agent = new Ant("testAnt", new AntState(10, 8, 2 * MathF.PI, 120f, 0, 180f, 100), (0, 0), new Random());
        var resourcesInSensoryField = _simulationArena.ResourcesInSensoryField(agent, "food");
        Assert.That(resourcesInSensoryField, Has.Count.EqualTo(1));
        Assert.That(resourcesInSensoryField.First().Item1, Is.EqualTo(resource));
    }

    [Test]
    public void TestSimulationArenaAgentsInSensoryField()
    {
        var agents = new List<ISimulationAgent>();
        var agent1 = new Ant("testAnt1", new AntState(10, 0, 2 * MathF.PI, 0, 0, 180, 100), (0, 0), new Random());
        var agent2 = new Ant("testAnt2", new AntState(10, 10, MathF.PI, 0, 0, 180, 100), (0, 0), new Random());
        agents.Add(agent1);
        agents.Add(agent2);
        var arena = new SimulationArena(100, 100, new ConcurrentDictionary<string, ISimulationResource>(), agents,
            new SimulationCanvas(100, 100), new PheromoneResourcePool(1, 1), new PheromoneResourceReturnPool(1, 1));
        var agentsInSensoryField = arena.AgentsInSensoryField(agent1);
        Assert.That(agentsInSensoryField, Has.Count.EqualTo(1));
        Assert.That(agentsInSensoryField.First().Item1, Is.EqualTo(agent2));
    }

    [Test]
    public void TestSimulationArenaWithinBounds()
    {
        Assert.That(_simulationArena.WithinBounds(5, 5), Is.EqualTo(true));
        Assert.That(_simulationArena.WithinBounds(-5, -5), Is.EqualTo(false));
        Assert.That(_simulationArena.WithinBounds(95, 95), Is.EqualTo(true));
        Assert.That(_simulationArena.WithinBounds(105, 105), Is.EqualTo(false));
    }
}