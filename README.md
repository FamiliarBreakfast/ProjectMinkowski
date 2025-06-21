# Codename *Project Minkowski*
Is a multiplayer space battler with mathematically correct special (and a little bit general) relativity. 
No longer are you an omniscient oracle, your information, and perception, of your current situation will depend on a finite speed of light that's the same no matter how you look at it.

> [!NOTE]
> ***Project Minkowski*** is in a very early stage, and needs a lot of code cleanup (especially from someone who understands relativity better than me).

> [!NOTE]
>***Project Minkowski*** is licensed under the **AGPL-V3**, except where otherwise noted.[^1][^2]

## CURRENT FEATURES AND TODO:
### Special Relativity (Complete)

todo: quick intro to sr\
todo: explanation of current minkwoski space system\
tldw: worldlines worldcones SPACE
### General Relativity (In Progress)

- During event sightline checks, take path topology into account
- - First, iterate over and distance check all masses
- - If any found, do full raytrace
- - If raytrace intersects curved space, roll forward/back on the worldline
- - - Analytical point sampling preferred over quantized scalar field
- - - Will break beyond singularity event horizons
- - - - Must decide what to do here
- [Massive objects (planets and stars)](#world)
- - Automatic space curvature
### World

- Asteroids or some kind of non-massive object fields (needs work, acceptable)
- - Currently, randomly generated grid offsets
- - - Prefer gridless approach
- - - Prefer less random approach
- - - - Noisemap?
### Effects

- Animation and sound system (in progress)
- - Animation must be tied to worldline
- Unclear how sound should work in local splitscreen
- FM synthesis (partial)
- - Better synthesis system
- - Sound design such that sound effects modulate eachother
- - - I have no experience with FM synthesis?
### Gameplay

- Doppler effect (hiatus)
- - How implement without obscuring player colors? (if possible?)
- More weapons (in progress)
- Better movement (in progress)
- - Needs tightening
- - - Increase speed of light?
- - Powerups/boosts
- - Possible FTL boost powerup for short periods of time
- Refactor entity/collision system for better performance
- - Implement chunking?
- - Or make worldgen deterministic
- Predictive aim reticule
- FTL?? There is engine support (in the sense it doesn't immediately crash)
- - Could cause paradoxes?
- - - Might not be a problem
### Bugs

- None at the moment
### Long Term

- Multithreading


- Online networking
- - Teams and Chat (with signal delay)


[^1]: **Motomangucode Font** is licensed under the **Creative Commons Attribution-NoDerivatives 4.0 International License**\
[^2]: **Space Mono Font** is licensed under the **Open Font License (OFL)**