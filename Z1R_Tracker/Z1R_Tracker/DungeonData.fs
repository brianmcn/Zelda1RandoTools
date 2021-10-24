﻿module DungeonData

let factoids = [|
    "A room with a gleeok cannot have a push block."
    "Zelda's shutter will always be the only shutter door in a room. If you see 2+ shutter doors, none of them are Zelda's. Exception: if Zelda is adjacent to Gannon, Ganon's room may have multiple shutter doors."
    "If you pirouette a dungeon entrance (walk out, walk right back in), you will unlock or bomb-hole a possible doorway on the wall opposite the entrance."
    "If you screen transition (e.g. from East to West) on the Overworld before entering a dungeon, you may unlock a doorway or uncover a free bomb-hole on the wall you came from (e.g. East)."
//        "(how does it work for up-a-ing from a room where you opened that side (via bomb (shutter?))"
    "If you have 0 keys, and a room has both a shutter and a locked door, and you press against the locked door the moment the shutter opens, you can go through the door without a key, unlocking it (khananakey)."
    "If a room has a shutter door, a push block cannot reveal a new staircase (just will open the shutter if pushable)."
    "Each seed has its own three fixed small/medium/large secret values, where the possible ranges are 1-20/25-40/50-150 rupees."
    "When playing 'swordless', the only way to defeat a gleeok is with a wand, and the only way to defeat wizzrobes is with explosions (from bombs or boomstick)."
    "Dungeon room 'five pairs' only drops in dungeon 1 in Shapes(1Q)."
    "Sometimes buying 2 blue potions is cheaper than buying a red one.  Buying a blue when you have a blue turns it red."
    "If a dungeon is on screen A15, recorder-ing to that dungeon will cause the whirlwind drop you at B15, as there's not enough space on A15 to land."
    "If you clear a dungeon room containing traps or bubbles, it will stay clear forever until you leave the dungeon (as traps & bubbles are considered enemies)."
    "Up-A-continue during fairy fountain refill returns to the starting screen with full health (but, Up-A-continue during the refill will NOT remove a red-bubble-curse)."
    "Bomb upgrade man never has a bombable north wall (nor do dungeon NPCs who give hints)."
    "If using 'Level 9 Entry: Random' flag, if the old man at the door tells you some nonsense, that means you need all 8 triforces to get in. Otherwise he will say 'only those with X triforces' or 'only those with candle' etc."
    "In 1QL5, the floor drop spawn for 'tee' room is on the island; in L4, it's bottom right of the tee room; in most other dungeons, it's top right of the tee room."
    "The dungeon room floor drop near the East door is unique to 1QL1."
    "For the Any Roads, from your current entrance, the 3 staircases go +1/+2/+3 steps along the 1-2-3-4-1-2-3-4-... chain."
    "If an overworld screen has 6 monsters, and you kill 2 and leave the screen, only 4 will spawn when you next return.  Overworld monsters can fully respawn only after a screen is fully cleared, or after a save."
    "If a dungeon room has 6 monsters, and you kill 2 and leave the room, only 4 will spawn when you next return.  Dungeon monsters can fully respawn only after a room is fully cleared, or after the player leaves the dungeon."
////////////////////////////////
    "In Z-Tracker, hovering a triforce icon at the top of the tracker will display its location on the overworld map, if that location is known or has been hinted."
    "In Z-Tracker, each overworld shop can be marked with two different items for sale.  Left click a shop tile to choose a second item to add to the shop."
    "In Z-Tracker, hovering some parts of the 'Item Progress' bar will highlight spots on the overworld map, e.g. hovering the candle highlights all remaining burnable trees."
    "In Z-Tracker, hovering the 'open cave icon' near the upper right of the tracker will highlight unmarked open caves, which may be useful if you're still looking for the wooden sword."
    "In Z-Tracker, hovering the 'bomb' icon in the upper right of the tracker will highlight any Bomb Shop locations you have marked on the map."
    "In Z-Tracker, hovering the Armos icon in the top middle of the tracker will highlight any Armos spots on the overworld map you have not yet marked."
    "In Z-Tracker, hovering the 'Spot Summary' text in the upper right will display all the remaining locations you have yet to find, based on your existing overworld map marks."
    "In Z-Tracker, when you locate a dungeon entrance on the overworld map, its triforce numeral lights up white on the top of the tracker, so that it is easy to see which dungeons you have/haven't yet found."
    "In Z-Tracker, you can 'paint out' a bunch of rooms in a dungeon by holding down the mouse button and dragging over rooms.  The mouse left-button paints 'completed' rooms, and the right-button paints 'uncompleted' rooms."
    "In Z-Tracker, left-clicking a dungeon door makes it a green door (can go), right-clicking makes it a red wall (can't go), and middle-clicking makes it a yellow door (other, perhaps a locked door)."
    "In Z-Tracker, left-clicking a dungeon's entrance room cycles the entrance arrow (SWNE), and left-clicking other rooms toggles them completed (darker) or uncompleted (brighter)."
    "In Z-Tracker, if you leave a dungeon because you are blocked (e.g. needing a Ladder or a Key), you can add a mark in the BLOCKERS section for that dungeon number, to remind you why you left."
    "In Z-Tracker, the Timeline at the bottom of the app is automatically tracking per-minute 'splits' of when you got each triforce piece (as well as every other key item)."
    "In Z-Tracker, you can edit HotKeys.txt in the application folder to set up various keyboard shortcuts."
    "Dr. Brian Lorgon111 made Z-Tracker.  Send him some love on twitch (lorgon) or on twitter (@lorgon111)."
    |]

let l1q1 =
    [|
        "........"
        "........"
        "..XX...."
        "...X.XX."
        ".XXXXX.."
        "..XXX..."
        "...X...."
        "..XXX..."
    |]

let l2q1 =
    [|
        "...XX..."
        "....XX.."
        "....XX.."
        "....XX.."
        "....XX.."
        "....XX.."
        "..XXXX.."
        "...XX..."
    |]

let l3q1 =
    [|
        "........"
        "........"
        "..XX...."
        "...X.X.."
        ".XXXXX.."
        ".XXXXX.."
        ".X.X...."
        "...XX..."
    |]

let l4q1 =
    [|
        "..XXXX.."
        "..XXXX.."
        "..XX...."
        "..XXX..."
        "..X....."
        "..XX...."
        "...XX..."
        "..XX...."
    |]

let l5q1 =
    [|
        "...XX..."
        "..XXXX.."
        "..XXXX.."
        "..X..X.."
        "....XX.."
        "...XXX.."
        "..XXXX.."
        "....XX.."
    |]

let l6q1 =
    [|
        "..XXXX.."
        ".XXXXXX."
        ".XX..XX."
        ".XXX.X.."
        ".X......"
        ".X......"
        ".X.X...."
        ".XXX...."
    |]

let l7q1 =
    [|
        ".XXXXXX."
        ".XXXXX.."
        ".XXXX..."
        ".XXX...."
        ".XX....."
        ".XXXX..."
        ".XXXXXX."
        ".XXX...."
    |]

let l8q1 =
    [|
        "....X..."
        "...XXX.."
        "..XXX..."
        ".XXXXX.."
        ".XXXX..."
        "..XXXX.."
        "....X..."
        "..XXXX.."
    |]

let l9q1 =
    [|
        ".XXXXXXX"
        "XXXXXXXX"
        "XXXXXXXX"
        "XXXXXXXX"
        "XXXXXXXX"
        "XXXXXXXX"
        ".XXXXXX."
        ".X.XX.X."
    |]

let firstQuest = [| l1q1; l2q1; l3q1; l4q1; l5q1; l6q1; l7q1; l8q1; l9q1 |]

let l1q2 =
    [|
        "...XX..."
        "...XX..."
        "...X...."
        "...XX..."
        "...XX..."
        "...X...."
        "...XX..."
        "...XX..."
    |]

let l2q2 =
    [|
        "...X...."
        "..XXX..."
        "..XXX..."
        "..XXX..."
        "..XXX..."
        "..XXX..."
        "..X.X..."
        "..X.X..."
    |]

let l3q2 =
    [|
        "........"
        ".....X.."
        "X....X.."
        "X....X.."
        ".....X.."
        ".....X.."
        ".....XX."
        ".....XX."
    |]

let l4q2 =
    [|
        "..XXX..."
        "..XXXX.."
        "..XXXX.."
        "..XXXX.."
        "..XXXX.."
        "..XXXX.."
        "..XXXX.."
        "..XXX..."
    |]

let l5q2 =
    [|
        "..XXX..."
        "..XXX..."
        "....X..."
        "...XX..."
        "..XX...."
        "..X....."
        "..XXX..."
        "..XXX..."
    |]

let l6q2 =
    [|
        "....XXX."
        "...XX.X."
        "...XX.X."
        "...XX.X."
        "..XXX..."
        ".XXXX..."
        "...XX..."
        "....X..."
    |]

let l7q2 =
    [|
        "........"
        "..XXXXX."
        "..X...X."
        "..XXX.X."
        "..XXX.X."
        "..XXX.X."
        "......X."
        "XXXXXXX."
    |]

let l8q2 =
    [|
        "XXXXXXXX"
        "XX.....X"
        "XX...X.X"
        "XX...X.X"
        "XX...X.X"
        "XX...X.X"
        "XXXXXX.X"
        ".......X"
    |]

let l9q2 =
    [|
        "XX....XX"
        "XXXXXXXX"
        "..XXXX.."
        ".XXXXXX."
        "XXXXXXXX"
        "XXXXXXXX"
        ".XXXXXX."
        "...XX..."
    |]

let secondQuest = [| l1q2; l2q2; l3q2; l4q2; l5q2; l6q2; l7q2; l8q2; l9q2 |]
