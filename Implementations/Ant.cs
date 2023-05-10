using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AntColonySimulation.Definitions;
using AntColonySimulation.Utils.Functional;
using AntColonySimulation.Utils.Geometry;

namespace AntColonySimulation.Implementations;

public class Ant: ISimulationAgent<AntState>
{
    private enum Mode { Forage, Return }
    
    private const float FoodAttentionThreshold = 0.1f;
    private const float PheromoneAttentionThreshold = 0.09f;
    private const float CarryingCapacity = 5f;
    private const int DepositPheromoneAfterSteps = 6;
    private const int StepsWithoutEventBeforeReset = 750;
    private const int InitialIgnorePheromonesForSteps = 50;

    public string Id { get; }
    public AntState State { get; private set; }
    
    private Mode _mode = Mode.Forage;
    
    private const float TwoPi = (float) (2 * Math.PI);
    
    private bool _hasUnresolvedResourceAccessRequest;
    private int _randomStepsSinceLastTurn = 1;
    private readonly int _turnAfterRandomSteps = 80;
    private int _stepsSincePheromoneDeposited = 1;
    private readonly Random _rnd;
    private float _maxTurningAngle = 0.1f;
    private float _desiredOrientation;
    private IOption<float> _currentHighestPheromoneAmountSensed = new None<float>();
    private IOption<(float, float)> _lastFoodTakenPos = new None<(float, float)>();
    private readonly (float, float) _home;
    private int _stepsWithoutEvent;
    private int _ignorePheromonesForSteps;
    private readonly float _initialSpeed;


    public Ant(
        string id, 
        AntState state, 
        (float, float) home, 
        Random rnd
    ) {
        _rnd = rnd;
        Id = id;
        State = state;
        _initialSpeed = State.Speed;
        _home = home;
        _desiredOrientation = State.Orientation;
        // Add a random int between 10% and 30% of _turnAfterRandomSteps with a random sign
        _turnAfterRandomSteps = _rnd.Next((int) (_turnAfterRandomSteps * 0.1f), (int) (_turnAfterRandomSteps * 0.43)) * (_rnd.Next(0, 2) * 2 - 1);
    }

    public void Act(ISimulationArena<AntState> arena, in float timeDelta)
    {
        ResetSpeedAndTurningAngle();
        if (_hasUnresolvedResourceAccessRequest)
        {
            return;
        }
        
        if (ShouldDepositFood(arena))
        {
            DepositFood();
            MoveWithOrientation(arena, timeDelta);
            return;
        }
        
        if (_mode == Mode.Return)
        {
            ReturnHome(arena, timeDelta);
            return;
        }
        
        var sensedFoodWithDistanceOrientationSaliency = GetFoodInSensoryFieldWithDistanceAndSaliency(arena);
        var foodToPickUp = GetFoodToPickUp(sensedFoodWithDistanceOrientationSaliency);
        if (foodToPickUp.IsSome())
        {
            var food = foodToPickUp.Get();
            arena.Resources.TryGetValue(food.Key, out var foodResource);
            if (foodResource == null) return;
            // Split foodResource, taking the min of the amount left and the amount the ant can carry
            var amountToPickUp = Math.Min(
                CarryingCapacity - State.TotalFoodCarried,
                foodResource.Amount
            );
            if (amountToPickUp <= 0f) return;
            var (takenResource, remainingResource) = food.Split(amountToPickUp);
            if (remainingResource.IsNone() || (remainingResource is Some<ISimulationResource> foodResourceRemaining && foodResourceRemaining.Get().Amount <= 0.01f))
            {
                arena.RaiseResourceDepletedEvent(food.Key);
                arena.Resources.TryRemove(food.Key, out _);
            }

            if (!takenResource.IsSome()) return;
            TakeFood((takenResource.Get() as FoodResource)!);
            return;
        }

        if (_mode == Mode.Forage)
        {
            Forage(arena, sensedFoodWithDistanceOrientationSaliency, timeDelta);
        }
    }

    private void ResetSpeedAndTurningAngle()
    {
        if (Math.Abs(State.Speed - _initialSpeed) > 0.01f)
        {
            State = State.WithData(speed: _initialSpeed);
        }

        if (Math.Abs(_maxTurningAngle - 0.1f) > 0.01f)
        {
            _maxTurningAngle = 0.1f;
        }
    }

    public bool WithinSensoryField(float x1, float y1)
    {
        var dist = Geometry2D.EuclideanDistance(State.X, State.Y, x1, y1);
        if (dist > State.SensoryFieldRadius) return false;
        var orientationTowardsObject = Math.Atan2(y1 - State.Y, x1 - State.X);
        var rawAbsDifference = Math.Abs(State.Orientation - orientationTowardsObject);
        var absRadiansDistance = Math.Min(TwoPi - rawAbsDifference, rawAbsDifference);
        return absRadiansDistance <= State.SensoryFieldAngelRadHalved;
    }

    private bool ShouldDepositFood(ISimulationArena<AntState> arena)
    {
        if (State.TotalFoodCarried == 0f) return false;
        // Home is in the center of the left half of the canvas
        var homeX = (float)arena.Home.X;
        var homeY = (float)arena.Home.Y;
        var dist = Geometry2D.EuclideanDistance(State.X, State.Y, homeX, homeY);
        return (dist <= 5f);
    }

    private void DepositFood()
    {
        State = State.WithData(totalFoodCarried: 0f);
        // If we remember a last food position, turn towards it
        // otherwise, turn around
        _desiredOrientation = _lastFoodTakenPos.MatchReturn(
            t => MathF.Atan2(t.Item1.Item2 - t.Item2.Y, t.Item1.Item1 - t.Item2.X),
            (s) => (float)((s.Orientation + Math.PI) % TwoPi),
            State
        );
        State = State.WithData(orientation: _desiredOrientation);
        _mode = Mode.Forage;
        _currentHighestPheromoneAmountSensed = new None<float>();
        _stepsWithoutEvent = 0;
    }
    
    public void SolveResourceAccessRequest(IOption<ISimulationResource> maybeResource)
    {
        _hasUnresolvedResourceAccessRequest = false;
        if (maybeResource is not Some<ISimulationResource> { Value.Type: "food" } some) return;
        _lastFoodTakenPos = new Some<(float, float)>((some.Value.X, some.Value.Y));
        // Turn towards home 
        _desiredOrientation = MathF.Atan2(_home.Item2 - State.Y, _home.Item1 - State.X);
        State = State.WithData(totalFoodCarried: State.TotalFoodCarried + some.Value.Amount, orientation: _desiredOrientation);
        _mode = Mode.Return;
        _currentHighestPheromoneAmountSensed = new None<float>();
        _stepsWithoutEvent = 0;
    }
    
    private void TakeFood(FoodResource food)
    {
        _hasUnresolvedResourceAccessRequest = false;
        _lastFoodTakenPos = new Some<(float, float)>((food.X, food.Y));
        // Turn towards home 
        _desiredOrientation = MathF.Atan2(_home.Item2 - State.Y, _home.Item1 - State.X);
        State = State.WithData(totalFoodCarried: State.TotalFoodCarried + food.Amount, orientation: _desiredOrientation);
        _mode = Mode.Return;
        _currentHighestPheromoneAmountSensed = new None<float>();
        _stepsWithoutEvent = 0;
    }
    

    private float EstimateOrientationTowardsResourceGradient(List<(ISimulationResource, float, float)> resources)
    {
        // Determine sensory field sector (bin size 10 degrees) with the highest NUMBER of pheromones,
        // then move towards the one with the lowest intensity in the sector with the highest number of pheromones.
        var sectorCounts = new Dictionary<int, int>();
        var resourcesBySector = new Dictionary<int, List<ISimulationResource>>();
        foreach (var (resource, _, orientation) in resources)
        {
            var sector = (int)Math.Floor(orientation / (Math.PI / 18));
            // Add sector if not exists
            sectorCounts.TryAdd(sector, 0);
            sectorCounts[sector] += 1;
            var currList = resourcesBySector.TryGetValue(sector, out var list) ? list : new List<ISimulationResource>();
            currList.Add(resource);
            resourcesBySector[sector] = currList;
        }
        
        var maxSectorKey = sectorCounts.MaxBy(t => t.Value).Key;
        var resourcesInMaxSector = resourcesBySector[maxSectorKey];
        
        // Always move towards the pheromone with the lowest intensity
        var minAmountPheromone =
            resourcesInMaxSector.MinBy(r => r.Amount);
        return null != minAmountPheromone ? 
            (float)Math.Atan2(minAmountPheromone.Y - State.Y, minAmountPheromone.X - State.X) :
            State.Orientation;
    }

    private void ReturnHome(ISimulationArena<AntState> arena, in float timeDelta)
    {
        // If home is within sensory field, move to i
        var (homeX, homeY) = ((float)arena.Home.X, (float)arena.Home.Y);
        if (WithinSensoryField(homeX, homeY))
        {
            // Reduce speed when approaching home
            State = State.WithData(speed: 0.7f * State.Speed);
            // Increase max turn angle when approaching home
            _maxTurningAngle = 1.2f * _maxTurningAngle;
            var angle = Math.Atan2(homeY - State.Y, homeX - State.X);
            _desiredOrientation = (float) angle % TwoPi;
            State = State.WithData(orientation: GetNewOrientation());
            MoveWithOrientation(arena, timeDelta);
            return;
        }
        var currentHighest = _currentHighestPheromoneAmountSensed.MatchReturn<float?,float?>(
            (t) => t.Item1,
            (_) => null,
            null
        );

        if (_stepsWithoutEvent >= StepsWithoutEventBeforeReset)
        {
            _ignorePheromonesForSteps = InitialIgnorePheromonesForSteps;
            _stepsWithoutEvent = 0;
            if (_currentHighestPheromoneAmountSensed.IsSome())
            {
                _currentHighestPheromoneAmountSensed = new None<float>();
            }
        }

        if (_ignorePheromonesForSteps == 0)
        {
            // Get all pheromones in sensory field
            var pheromonesWithDistanceAndOrientation =
                arena.ResourcesInSensoryField(this, "pheromone", PheromoneAttentionThreshold, currentHighest);

            if (pheromonesWithDistanceAndOrientation.Count > 0)
            {
                var maxPheromone = 0f;
                foreach (var (pheromone, _, _) in pheromonesWithDistanceAndOrientation)
                {
                    if (pheromone.Amount > maxPheromone) maxPheromone = pheromone.Amount;
                }
                _currentHighestPheromoneAmountSensed = new Some<float>(maxPheromone);

                var gradient = EstimateOrientationTowardsResourceGradient(pheromonesWithDistanceAndOrientation);

                _desiredOrientation = gradient;
                State = State.WithData(orientation: GetNewOrientation());
                MoveWithOrientation(arena, timeDelta);
                _stepsWithoutEvent++;
                return;
            }
            _stepsWithoutEvent++;
            MoveRandomly(arena, timeDelta);
            return;
        }
        _ignorePheromonesForSteps--;
        MoveRandomly(arena, timeDelta);
    }

    private void Forage(ISimulationArena<AntState> arena, List<(ISimulationResource, float, float, double)> food, in float timeDelta)
    {
        // If there is food in our sensory field,
        // move towards it
        if (food.Count > 0)
        {
            // Sort by salience (Item3)
            food.Sort((t1, t2) => t2.Item3.CompareTo(t1.Item3));
            // Get most salient food
            var mostSalientFood = food[0].Item1;
            // Determine new orientation
            var foodX = mostSalientFood.X;
            var foodY = mostSalientFood.Y;
            var newOrientation = (float)Math.Atan2(
                foodY - State.Y, 
                foodX - State.X
            );
            // Reduce speed when approaching home
            State = State.WithData(speed: 0.7f * State.Speed);
            // Increase max turn angle when approaching home
            _maxTurningAngle = 1.2f * _maxTurningAngle;
            _desiredOrientation = newOrientation;
            State = State.WithData(orientation: GetNewOrientation());
            // Move towards food
            MoveWithOrientation(arena, timeDelta);
            return;
        }

        var currentHighest = _currentHighestPheromoneAmountSensed.MatchReturn<float?, float?>(
            (t) => t.Item1,
            (_) => null,
            null
        );
        
        if (_stepsWithoutEvent >= StepsWithoutEventBeforeReset)
        {
            _ignorePheromonesForSteps = InitialIgnorePheromonesForSteps;
            _stepsWithoutEvent = 0;
            if (_currentHighestPheromoneAmountSensed.IsSome())
            {
                _currentHighestPheromoneAmountSensed = new None<float>();
            }
        }
        
        if (_ignorePheromonesForSteps == 0)
        {
            // Get all pheromones in sensory field
            var pheromonesWithDistanceAndOrientation =
                arena.ResourcesInSensoryField(this, "pheromone-r", PheromoneAttentionThreshold, currentHighest);
            if (pheromonesWithDistanceAndOrientation.Count > 0)
            {
                var maxPheromone = 0f;
                foreach (var (pheromone, _, _) in pheromonesWithDistanceAndOrientation)
                {
                    if (pheromone.Amount > maxPheromone) maxPheromone = pheromone.Amount;
                }
                _currentHighestPheromoneAmountSensed = new Some<float>(maxPheromone);
                
                var gradient = EstimateOrientationTowardsResourceGradient(pheromonesWithDistanceAndOrientation);
                _desiredOrientation = gradient;
                State = State.WithData(orientation: GetNewOrientation());

                MoveWithOrientation(arena, timeDelta);
                _stepsWithoutEvent++;
                return;
            }
            _stepsWithoutEvent++;
            MoveRandomly(arena, timeDelta);
            return;
        }
        _ignorePheromonesForSteps--;
        
        MoveRandomly(arena, timeDelta);
    }

    private List<(ISimulationResource, float, float, double)> GetFoodInSensoryFieldWithDistanceAndSaliency(ISimulationArena<AntState> arena)
    {
        var foodWithDistanceAndOrientation = 
            arena.ResourcesInSensoryField(this, "food", FoodAttentionThreshold);
        return foodWithDistanceAndOrientation
            .Select<(ISimulationResource, float, float), (ISimulationResource, float, float, double)>(
                t => (t.Item1, t.Item2, t.Item3, t.Item1.Amount / Math.Pow(t.Item2, 2))
            ).ToList();
    }
    
    private IOption<ISimulationResource> GetFoodToPickUp(List<(ISimulationResource, float,float, double)> foodWithDistanceOrientationSaliency)
    {
        // If there is no food in sensory field, don't pick up
        if (foodWithDistanceOrientationSaliency.Count == 0) return new None<ISimulationResource>();
        // Sort by distance (Item2)
        foodWithDistanceOrientationSaliency.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
        //If there is food with a distance smaller than or equal to 2, pick up
        var shouldPickUp = foodWithDistanceOrientationSaliency[0].Item2 <= 2;
        if (!shouldPickUp) return new None<ISimulationResource>();
        return new Some<ISimulationResource>(foodWithDistanceOrientationSaliency[0].Item1);
    }
    
    private void MoveWithOrientation(ISimulationArena<AntState> arena, float timeDelta)
    {
        
        var (currX, currY, currOrientation, currSpeed) = (State.X, State.Y, State.Orientation, State.Speed);
        var deltaX = (float)Math.Cos(currOrientation) * currSpeed * timeDelta;
        var deltaY = (float)Math.Sin(currOrientation) * currSpeed * timeDelta;
        var newX = MathF.Max(0, MathF.Min(arena.Width - 1, currX + deltaX));
        var newY = MathF.Max(0, MathF.Min(arena.Height - 1, currY + deltaY));
        // Check if moving would hit a wall
        var withinBounds = arena.WithinBounds(currX + deltaX, currY + deltaY);
        if (!withinBounds)
        {
            // Determine motion away from 
            (var orientation, deltaX, deltaY) = GetOrientationAndPosDeltaAwayFromWalls(
                arena, 
                timeDelta,
                currOrientation,
                currSpeed,
                currX,
                currY
            );
            _desiredOrientation = orientation;
            // Move in new direction
            newX = MathF.Max(0, MathF.Min(arena.Width - 1, currX + deltaX));
            newY = MathF.Max(0, MathF.Min(arena.Height - 1, currY + deltaY));
            State = State.WithData(orientation: orientation, x: newX, y: newY);
        }
        else
        {
            State = State.WithData(x: newX, y: newY);   
        }

        DepositPheromonePeriodically(arena);
    }

    private void DepositPheromonePeriodically(ISimulationArena<AntState> arena)
    {
        if (_stepsSincePheromoneDeposited % DepositPheromoneAfterSteps == 0)
        {
            DepositPheromone(arena);
            _stepsSincePheromoneDeposited = 1;
            return;
        }
        _stepsSincePheromoneDeposited++;
    }

    private (float, float, float) GetOrientationAndPosDeltaAwayFromWalls(
        ISimulationArena<AntState> arena, 
        float timeDelta,
        float currOrientation,
        float currSpeed,
        float currX,
        float currY
    ) {
        
        // Chose a random new orientation, 
        // see if going in that direction would hit a wall.
        // Repeat until a direction is found that does not hit a wall
        // then return that direction
        var hitsWall = true;
        var newOrientation = currOrientation;
        var deltaX = 0f;
        var deltaY = 0f;
        while (hitsWall)
        {
            newOrientation = (float)_rnd.NextDouble() * TwoPi;
            deltaX = (float)Math.Cos(newOrientation) * currSpeed * timeDelta;
            deltaY = (float)Math.Sin(newOrientation) * currSpeed * timeDelta;
            hitsWall = !arena.WithinBounds(currX + deltaX, currY + deltaY);
        }

        return (newOrientation, deltaX, deltaY);
    }

    private void MoveRandomly(ISimulationArena<AntState> arena, in float timeDelta)
    {
        // Set random orientation every n steps, then MoveWithOrientation
        if (_randomStepsSinceLastTurn % _turnAfterRandomSteps == 0)
        {
            var random = _rnd;
            // Get a delta orientation between -PI/4 and PI/4
            var orientationDelta = (float)(random.NextDouble() * Math.PI / 2 - Math.PI / 4);
            _desiredOrientation = State.Orientation + orientationDelta;
            State = State.WithData(orientation: GetNewOrientation());
            _randomStepsSinceLastTurn = 1;
        }
        else
        {
            _randomStepsSinceLastTurn++;
        }

        if (Math.Abs(State.Orientation - _desiredOrientation) > 0.01f)
        {
            State = State.WithData(orientation: GetNewOrientation());
        }
        //else
        //{
        //    _desiredOrientation = State.Orientation;
        //}
        MoveWithOrientation(arena, timeDelta);
    }

    private float GetNewOrientation()
    {
        // Compare current orientation with desired orientation,
        // calculate difference, make sure it is within acceptable range,
        // then rotate by the min of the difference and the max turning angle (with the sign of the difference) 
        var clockwiseRadians = _desiredOrientation < State.Orientation ?
            TwoPi - State.Orientation + _desiredOrientation :
            _desiredOrientation - State.Orientation;
        var counterclockwiseRadians = _desiredOrientation > State.Orientation ?
            TwoPi - _desiredOrientation + State.Orientation :
            State.Orientation - _desiredOrientation;
        var minTurn = Math.Min(clockwiseRadians, counterclockwiseRadians);
        var sign = clockwiseRadians < counterclockwiseRadians ? 1 : -1;
        var orientationChange = Math.Min(minTurn, _maxTurningAngle);
        return State.Orientation + orientationChange * sign;
    }

    private void DepositPheromone(ISimulationArena<AntState> arena)
    {
        switch (_mode)
        {
            case Mode.Forage:
                arena.AddPheromone("pheromone", new Point(State.X, State.Y), 0.25f, 0.10f);
                break;
            case Mode.Return:
                arena.AddPheromone("pheromone-r", new Point(State.X, State.Y), 0.25f, 0.10f);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }
    
    
    
}