# Startup Options

Contents of this document:

  - [Supported z1r flagsets](#startup-flagsets)
  - [Heart Shuffle option](#startup-hs)
  - [Hide Dungeon Numbers option](#startup-hdn)
  - [Start buttons (choose overworld quest)](#startup-coq)
  - [Options](#startup-o)

## <a id="startup-flagsets"></a> Supported z1r flagsets

Z-Tracker is designed to work with most of the myriad flags and options available in the Zelda 1 Randomizer.  A few common options need to be specified at startup, and 
a few of the more rarely used flags will result in a somewhat degraded experience.

**Flags** that need to be specified at startup time:

 - [**Heart Shuffle**](#startup-hs)
 - [**Hide Dungeon Numbers**](#startup-hdn)
 - [**Overworld Quest**](#startup-coq)

Each is described in more detail in the linked sections below.  Note that "**Mirror Overworld**" and "**Second Quest Dungeons**" are supported in the Options Menu, but those options 
can be changed at any time in the middle of the game, not just at startup.

**Flags** that are not directly supported, and may result in a minor degradation of the experience:

 - **Whistle To New Dungeons** is assumed to be checked and **Recorder to Unbeaten Dungeons** is assumed to be un-checked.  If your flags don't meet these conditions, then 
     - some aspects of [Routing](use.md#routing) will not work properly: consider turning off 'Draw Routes' in the [Options Menu](#startup-o) and avoid using [Link Routing](use.md#main-link)
     - the map legend and green dungeon icons will suggest the wrong things
 - **Change Sword Hearts** is assumed to be checked, but if it's not checked in your flagset, the only bad side-effect is that you may get e.g. a spurious reminder to "Consider the magical sword"
   when you have 10 hearts, even though you actually need 12 hearts
 - **Level 9 Entry** is assumed to be 8 triforces; if it's something else, various "go-time" reminders may not trigger, or may trigger incorrectly
 - **Force Ganon Fight** is assumed to be checked; if not, then "go-time" reminders will erroneously think you need the Silver Arrows

None of these are sufficiently "breaking" to advise against using Z-Tracker, but be aware of the limitations.


## <a id="startup-hs"></a> Heart Shuffle option

Turning this option off will just cause the first item box of each of the first eight dungeons to be pre-populated with a Heart Container.


## <a id="startup-hdn"></a> Hide Dungeon Numbers option

(If this is your first time reading the Z-Tracker documentation, skip this section and return to it after reading the main [gameplay](use.md) documentation, or after having already 
used Z-Tracker at least once.)

Enabling this option causes a number of changes in the tracker to help the player deal with this randomizer feature:
 - top area:
   - the dungeon triforces in the top of the tracker will be labeled ABCDEFGH instead of 12345678
   - a button above each triforce can be clicked to select that dungeon's Color and Number, once known
   - clicking the triforce to mark it gotten automatically pops up the Number chooser (and displays the 'triforce decoder diagram')
   - each dungeon has 3 boxes (some will go unused)
   - an additional 'triforce summary' appears in the upper right
      - triforces appear in 1-8 order, this helps the player see the numeric order e.g. when they are trying to recorder-to-next-dungeon 
      - hint 'halo's are associated with triforce numbers (not letters) and thus appear here by default
 - dungeon area:
   - the dungeon tabs are labeled ABCDEFGH9
   - the dungeon tabs and LEVEL- text get the Color of the dungeon
   - LEVEL-N becomes LEVEL-?, and the rainbow question mark is a button to select a Color for the dungeon
 - other:
   - the overworld map tiles for dungeons are labeled A-H
   - the blockers labels are A-H
   - voice reminders may refer to 'this dungeon' rather than e.g. 'dungeon three' when the Number is unknown
 - hotkeys:
   - pressing keyboard keys 1-8 will set the dungeon number when
      - hovering a lettered triforce
      - hovering the color/number button above the lettered triforces at the top of the tracker
      - inside the Number chooser popup

The workflow for the player then becomes:
 - when you first encounter a dungeon, label it as the first unused letter A-H: this will always be the canonical label for this dungeon
    - optionally, mark the floor color of the dungeon, by clicking either the '?' in the dungeon tab or the button above that letter's triforce
 - as you get items from the dungeon, mark them in that letter's column
 - when you get the triforce, set the dungeon Number (the Number chooser pops up automatically when you mark the triforce gotten)
 - if you can otherwise deduce with certainty the dungeon Number, set the dungeon number by clicking the button above the triforce

The Color marks are for the player's reference, and have no semantic meaning to the tracker.
The Number marks have semantics that interact with a number of tracker features, you should set it once known.


## <a id="startup-coq"></a> Start buttons (choose overworld quest)

Choose your overworld quest before you begin.  This chooses both the correct overworld map drawing as well as the sets of coordinates that may have entrance locations.
It also populates the [Spot Summary](use.md#spot-summary) with the appropriate set of locations you will eventually find.

If you don't know which overworld quest it will be, due to the rando flagset, then select "Mixed - Second Quest".  
You may need to compensate a small bit if you discover you are in a different quest:
 - coordinate locations A12, D13, and H5 may look different in a non-mixed quest
 - if the seed is actually first-quest, then the Spot Summary...
    - ...will have 1 extra Door Repair, 1 extra Money Making Game, 2 extra Potion Shops, and 3 extra item shops, and
    - ...will report 1 large and 6 small secrets, when actually there will be 3 large and 4 small secrets
 - if you discover you are in (unmixed) first quest, consider clicking the [HSQ button](use.md#hfq-hsq)
 - if you discover you are in (unmixed) second quest, consider clicking the [HFQ button](use.md#hfq-hsq)


## <a id="startup-o"></a> Options

The options menu appears on the startup page, but most options can be changed later by bringing up the [Options Menu](use.md#main-om) while the application is running.

The only option you need to setup beforehand on the startup screen is Listen for Speech; see [Speech Recognition](use.md#speech-recognition).

