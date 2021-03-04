## SomethingNeedDoing

This plugin gives you control of a little Orc peon that does things for you. 
Specifically, crafting. 
It's a better way of storing and running all your different macros.

![](https://github.com/daemitus/SomethingNeedDoing/raw/master/res/game.png)

## Features
- Folders, drag/drop re-ordering, and renaming of macros.
- `<wait.#>` Default wait usage
- `<wait.#.#>` - Optional fractional wait format 
- `/wait #` - Default wait command usage
- `/wait #.#` - Optional fractional wait command format
- `/loop` - Repeat the current macro
- `/loop #` - Repeat the current macro N times
- `/require "<name>"` - Require an effect to be present, like `medicated` or `well fed`
- `/waitaddon "<name>"` - Wait until a certain UI addon is present. No more starting a macro before the craft has actually been started.
- `/runmacro "<name>"` - Run a macro within a macro.
- Intelligent skill queueing - The next crafting skill won't be sent until at minimum, a response from the game server is received. 
This can be helpful in laggy situations. 
You'll still need a `<wait.#>` modifier however. 
The response is generally quicker than the animation.

Note: Unless otherwise provided by the game or another plugin, these do not work outside the craft interface.

## In-game usage
* Type `/craft` to pull up the GUI.
