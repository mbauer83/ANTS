# ANTS

**ANTS** (**A**nt **N**utrition-**T**racking **S**imulation) is an ant-colony simulation written from scratch in **C#11** with **.NET 7** and using **Wpf** primitives for graphical presentation. It creates a simulated 2D-world where ants forage for food and return it to their nest.

**NOTE:** This application deliberately avoids using a more design-oriented color scheme in order to improve accessibility for people with impaired color-vision.

The ants' behavior is based on entirely local information and local rules, with two small exceptions: The ants have a memory of the direction where they last picked up food and will turn towards it after having deposited food at the nest. Furthermore, they remember the orientation of the nest, and will turn towards it after having picked up food. However, this information is only used for the initial turning - all movement thereafter is generated as usual.

The nest is in the center of the left half of the world, while an initial food-cluster is in the center of the right half.

**More food can be added by holding down the left mouse-button and dragging the mouse.**

This project is intended mainly for purposes of demonstration and learning. There are no non-functional requirements for particularly high performance or enterprise-grade modularity, documentation or inspectability.

It demonstrates one possible way to approach the development of a basic artificial-life simulation with modern C# features.
The simulation itself demonstrates how complex and intricate patterns and dynamics can emerge from relatively simple, local rules.


## Ant Behavior

The ants' overall behavior is rather simple: They are either foraging or returning food to the nest, regularly depositing a corresponding kind of pheromone, which evaporates at a certain rate which decreases with the intensity/amount of the pheromone. In both cases, they first check whether the target is within their sensory field (indicated by light-blue "vision cones") and move towards it if so. 

If the target is not in sight, they will look for pheromones of the kind opposite to the one they're currently depositing. From the position and intensity of all sensed pheromones, they determine a new orientation and move in that direction. If neither their target nor pheromones are in their sensory field, they move randomly.
When encountering a wall, a random orientation is chosen until that orientation does not lead into a wall.

To avoid following trails that do not lead to the target for too long, the ants count the number of steps they took without finding their target, and when that exceeds a threshold, they will move randomly for a certain number of steps, ignoring pheromones before resuming their normal behavior.

## Challenges & Approaches in the Problem-Domain

There are several challenges when developing an ant-colony simulation. The most difficult challenge in the actual problem-domain is the pheromone-trail following. 

Pheromones are deposited as discrete objects, so there are no paths, there is no inherent direction to follow. Since pheromones evaporate, the ants also have to move in a direction where the pheromone-intensity decreases (as those pheromones have had longer to decay, and are thus closer to the target from which the ant started). This is somewhat more difficult than it may sound. We can certainly determine the "center of mass" of pheromones in an ants sensory field rather easily by averaging the positions weighted by intensity. But moving towards that point would mean moving towards increasing instead of decreasing pheromone-intensity. If we added 180° to the direction thus determined, that would just lead ants away from any trail instead of following a trail in the decreasing direction. 

Always moving towards the pheromone with the lowest intensity in the sensory field works relatively well at first - but at some point, the first trails between nest and food will have evaporated. Other trails in random directions will then be older and thus of lower intensity. Thus, the ants start following random trails very quickly - and any order that had emerged destabilizes.

The solution I have employed here leads to very stable trails, though (just as in real life) some will make significant detours, and there will sometimes be ants which move in the wrong direction. Also - in rare cases, a number of foraging and returning ants will create a closed path which does not involve the nest or food. This is mitigated by the random steps after following pheromone-trails for too long.

To determine their orientation when following pheromone-trails, ants will not simply orient towards the pheromone with the lowest intensity in their sensory field. Instead, they keep track of the intensity of the last pheromone they followed, and at every step they will only be aware of pheromones of lower intensity. They then subdivide their sensory field into sectors of a certain number of degrees (ten, currently) and count the number of pheromones in each sector. Finally, they determine the lowest-intensity pheromone-resource in the sector with the most pheromones, and move in its direction.

This is a good approximation to the desired behavior, which can be specified as "move along the strongest trail in the decreasing direction". Disregarding pheromones with an intensity equal to or greater than the intensity of the pheromone they last moved towards reduces the incidence of moving away from a trail they were following and contributes to following trails in the decreasing direction. Selecting from the sector with the greatest number of pheromones is a sufficient heuristic for orienting towards the strongest trails. And finally, selecting the weakest pheromone (below the intensity of the previously tracked pheromone) in that sector is a sufficient heuristic to chose a direction of decreasing intensity.

As heuristics, these approaches sometimes fail - but then, real-life animals exclusively use heuristics in action-selection, so this actually contributes to the emergence of patterns similar to those that appear in nature when heuristics lead animals astray. 

## Challenges & Approaches in the Application-Domain

There are also challenges in implementation-details. First, the ants' behavior is computationally intensive, so we should make use of parallelism/concurrency to use resources efficiently. Second, the food is a shared resource, so we have to coordinate access and modification by ants. Third, the amount of pheromones in the world becomes very large and changes constantly during the runtime of the program. Since each pheromone is its own data structure which has to be created and destroyed, garbage-collection can become problematic. Finally, drawing a large number of moving and rotating ants with "vision cones" as well as a large number of pheromones with different shades depending on their intensity of evaporation is computationally intensive and has to be optimized.

There are many ways to achieve concurrency/parallelization in C#, each with their different strenghts and weaknesses.
This application uses `Task.WaitAll`, partitioning, `async`/`await`, a preconfigured thread-pool and `ConfigureAwait(false)` in various combinations.

Access to the shared resources is handled by storing them in a `ConcurrentDictionary`, which permits safe access from ants acting concurrently/in parallel.

To handle the problem of object-creation and garbage collection for pheromones, this application uses three strategies. The first is to not create multiple pheromone-instances for the same position and increase the value of the existing pheromone instead. The second consists of the restriction of positioning of resources to integer coordinates (whereas ants have to be positioned by fractional values to achieve smooth motion with all required degrees of freedom). The third strategy is using an object-pool for pheromones. Here, we create a lot of Pheromone-objects before starting the simulation-loop. When the application has to "create" an instance, it retrieves one from the pool, modifies its properties, uses it and finally releases it back into the pool when the pheromone is fully evaporated. While this increases the bootstrapping-time and the memory-usage of the generation 2 heap, it significantly reduces the amount of data to be garbage-collected each cycle.

To optimize the coloration of pheromones, we introduce graduation of the color-values and memoize the `SolidColorBrush`-objects created. For ants, it is sufficient to change their location and orientation instead of re-creating them every frame.

## Design Decisions

The application-code is divided into definitions (interfaces) and implementations, both logically and in its directory-structure. Every functional component is defined by an interface, simplifying potential extension with e.g. more kinds of resources or agents. For simplicity, the application does not use declarative configuration and does not include runtime-configuration of simulation-parameters via user controls, instead relying on class constants. These features may be added at a later point.

## Limitations & Known Issues

The application is deliberately coded from scratch with minimal and simple tools (Wpf and its primitives) in order to provide a demonstration of how the problem-domain and solution-implementation can be approached. This naturally entails that  significant potential benefits to performance and conciseness from using dedicated libraries are not realized.

Since ants only get created and added to the canvas once while pheromones get removed and added all the time, the latter have a higher default z-index. Specifying the z-index of ants (or pheromones) manually leads to a severe drop in performance due to the engine having to re-calculate the rendering-order for so many resources every frame. Thus, the application does not set proper z-levels, which means that the triangles representing ants will be obscured by intersecting pheromone-representations.

We're abusing Wpf a bit here, and even though the only resource where modifications may be attempted from multiple agents in parallel is managed via a `ConcurrentDictionary` through the `SimulationArena` itself and everything that is not UI is taken off the main thread, the application sometimes becomes irresponsive and requires restart. This issue is under ongoing investigation.

Currently, the `SimulationArena` is generic in the the type of `State` of its `Agent`s. In languages that don't require concretizing an interface provided as a generic type parameter and/or those with support for union-types, this would not hinder extending the application to support agents with different types of states in the same simulation. Since C# does not meet these criteria, this aspect of the application has to be changed to achieve this goal. In the future, this aspect will be rewritten to facilitate extending the functionality in the manner specified above.

Additionally, this solution deliberately avoids using third-party libraries like `MediatR` (which can reduce boilerplate and promote better patterns) in order to demonstrate an approach from first principles.

There is also further opportunity for improving the codebase in terms of modularity, conciseness, and adherence to best practices. The additional effort is justified and appropriate when building production-grade or enterprise-level software, but not necessarily for a project of this scope. However, improved adherence to such principles would help demonstrate these best-practices, so corresponding modifications may be made at a later date.
