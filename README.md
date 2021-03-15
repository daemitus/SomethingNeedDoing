## SomethingNeedDoing

This plugin gives you control of a little Orc peon that does things for you. 
Specifically, crafting. 
It's a better way of storing and running all your different macros.

![](https://github.com/daemitus/SomethingNeedDoing/raw/master/res/game.png)

## Features
- Folders, drag/drop re-ordering, and renaming of macros.
- `<wait.#>` Default wait usage.
- `<wait.#.#>` - Optional fractional wait format.
- `<maxwait.#>` - For use with `/require` and `/waitaddon` to override the default timeout value.
- `<maxwait.#.#>` - Optional fractional wait format.
- `/wait #` - Default wait command usage.
- `/wait #.#` - Optional fractional wait command format.
- `/loop` - Repeat the current macro forever.
- `/loop #` - Repeat the current macro N times.
- `/require "<name>"` - Require an effect to be present, like `medicated` or `well fed`. By default, will timeout after 1 second.
- `/waitaddon "<name>"` - Wait until a certain UI addon is present. No more starting a macro before the craft has actually been started. By default, will timeout after 5 seconds.
- `/runmacro "<name>"` - Run a macro within a macro.
- Intelligent skill queueing - The next crafting skill won't be sent unless a response from the game server is received. This can be helpful in laggy situations. You'll still need a `<wait.#>` modifier however. The server response is generally quicker than the animation.
- `/ac "skill-name" <unsafe>` - Bypass intelligent skill queueing for whatever reason.

Note: Unless otherwise provided by the game or another plugin, these do not work outside the craft interface.

## In-game usage
* Type `/pcraft` to pull up the GUI.
