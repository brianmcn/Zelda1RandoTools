module DungeonData

module Factoids =
    let noviceTips = [|
        // novice
        "Damage table: wood sword=1; white sword=2; magic sword=4; wand(melee or beam)=2; wood arrow=2; silver arrow=4; fire=1; bomb explosion=4; boomerang=0 (some enemies have 0 HP and can be killed by boomerang)"
        "If an overworld screen has 6 monsters, and you kill 2 and leave the screen, only 4 will spawn when you next return.  Overworld monsters can fully respawn only after a screen is fully cleared, or after a save."
        "If a dungeon room has 6 monsters, and you kill 2 and leave the room, only 4 will spawn when you next return.  Dungeon monsters can fully respawn only after a room is fully cleared, or after the player leaves the dungeon."
        "Each seed has its own specific value for small/medium/large secrets, where the possible ranges for any seed are 1-20/25-40/50-150 rupees."
        "Sometimes buying 2 blue potions is cheaper than buying a red one.  Buying a blue potion when you have a blue potion turns it into a red potion."
        "For the Any Roads, from your current entrance, the 3 staircases go +1/+2/+3 steps along the 1-2-3-4-1-2-3-4-... chain."
        "To stun a Dodongo with a single bomb, walk halfway between two tiles when placing the bomb; Dodongo can only eat bombs placed evenly on the tile grid.  After being stunned, Dodongo can be killed with a sword for a bomb pack drop."
        "When there is a standing item drop on the floor of a dungeon room when you first walk in, there cannot be another prize drop for killing all monsters in the room."
        "If a dungeon room has a pushblock, it will always be the leftmost block in the center row."
        "Overworld push-blocks (such as rocks that can be pushed with power bracelet) must be pushed up/down from directly below/above the block.  In contrast, dungeon pushblocks are pushable in all 4 directions."
        "In most dungeons, there is one floor drop item, and the remaining key item(s) are found in basements.  Exceptions: L9 has no floor drops; L1 and L2 have two floor drops in first-quest-dungeons, whereas L1 and L3 have two floor drops in second-quest-dungeons."
        "Generally/roughly, the higher the dungeon number, the larger the dungeon is likely to be, and the less likely a given wall will be bombable."
        "If you up-a out of a door repair charge room before the NPC finishes speaking and starts taking your money, you can avoid the charge."
        |]
    let intermediateTips = [|
        // intermediate
        "A room with a gleeok cannot have a push block."
        "If you defeat 1 of 2 Lanmolas, or 1 of 3 small Digdoggers, and then leave the room, the monsters will all be gone when you return a moment later."
        "A single dodongo in a room is a boss (will not respawn if killed and room is exited).  Three dodongos in a room is an enemy group (that can respawn)."
        "If you pirouette a dungeon entrance (walk out, walk right back in), you will unlock or bomb-hole a possible doorway on the wall opposite the entrance."
        "If you screen transition (e.g. from East to West) on the Overworld before entering a dungeon, you may unlock a doorway or uncover a free bomb-hole on the wall you came from (e.g. East)."
    //        "(how does it work for up-a-ing from a room where you opened that side (via bomb (shutter?))"
        "If a room has a shutter door, a push block cannot reveal a new staircase (just will open the shutter if pushable)."
        "A dungeon room with a push block shutter will never drop an item."
        "Consider the room that has the triforce in vanilla; in Shapes dungeons, when that room does not have a standing item upon entry, it will always drop the Map after killing all enemies."
        "When playing 'swordless', the only way to defeat a gleeok is with a wand, and the only way to defeat wizzrobes is with explosions (from bombs or boomstick)."
        "If a dungeon is on screen A15, recorder-ing to that dungeon will cause the whirlwind drop you at B15, as there's not space on A15 to land."
        "If you clear a dungeon room containing traps or bubbles, it will stay clear forever until you leave the dungeon (as traps & bubbles are considered enemies)."
        "Up-A-continue/save during a fairy fountain refill returns you to your starting screen on the overworld with full health."
        "Bomb upgrade man never has a bombable north wall (nor do rooms containing dungeon NPCs who give hints)."
        "Standing just halfway out the east door of a dungeon room makes the player immune to some attacks; this 'safety door' strategy can be effective to shoot beam swords at gleeoks or lanmola, for example."
        |]
    let advancedTips = [|
        // advanced
        "Zelda's shutter will always be the only shutter door in a room. If you see 2+ shutter doors, none of them are Zelda's. Exceptions: If Zelda is adjacent to Gannon, Gannon's room may have multiple shutter doors. If Gannon is not required, Zelda can be anywhere, including rooms without shutters."
        "If you have 0 keys, and a room has both a shutter and a locked door, and you press against the locked door the moment the shutter opens, you can go through the door without a key, unlocking it (khananakey)."
        "If using 'Level 9 Entry: Random' flag, if the old man at the door tells you some nonsense, that means you need all 8 triforces to get in. Otherwise he will say 'only those with X triforces' or 'only those with candle' etc."
        "In Shapes (1Q) dungeons, after clearing monsters, dungeon room 'five pairs' can only drop in dungeon 1, and dungeon room 'maze' can only drop in dungeon 4."
        "In 1QL5, the floor drop spawn for 'tee' room is on the island; in L4, it's bottom right of the tee room; in most other dungeons, it's top right of the tee room."
        "The dungeon room floor drop near the East door is unique to 1QL1."
        "These dungeon room never drop after clearing monsters: '3 full rows', 'Zelda room', 'circle wall', 'single block', and '<spike trap angles>'."
        "If the 'Add 2nd Quest Doors' randomizer flag is used, walk-through-walls will never appear in levels 1, 3, or 7, but walk-through-walls may appear in any other dungeon."
        |]
    let zTrackerTips = [|
        "In Z-Tracker, mouse-hovering the question mark (at the top, just left of the timer) will show off all the other places you can mouse-hover to get various bits of useful information."
        "In Z-Tracker, each overworld shop can be marked with two different items for sale.  Left click a shop tile to choose a second item to add to the shop."
        "In Z-Tracker, when you locate a dungeon entrance on the overworld map, its triforce numeral lights up white on the top of the tracker, so that it is easy to see which dungeons you have/haven't yet found."
        "In Z-Tracker, you can 'paint out' a bunch of rooms in a dungeon by holding down the mouse button and dragging over multiple rooms.  The mouse left-button paints 'completed' rooms, and the right-button paints 'uncompleted' rooms."
        "In Z-Tracker, left-clicking a dungeon door makes it a green door (can go), right-clicking makes it a red wall (can't go), and middle-clicking makes it a yellow door (other, perhaps a locked door)."
        "In Z-Tracker, left-clicking a dungeon's entrance room cycles the entrance arrow (SWNE), and left-clicking most other rooms toggles them completed (darker) or uncompleted (brighter)."
        "In Z-Tracker, if you leave a dungeon because you are blocked (e.g. needing a Ladder or a Key), you can add a mark in the BLOCKERS section for that dungeon number, to remind you why you left."
        "In Z-Tracker, you can edit the key items and triforce you got from a dungeon, using the box right next to the dungeon map, rather than having to mouse all the way back up to the top of the app."
        "In Z-Tracker, the Timeline at the bottom of the app is automatically tracking per-minute 'splits' of when you got each triforce piece (as well as every other key item)."
        "In Z-Tracker, you can edit HotKeys.txt in the application folder to set up various keyboard shortcuts.  You can create a Notes.txt file to pre-populate the Notes text in the app, as well."
        "In Z-Tracker, the overworld map highlights various tiles with a green rectangle if you can uncover a location there, a yellow rectangle if the screen may or may not yield a location and you can uncover it, or a red rectangle if you don't have the item to uncover a location there."
        "Dr. Brian Lorgon111 made Z-Tracker.  Send him some love on twitch (lorgon) or on twitter (@lorgon111)."
        |]
    let allTips = [|
        yield! noviceTips
        yield! intermediateTips
        yield! advancedTips
        yield! zTrackerTips
        |]

//////////////////////////////////////////////

let oldManHintCounts1Q = [| 1; 1; 1; 1; 3; 2; 2; 2; 3 |]   // 1 hungry goriya that can be shuffled anywhere
let oldManHintCounts2Q = [| 0; 0; 0; 2; 0; 1; 0; 1; 2 |]   // correct?   also 2 hungry goriya that can be shuffled anywhere?

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
