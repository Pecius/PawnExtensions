# Pawn Extensions
<p align="center">
	<a href="https://rimworldgame.com">
		<img src="https://img.shields.io/badge/RimWorld-1.4-blue.svg" />
	</a>
	<a href="https://github.com/pardeike/Harmony">
		<img src="https://img.shields.io/badge/Harmony-2.0.X-blue.svg" />
	</a>
</p>

## Current features
- Pawn sounds with customizable randomized cooldown triggered by:
  - aiming
  - death
  - draft
  - drafted idle
  - pain
- allows disabling:
 	- certain jobs
	- diseases
	- pain
	- passions
	- needs display
	- romance
- pawn links - bonuses in form of a hediff for pawns in range
- applying hediffs depending on the state of a body part
- transforming stat value depending on the state of a body part
- spawning a pawn from an object via menu option (so you can "craft" and then "activate" the pawn)
- and more in the future...

## This assembly isn't fit for distribution on its own!
Sadly, as it's tricky to distribute assemblies like this with your mods with the reason being that the game will load only the first approached instance of it, regardless of version difference.

Which means that if your mod is loaded first, uses the assembly and you don't update it, all other mods will be forced to use the outdated version.

This limitation can be of course solved by making it a dependency (like uploading it on steam workshop).

I created it primary for personal mods in which I'll always ensure to supply the newest version.

But if you're interested in using it as well then let me know!

It's currently used in [Geth mod](https://steamcommunity.com/sharedfiles/filedetails/?id=1819987395).
