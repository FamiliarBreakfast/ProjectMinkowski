*Codename **Project Minkowski*** is a multiplayer space battler with mathematically correct special (and a little bit general) relativity. No longer are you an omniscient oracle, your information, and perception, of your current situation will depend on a finite speed of light that's the same no matter how you look at it.

***Project Minkowski*** is in a very early stage, and needs a lot of code cleanup (especially from someone who understands relativity better than me).

***Project Minkowski*** is licensed under the **AGPL-V3**.
  
  
  
**TODO:**
- Asteroids or some kind of object fields (in progress)
- Red/blue shift
- Radar pings *(in progress)*
- Collision (in progress)
- Weapons
- FTL?? There is engine support
- (long term) Online networking
- (long term) Partial general relativity

COLLISION:
- every object is defined by a list of points
- these points make up both its physical appearance and its collision polygon
- each object has one worldline
- but collision takes into account its polygonal shape
- almost as if it were a 3d worldtube
- this can all happen once globally, rather than per frameofreference, since causality is preserved
- this is good since were already struggling performance wise just from the basic lorentz transformations
- objects can still implement custom systems in the draw function in order to render more complex geometry than is actually needed for collision
- but the outer 'edge'/hull must visually match otherwise collision will appear broken

OPTIMIZATIONS:
- collision:
- - a simple entity definable radius to begin checking collision
- - basically a bounding box
- - or square?
- 