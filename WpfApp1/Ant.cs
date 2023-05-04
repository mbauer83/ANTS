using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.utils.fn;
using WpfApp1.utils.geometry;

namespace WpfApp1;

public class Ant: ISimulationAgent<AntState>
{
    private enum Mode { Forage, Return }
    private enum MovementMode { Random, Gradient }
    
    private Mode _mode = Mode.Forage;
    private MovementMode _movementMode = MovementMode.Random;
    
    private float TwoPi = (float) (2 * Math.PI);
    
    private float MaxFood = 1;
    
    private bool _hasUnresolvedResourceAccessRequest = false;
    public string Id { get; }
    public AntState State { get; private set; }

    private IOption<ISimulationResource> _lastStrongestPheromoneSource;
    private float _distanceWithoutFoodSensed = 0f;
    private float _exploreAfterDistanceWithoutFoodSensedThreshold = 100f;
    private float _foodAttentionThreshold = 0.1f;
    private float _pheromoneAttentionThreshold = 0.1f;
    private float _carryingCapacity = 5f;
    private int _randomStepsSinceLastTurn = 1;
    private int _turnAfterRandomSteps = 80;
    private int _stepsSincePheromoneDeposited = 1;
    private int _depositPheromoneAfterSteps = 5;
    private int _followedPheromonesForSteps = 0;
    private int _switchToExplorationAfterFollowedPheromonesForSteps = 200;
    private ISimulationResource? _lastPheromoneFollowed = null;
    private Random _rnd;
    private float _maxTurningAngle = 0.1f;
    private float _desiredOrientation = 0f;
    private int _ignorePheromonesForSteps = 0;
    private IOption<float> _lowestIntensityPheromoneSensed = new None<float>();
    private IOption<(float, float)> _lastFoodTakenPos = new None<(float, float)>();
    private (float, float) _home;


    public Ant(string id, AntState state, (float, float) home, Random rnd)
    {
        _rnd = rnd;
        Id = id;
        State = state;
        _home = home;
        _desiredOrientation = State.Orientation;
        _lastStrongestPheromoneSource = new None<ISimulationResource>();
        // Add a random int between 10% and 40% of _turnAfterRandomSteps with a random sign
        _turnAfterRandomSteps = _rnd.Next((int) (_turnAfterRandomSteps * 0.1f), (int) (_turnAfterRandomSteps * 0.4f)) * (_rnd.Next(0, 2) * 2 - 1);
    }

    public async Task Act(SimulationArena<AntState> arena, float timeDelta)
    {
        if (Math.Abs(State.Speed - 180f) > 0.01f)
        {
            State = State.WithData(speed: 180f);
        }
        if (Math.Abs(_maxTurningAngle - 0.1f) > 0.01f)
        {
            _maxTurningAngle = 0.1f;
        }
        if (_hasUnresolvedResourceAccessRequest)
        {
            return;
        }
        
        if (ShouldDepositFood(arena))
        {
            DepositFood(arena);
            MoveWithOrientation(arena, timeDelta);
            return;
        }
        
        if (_mode == Mode.Return)
        {
            ReturnHome(arena, timeDelta);
            return;
        }
        
        var foodInSensoryFieldWithDistanceAndSaliency = GetFoodInSensoryFieldWithDistanceAndSaliency(arena);
        var foodToPickUp = GetFoodToPickUp(foodInSensoryFieldWithDistanceAndSaliency);
        ResourceAccessRequest<AntState> FoodResourceToRequest(ISimulationResource r)
        {
            var amountToPickUp = Math.Min(
                _carryingCapacity,
                r.Amount
            );
            return new ResourceAccessRequest<AntState>(
                "food",
                r.X,
                r.Y,
                amountToPickUp,
                this
            );
        }
        if (foodToPickUp.IsSome())
        {
            arena.AddResourceAccessRequest(FoodResourceToRequest(foodToPickUp.Get()));
            return;
        }

        if (_mode == Mode.Forage)
        {
            Forage(arena, foodInSensoryFieldWithDistanceAndSaliency, timeDelta);
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

    private bool ShouldDepositFood(SimulationArena<AntState> arena)
    {
        if (State.TotalFoodCarried == 0f) return false;
        // Home is in the center of the left half of the canvas
        var homeX = (float)arena.Home.X;
        var homeY = (float)arena.Home.Y;
        var dist = Geometry2D.EuclideanDistance(State.X, State.Y, homeX, homeY);
        return (dist <= 15f);
    }

    private void DepositFood(SimulationArena<AntState> arena)
    {
        State = State.WithData(totalFoodCarried: 0f);
        _ignorePheromonesForSteps = 0;
        _movementMode = MovementMode.Gradient;
        // If we remember a last food position, turn towards it
        // otherwise, turn around
        _desiredOrientation = _lastFoodTakenPos.MatchReturn(
            some => MathF.Atan2(some.Item2 - State.Y, some.Item1 - State.X),
            () => (float)((State.Orientation + Math.PI) % TwoPi)
        );
        State = State.WithData(orientation: _desiredOrientation);
        _mode = Mode.Forage;
        _lowestIntensityPheromoneSensed = new None<float>();
    }
    
    public async void SolveResourceAccessRequest(IOption<ISimulationResource> maybeResource)
    {
        _hasUnresolvedResourceAccessRequest = false;
        if (maybeResource is not Some<ISimulationResource> { Value.Type: "food" } some) return;
        _lastFoodTakenPos = new Some<(float, float)>((some.Value.X, some.Value.Y));
        // Turn towards home 
        _desiredOrientation = MathF.Atan2(_home.Item2 - State.Y, _home.Item1 - State.X);
        State = State.WithData(totalFoodCarried: this.State.TotalFoodCarried + some.Value.Amount, orientation: _desiredOrientation);
        _mode = Mode.Return;
        _lowestIntensityPheromoneSensed = new None<float>();
    }
    

    private float EstimateOrientationTowardsResourceGradient(List<(ISimulationResource, float)> resources)
    {
        
        // Always move towards the pheromone with the lowest intensity
        var minAmountPheromone =
            resources
                .Select(r => r.Item1)
                .OrderBy(r => r.Amount).First();
        var newOrientation = (float)Math.Atan2(minAmountPheromone.Y - State.Y, minAmountPheromone.X - State.X);
        return newOrientation;

        //var maxAmount = resources.Select(r => r.Item1.Amount).Max();
        //var weightedMaxXY = (0d, 0d);
        //foreach (var (resource, distance) in resources)
        //{
        //    var angle = Math.Atan2(resource.Y - State.Y, resource.X - State.X);
        //    var relativeAmount = resource.Amount / maxAmount;
        //    var angleWeight = 1f / MathF.Pow(relativeAmount, 4);
        //    weightedMaxXY = (
        //        weightedMaxXY.Item1 + Math.Cos(angle) * angleWeight,
        //        weightedMaxXY.Item2 + Math.Sin(angle) * angleWeight
        //    );
        //}

        //return (float)Math.Atan2(weightedMaxXY.Item2,
        //    weightedMaxXY.Item1);// + MathF.PI; // add PI to turn in the opposite direction
    }

    private void ReturnHome(SimulationArena<AntState> arena, float timeDelta)
    {
        // If home is within sensory field, move to i
        var (homeX, homeY) = ((float)arena.Home.X, (float)arena.Home.Y);
        if (WithinSensoryField(homeX, homeY))
        {
            // Reduce speed when approaching home
            State = State.WithData(speed: 0.75f * State.Speed);
            // Increase max turn angle when approaching home
            _maxTurningAngle = 1.2f * _maxTurningAngle;
            var angle = Math.Atan2(homeY - State.Y, homeX - State.X);
            _desiredOrientation = (float) angle % TwoPi;
            State = State.WithData(orientation: GetNewOrientation());
            MoveWithOrientation(arena, timeDelta);
            return;
        }
        
        

        // Get all pheromones in sensory field
        var pheromonesWithDistance = 
            arena.ResourcesInSensoryField(this, "pheromone", 0.05f);
        // Map with salience (inverse square law)
        var pheromoneWithSalience = pheromonesWithDistance
            .Select<(ISimulationResource, float), (ISimulationResource, float, double)>(t =>
                (t.Item1, t.Item2, t.Item1.Amount / Math.Pow(t.Item2, 2)))
            .Where(
                t => 
                    _lowestIntensityPheromoneSensed.MatchReturn(
                        l => t.Item1.Amount < l,
                        () => true
                    )
            )
            .ToList();
        pheromoneWithSalience.Sort((t1, t2) => t2.Item3.CompareTo(t1.Item3));
        if (pheromoneWithSalience.Count > 0)
        {
            _lowestIntensityPheromoneSensed = new Some<float>(pheromonesWithDistance
                .Select(t => t.Item1.Amount)
                .Min());
            var gradient = EstimateOrientationTowardsResourceGradient(pheromonesWithDistance);

            _desiredOrientation = gradient;
            State = State.WithData(orientation: GetNewOrientation());
            MoveWithOrientation(arena, timeDelta);
            return;
        }
        MoveRandomly(arena, timeDelta);
    }

    private void Forage(SimulationArena<AntState> arena, List<(ISimulationResource, float, double)> food, float timeDelta)
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
            State = State.WithData(speed: 0.75f * State.Speed);
            // Increase max turn angle when approaching home
            _maxTurningAngle = 1.2f * _maxTurningAngle;
            _desiredOrientation = newOrientation;
            State = State.WithData(orientation: GetNewOrientation());
            // Move towards food
            MoveWithOrientation(arena, timeDelta);
            return;
        }

        // Get all pheromones in sensory field
        var pheromonesWithDistance = 
            arena.ResourcesInSensoryField(this, "pheromone-r", 0.05f);
        // Map with salience (inverse square law)
        var pheromoneWithSalience = pheromonesWithDistance
            .Select<(ISimulationResource, float), (ISimulationResource, float, double)>(t => (t.Item1, t.Item2, t.Item1.Amount / Math.Pow(t.Item2, 2)))
            .Where(
                t => 
                    _lowestIntensityPheromoneSensed.MatchReturn(
                        l => t.Item1.Amount < l,
                        () => true
                    )
            )
            .ToList();
        pheromoneWithSalience.Sort((t1, t2) => t2.Item3.CompareTo(t1.Item3));
        if (pheromoneWithSalience.Count > 0)
        {
            _lowestIntensityPheromoneSensed = new Some<float>(pheromonesWithDistance
                .Select(t => t.Item1.Amount)
                .Min());
            var gradient = EstimateOrientationTowardsResourceGradient(pheromonesWithDistance);
            _desiredOrientation = gradient;
            State = State.WithData(orientation: GetNewOrientation());

            MoveWithOrientation(arena, timeDelta);
            return;
        }
        MoveRandomly(arena, timeDelta);
    }

    private List<(ISimulationResource, float, double)> GetFoodInSensoryFieldWithDistanceAndSaliency(SimulationArena<AntState> arena)
    {
        var foodWithDistance = 
            arena.ResourcesInSensoryField(this, "food", 0.1f);
        return foodWithDistance
            .Select<(ISimulationResource, float), (ISimulationResource, float, double)>(
                t => (t.Item1, t.Item2, t.Item1.Amount / Math.Pow(t.Item2, 2))
            ).ToList();
    }
    
    private IOption<ISimulationResource> GetFoodToPickUp(List<(ISimulationResource, float, double)> foodWithDistanceAndSaliency)
    {
        // If there is no food in sensory field, don't pick up
        if (foodWithDistanceAndSaliency.Count == 0) return new None<ISimulationResource>();
        // Sort by distance (Item2)
        foodWithDistanceAndSaliency.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
        //If there is food with a distance smaller than or equal to 2, pick up
        var shouldPickUp = foodWithDistanceAndSaliency[0].Item2 <= 2;
        if (!shouldPickUp) return new None<ISimulationResource>();
        return new Some<ISimulationResource>(foodWithDistanceAndSaliency[0].Item1);
    }
    
    private void MoveWithOrientation(SimulationArena<AntState> arena, float timeDelta)
    {
        var deltaX = (float)Math.Cos(State.Orientation) * State.Speed * timeDelta;
        var deltaY = (float)Math.Sin(State.Orientation) * State.Speed * timeDelta;
        // Check if moving would hit a wall
        var withinBounds = arena.WithinBounds(State.X + deltaX, State.Y + deltaY);
        if (!withinBounds)
        {
            _ignorePheromonesForSteps = 50;
            // Switch movement mode to random
            _movementMode = MovementMode.Random;
            // Invert orientation
            var newOrientation = GetOrientationAwayFromWalls(arena, timeDelta);
            _desiredOrientation = newOrientation;
            deltaX = (float)Math.Cos(State.Orientation) * State.Speed * timeDelta;
            deltaY = (float)Math.Sin(State.Orientation) * State.Speed * timeDelta;
            // Move in new direction
            State = State.WithData(orientation: newOrientation, x: State.X - deltaX, y: State.Y - deltaY);
        }
        else
        {
            State = State.WithData(x: State.X + deltaX, y: State.Y + deltaY);   
        }
        if (_stepsSincePheromoneDeposited % _depositPheromoneAfterSteps == 0)
        {
            DepositPheromone(arena);
            _stepsSincePheromoneDeposited = 1;
        }
        else
        {
            _stepsSincePheromoneDeposited++;
        }
    }

    private float GetOrientationAwayFromWalls(SimulationArena<AntState> arena, float timeDelta)
    {
        
        // Chose a random new orientation, 
        // see if going in that direction would hit a wall.
        // Repeat until a direction is found that does not hit a wall
        // then return that direction
        var hitsWall = true;
        var newOrientation = State.Orientation;
        while (hitsWall)
        {
            newOrientation = (float)_rnd.NextDouble() * TwoPi;
            var deltaX = (float)Math.Cos(newOrientation) * State.Speed * timeDelta;
            var deltaY = (float)Math.Sin(newOrientation) * State.Speed * timeDelta;
            hitsWall = !arena.WithinBounds(State.X + deltaX, State.Y + deltaY);
        }

        return newOrientation;

        // See if turning left or right by an eigth of a circle would hit a wall.
        // Increase by eigths of a circle until no wall is hit, then return the corresponding
        // orientation
        //var hitsWall = true;
        //var currentOrientation = State.Orientation;
        //var n = 1;
        //while (hitsWall)
        //{
        //    var nEighthsOfCircle = (float)(Math.PI / 8) * n;
        //    var orientationPlus = (State.Orientation + nEighthsOfCircle) % TwoPi;
        //    var orientationMinus = (State.Orientation - nEighthsOfCircle) % TwoPi;
        //    var deltaXPlus = (float)Math.Cos(orientationPlus) * State.Speed * timeDelta;
        //    var deltaYPlus = (float)Math.Sin(orientationPlus) * State.Speed * timeDelta;
        //    var deltaXMinus = (float)Math.Cos(orientationMinus) * State.Speed * timeDelta;
        //    var deltaYMinus = (float)Math.Sin(orientationMinus) * State.Speed * timeDelta;
        //    var withinBoundsPlus = arena.WithinBounds(State.X + deltaXPlus, State.Y + deltaYPlus);
        //    var withinBoundsMinus = arena.WithinBounds(State.X + deltaXMinus, State.Y + deltaYMinus);
        //    
        //    if (withinBoundsPlus)
        //    {
        //        hitsWall = false;
        //        currentOrientation = orientationPlus;
        //    }
        //    else if (withinBoundsMinus)
        //    {
        //        hitsWall = false;
        //        currentOrientation = orientationMinus;
        //    }

        //    n++;
        //}
        //return currentOrientation;
    }

    private void MoveRandomly(SimulationArena<AntState> arena, float timeDelta)
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
        // calculate difference, make sure it is within +-PI,
        // divide difference by max turning angle,
        // use LERP to interpolate, then return return
        var clockwiseRadians = _desiredOrientation < State.Orientation ?
            TwoPi - State.Orientation + _desiredOrientation :
            _desiredOrientation - State.Orientation;
        var counterclockwiseRadians = _desiredOrientation > State.Orientation ?
            TwoPi - _desiredOrientation + State.Orientation :
            State.Orientation - _desiredOrientation;
        var minTurn = Math.Min(clockwiseRadians, counterclockwiseRadians);
        var sign = clockwiseRadians < counterclockwiseRadians ? 1 : -1;
        var orientationChange = Math.Min(minTurn, _maxTurningAngle);
        return State.Orientation + ((float)orientationChange * sign);
    }

    private void DepositPheromone(SimulationArena<AntState> arena)
    {
        ISimulationResource pheromone = 
            _mode == Mode.Forage ? 
                new PheromoneResource(State.X, State.Y, 0.8f, 0.35f) : 
                new PheromoneResourceReturn(State.X, State.Y, 0.8f, 0.35f);
        arena.AddPheromone(pheromone);
    }
    
    
    
}