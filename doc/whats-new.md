# What's new in each version

A summary of the features/fixes in the various releases of Z-Tracker

 - [Version 1.2](#v1.2) (under development)
 - [Version 1.1](#v1.1) (released 2022-02-06)
 - [Version 1.0](#v1.0) (released 2021-12-11)

---

## <a id="v1.2"></a>Version 1.2

### Save and load

You can now save the state of the tracker to a file, and load it again (say, tomorrow) to pick up a seed where you left off.

Click the 'Save' button in the running tracker to save all of the current tracker state; it will automatically be saved to a file with the current date and time in the filename.

![Save button](screenshots/save-button.png)

On the startup screen, choose the 'Start: from a previously saved state' option to load up a prior save.

![Load button](screenshots/load-button.png)

There is also an auto-save which saves the full tracker state (to a file named 'zt-save-zz-autosave.json') approximately every minute.  This might be useful if you accidentally 
close the tracker or have a crash in the middle of a seed.

There's also an option in the Options Menu to 'Save on completion', to automatically make a save when you click Zelda upon completion of the seed.  This can be useful to folks who
like to keep records of all their Timeline splits for each game played.  (Note that the save files are large, so ensure you have plenty of disk space if you use this option to 
accumulate a lot of saves.)

### Dungeon numerals stay visible until first room is marked

To make it less likely to accidentally map a dungeon in the wrong tab, the dungeon numeral stays visible over the whole tab until the first room is marked.

![Dungeon numeral](screenshots/dungeon-numeral.png)

### More types of dungeon rooms and monsters; Simpler UI for choosing rooms, Monster Detail, and Floor Drop Detail

The dungeon room shape "Lava Moat" has been added, and now the existing room types "Bomb Upgrade", "Hungry Goriya", "Gannon", "Zelda", and "Off the map" are now
always available in the dungeon room selection popup menu.  

![Dungeon room types](screenshots/dungeon-room-types.png)

Many more monster types can be optionally marked in a room:

![Dungeon room types](screenshots/dungeon-room-monster-detail.png)

For details, see the [main documentation](use.md#main-drpu) on this topic.

### Special NPC room highlights

You can make Bomb-Upgrade rooms and NPC-with-Hint rooms stand out visually, and surface the existence of these rooms and Hungry-Goriya-Bait-Block rooms in the tab headers:

![Special NPC rooms](screenshots/special-npc-room-color.png)

![Bait block rooms](screenshots/bait-block-rooms.png)

See [Special NPC rooms](use.md#main-dungeon-special-rooms) for details.

### Vanilla dungeon map improvements

The user interface and colors for the vanilla dungeon maps overlay has been improved.

![Vanilla map selection](screenshots/vanilla-dungeon-compatibility.png)

### Highlight potential dungeon continuations

This is a new feature to help locate paths to the rest of the rooms in a dungeon, see [here](use.md#main-hpdc) for details.

![Dungeon potential highlighted](screenshots/dungeon-potential-highlighted.png)

### Circle arbitrary overworld tiles

You can now circle arbitrary overworld spots (perhaps to remind you of a screen with easy-to-kill bomb-droppers).  You can even add some labels and colors to these marks
(you might want this if using Z-Tracker to play other randomizer variants, such as z1m1, for example).  See [the full documentation](use.md#circle-overworld) for details.

![Overworld circle](screenshots/overworld-middle-click-circle.png)

### LostWoods/LostHills magnifier improvements

The Lost Woods tile and Lost Hills tile now show the routes to traverse these 'maze' tiles on the overworld magnifier.  This is to help both Zelda 1 novice players, as well as
veterans who may get confused/disoriented when playing using the rando flag "Mirror Overworld".

![Lost woods magnifier](screenshots/lost-woods-magnifier.png)

### Timeline improvements

Many changes and improvements:

 - The timeline now updates instantly as you get items, rather than only updating once per minute.  The splits for each item/triforce are stored at one-second granularity and can
be seen by mouse-hovering a particular item in the timeline.  

 - In 'Hidden Dungeon Numbers', lettered triforces will now change to numbered ones on the timeline after the numbers are known.  
 
 - Items marked as 'skipped' (by middle-clicking them) now show up in the timeline ('X'-ed out, as in the top-tracker).

 - There are now more time labels on the timeline, making it easier to read.

 - The timeline now shows a graph of 'overworld progress over time' in its background, showing the trend of the 'remaining unmarked overworld spots' counting towards 0 at the top of the graph.

 - When you click Zelda to finish the seed, your final time and final number of remaining unmarked overworld spots appear on the timeline, and a screenshot of the timeline is taken and 
saved as "most-recent-completion-timeline.png" in your Z-Tracker directory, which you might share in a race-spoilers discussion.

![New Timeline](screenshots/most-recent-completion-timeline.png)

### More application sizes

On the startup screen, the top banner used to allow changing the application to run either at full size (default) or at 2/3 size (for some laptops/tablets where Z-Tracker couldn't fit on the screen).  
Now there is third option of 5/6 size; furthermore, by manually editting the json settings file, you could try other arbitrary sizes as well (though they may not look very good due to pixel scaling issues).

### Changed-tile animations

You might sometimes accidentally click/hotkey an overworld tile or dungeon room that you didn't mean to change, and you might not notice/see which tile you just changed.  There is now an option to 
"Animate tile changes" in the Options Menu which causes a brief highlight animation over the most-recently-changed overworld tile or dungeon room.

### HotKey improvements

Now most popup menus will remind you of HotKeys you have mapped in the descriptions of the corresponding items.  This is useful for the scenario where you know that you mapped a HotKey for
a certain item, but you forget what you set it to.  

![HotKey reminder](screenshots/hotkey-reminder.png)

Also, you can now map 'global' hotkeys to toggle items in the upper right of the tracker (e.g. wood arrow, bomb, zelda), or to switch among the 10 dungeon tabs.

Plus, you can now hotkey the 'Take Any' (heart) and 'Take This' (wood sword) cave options.

Furthermore, you can now bind any keyboard keys (not just 0-9/A-Z).  See [HotKeys](extras.md#hotkeys) for details.

### A number of minor fixes and changes

 - fixed: mini-mini-map (the dungeon hover-blue-bars) now shows text like "BOARD-5" properly, no longer obscures dungeon map
 - fixed: dungeon summary tab no longer sometime shows stale/missing info
 - fixed: fewer reminders to "consider dungeon X" when you're already inside dungeon X right now
 - fixed: clicking FQ or SQ on the dungeon summary tab (in non-hidden-dungeon-numbers) toggles all dungeons consistently into that vanilla map display
 - fixed: issues with image crispness in Broadcast Window
 - you can now mark up to 3 Blockers per dungeon
 - hovering certain blockers (bait/key/bomb/bow&arrow) will highlight the corresponding marked shops on the overworld map
 - hovering to highlight shops can animate tiles to make them easier to see (OptionsMenu: "Animate Shop Highlights", on by default)
 - Link can now route to blocker-shops (e.g. bait/key blocker), or to potion shops (Link's potion shop icon appears over take-any hearts)
 - made dungeon LEVEL/BOARD header use Zelda font
 - there's now a 5th [dungeon door state](use.md#main-dt-doors) (purple) which you can optionally use to distinguish shutters from key-locked-doors, or whatnot
 - dungeons show an [old man count](use.md#main-dungeon-old-man-count), tracking the number of NPC rooms you have marked versus the expect number (e.g. "OM:2/3")
 - mouse hovering a dungeon room shows a helpful row-locator graphic in the corner, see [Row location assistance](use.md#main-dungeon-row-location) for details
 - if you mark any off-the-map rooms, mouse hovering the little blue bars in the corner of the dungeon tracker will also display the 'inverse map' (when painting 'holes' upon entering 9 with atlas)
 - when on the dungeon summary tab, all dungeon locations get a thick green highlight on the overworld map, to make it easy to see all dungeon locations at once
 - an empty dungeon item box display suggests whether it is a basement item or standing/floor drop ('Show basement info' in the Options Menu; 'on' by default)
 - speaking either "don't care" or "nothing" will mark off an overworld tile using [Speech Recognition](use.md#speech-recognition)
 - when you get a reminder to consider the magical sword, tracker now briefly highlights its location on the map
 - when you get the 8th triforce piece, reminder that dungeon 9 is unlocked now briefly highlights the location of dungeon 9
 - there's now reminders for Door Repair Count on the Options Menu
 - you can now choose to hide certain overworld icons you don't want to see (Options Menu\Overworld\More settings)
 - when you reset the timer, you immediately get put into the 'place the start spot location icon' popup to mark your start screen
 - new dungeon map drag-painting implementation, see [full documentation](use.md#main-dr-drag) for details
 - Show/Run Custom button now can 'RUN' URLs, which get launched in your default browser
 - you can have tracker snoop for window (e.g. emulator window) whose title contains seed & flags, and display/save that info
 - reconfigured top right area of tracker to improve Broadcast Window view while in dungeons
 - improved readability of 'Coords' display
 - recorder destination in map legend is now clickable, to cycle dungeon numbers 1-8, to optionally keep track of where you just recordered to

---

## <a id="v1.1"></a>Version 1.1

### Starting Items and Extra Drops

You can now track items obtained outside the usual places, see [the documentation](use.md#main-sioed) for details.

![Extras](screenshots/extras-example.png)

### Dungeon Summary Tab

The dungeon tracker now has a tab showing the tracking of the first 8 dungeons on a single screen:

![Dungeon summary tab](screenshots/dungeon-summary-tab.png)

### Maybe-blockers

There's now an option to distinguish "definitely" and "maybe" in the BLOCKERS sections.  See [the documentation](use.md#main-blockers) for details.

![Blockers](screenshots/blockers.png)

### BOARD instead of LEVEL

In the options menu, you can now choose BOARD instead of LEVEL for column headers, just like in the randomizer.

![BOARD](screenshots/board-versus-level.png)

### Improvements to ''Hidden Dungeon Numbers' support

If the dungeon number is known, both the number and letter appear on the overworld map.

Hotkeys: Pressing keyboard keys 1-8, either in the Number chooser, or when hovering a lettered triforce or its number button, will set the number for that dungeon.

If the dungeon number is known, and that dungeon only has two items rather than three, the third item box is automatically marked off with a 'ghostbusters' (circle/slash) icon.

![HDN numbers](screenshots/hidden-dungeon-numbers-known.png)

### Gannon and Zelda rooms in dungeon 9

New room options for the final dungeon (that replace Bomb Upgrade and Meat Block in L9)

![Gannon & Zelda](screenshots/gannon-zelda.png)

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
 - there's now an option for overworld-routing to show & utilize screen scrolling
 - the current dungeon tab highlights corresponding blockers area
 - dungeon doors/rooms give mouseover feedback, to make it easier to click the intended target
 - there's an option to automatically infer some dungeon door marks (see [the documentation](use.md#main-dt) for Doors)
 - main application window and broadcast window remember their locations
 - 'Show HotKeys' window remembers its size/location
 - a count of how many dungeon entrances you have already found appears at the top of the tracker
 - added Z-Tracker logo next to the kitty
 - big icons checkbox in dungeons is persisted across sessions
 - The file Notes.txt can be used as the default Notes text at startup
 - The rest of the Helpful hints (ones that don't lead to dungeon/sword) have been added to the Hint Decoder
 - you can now change between 'Default' and '2/3 size' main window in the application (on the startup screen)
 - when you change dungeon tabs, or first mark a room in a dungeon, the dungeon number appears briefly on the tab to remind you which tab you are on, to help prevent the error of mapping in the wrong tab
 - there's a new reminder if you are still missing the white sword cave when you locate the 9th dungeon
 - there's a new button 'Show/Run Custom' which can be used to script SHOWing some image windows or RUNning executables, see [the documentation](use.md#main-buttons) for more info
 - the currently running version number now appears on the startup screen

 And some small fixes:
 - fix GRAB getting triggered when clicking dungeon tab then pressing Enter
 - fix 2Q overworld not showing fairy icon at 1Q5
 - fix coordinate display in Mirror Overworld to match z1r spoiler log format
 - fix HFQ/HSQ buttons to be more useful and to display less ugly
 - fix windows/taskbar to now properly display Z-Tracker logo icon

---

## <a id="v1.0"></a>Version 1.0

Original Release (see [full documentation for v1.0](https://github.com/brianmcn/Zelda1RandoTools/blob/v1.0/doc/TOC.md))