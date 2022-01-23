# What's new in this version

A summary of the features/fixes in the various releases of Z-Tracker

## <a id="v1.1"></a>Version 1.1

### Starting Items and  Extra Drops

You can now track items obtained outside the usual places, see [the documentation](use.md#main-sioed) for details.

### Dungeon Summary Tab

The dungeon tracker now has a tab showing the tracking of the first 8 dungeons on a single screen:

![Dungeon summary tab](screenshots/dungeon-summary-tab.png)

### Maybe-blockers

There's now an option to distinguish "definitely" and "maybe" in the BLOCKERS sections.  See [the documentation](use.md#main-blockers) for details.

### BOARD instead of LEVEL

In the options menu, you can now choose BOARD instead of LEVEL for column headers, just like in the randomizer.

![BOARD](screenshots/board-versus-level.png)

### Improvements to ''Hidden Dungeon Numbers' support

If the dungeon number is known, both the number and letter appear on the overworld map.

If the dungeon number is known, and that dungeon only has two items rather than three, the third item box is automatically marked off with a 'ghostbusters' (circle/slash) icon.

![HDN numbers](screenshots/hidden-dungeon-numbers-known.png)

### Gannon and Zelda rooms in dungeon 9

New room options for the final dungeon (that replace Bomb Upgrade and Meat Block in L9)

![Gannon & Zelda](screenshots/gannon-zelda.png)

### Minimap overlay and Near-mouse HUD

The minimap overlay is super-cool.  The Near-mouse HUD is an experimental feature.  See [the documentation](use.md#main-buttons) for details, and consider trying out these optional features.

### Highlight empty item boxes

In the item box popup, whereas middle-clicking an item marks that item as intentionally skipped, now middle-clicking the empty box toggles the
box outline between red and white.  You can use this white box outline mark however you like, e.g. to highlight a high-priority dungeon item 
to find ("I never found the stair item in 3, remember to come back there soon").  

By default, the white sword, ladder, and armos item boxes start out with this white highlight.

### Mouse hover explainer

There are a lot of places in the app where hovering the mouse over something can display useful information.  Now you can learn about all of
these mouse-hover targets inside the app, by hovering the question mark left of the game timer at the top of the app.  When you move the mouse 
over the question mark, the diagram below appears:

![Mouse Hover Explainer](screenshots/mouse-hover-explainer.png)

### Other minor stuff

A number of minor improvements:
 - there's now a legend under the overworld magnifier
 - the current dungeon tab highlights corresponding blockers area
 - dungeon doors/rooms give mouseover feedback, to make it easier to click the intended target
 - there's an option to automatically infer some dungeon door marks (see [the documentation](use.md#main-dt) for Doors)
 - 'Show HotKeys' window remembers its size/location
 - added Z-Tracker logo next to the kitty
 - big icons checkbox in dungeons is persisted across sessions
 - The file Notes.txt can be used as the default Notes text at startup

 And some small fixes:
 - fix GRAB getting triggered when clicking dungeon tab then pressing Enter
 - fix 2Q overworld not showing fairy icon at 1Q5
 - fix coordinate display in Mirror Overworld to match z1r spoiler log format
 - fix HFQ/HSQ buttons to be more useful and to display less ugly


## Version 1.0

Original Release (see [full documentation for v1.0](https://github.com/brianmcn/Zelda1RandoTools/blob/v1.0/doc/TOC.md))