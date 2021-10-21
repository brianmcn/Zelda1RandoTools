# HotKeys and extra windows

## <a id="hotkeys"></a> HotKeys

You can bind hotkeys (keyboard keys 0-9 or a-z) by editing the HotKeys.txt file in the Z-Tracker folder.  The text file format is self-describing.

There are four mouse-hover 'contexts' for hotkeys: [item boxes](use.md#item-boxes), overworld tiles, blockers, and dungeon rooms.  So for example 'b' could be set up to mean 'bow' 
when the mouse is over an item box, 'bomb shop' when the mouse is over an overworld tile, 'bow & arrow' when the mouse is over blockers, and 'bomb upgrade 
room' when the mouse is over a dungeon room.

While there is no support for editing or reloading hotkey information inside the application, Z-Tracker does provide a way to display your current hotkey
information.  Click the button 'Show HotKeys' to the right of the overworld map legend, and it will create a new, resizable, window displaying your hotkey
mappings.  You can choose to leave that window wherever you like on your desktop as a 'cheat sheet' when trying to learn your hotkeys; close the hotkey
window at any time when you are done with it.

Hotkeys only work when the Z-Tracker window has focus.  If your hotkeys don't appear to be working, click somewhere in the app window to ensure that window 
has focus, and then try again to hover an empty item/overworld/blocker/dungeon box and press a keyboard hotkey.

Hotkeys have some 'smarts' in addition to just 'setting the state you pressed':
 - item boxes
    - repeat presses of the same item will cycle the red-green-purple of the box outline, so for example is 'b' is bow, then 'bbbb' on an empty box would go
      empty box -> red bow -> green bow -> purple bow -> red bow
 - overworld
    - on an item shop, pressing a hotkey of an item already-in-a-shop will remove that item from the shop
    - on an item shop, pressing a hotkey of a different item, when the shop has an extra space, will add that item to the shop
    - on an item shop, if the shop is filled with two items, pressing a third item's hotkey will replace the first item
    - on tiles with a brightness toggle, repeat hotkeys will toggle between "bright" and "dark" versions of the tile icon
 - blockers
    - a repeat hotkey toggles a blocker box back to empty
 - dungeon rooms
    - a repeat hotkey toggles a RoomType, MonsterDetail, or FloorDropDetail back to Unmarked
These ad-hoc behaviors are designed either to make common cases fast and easy, or to make it easy to correct mismarks from 'fat fingering'.


SAVE STATE

There is no support for saving in-progress game data; the tool is currently designed only for playing a single continuous session.


BROADCAST WINDOW (WPF only)

On the options menu, you can opt into a 'broadcast window'.  This causes a separate, smaller window to appear; the new window is designed for stream-capture, 
for streamers who think the Z-Tracker app is too large for their stream layout.  The broadcast window has two possible displays: one is overworld-focused, and
the other is dungeon-map focused.  The broadcast window will automatically switch between views depending upon if your mouse is in the upper portion or the 
lower portion of the main Z-Tracker window.  The broadcast window is not interactive - it does not respond to mouse clicks, and is only a display.  Thus, you 
use the main Z-Tracker window exactly as your normally would, but rather than have your stream capture the main interactive Z-Tracker window, you instead 
capture the smaller broadcast window, and it will automatically show the correct subset of the view to your viewers, based on your mousing.

The broadcast window also has an option to be 2/3 size or 1/3 size (512 or 256 pixels wide, rather than 768 pixels wide).  
Using this exact size ratio can help keep a bit of the pixel art 'crisp' in a smaller area, and may look better than using OBS to downscale your screen capture 
to some arbitrary but similar size.  


WINDOW SIZE (WPF only)

The Z-Tracker application is somewhat large (the window content is 768x963), designed to make all of the important information available to the player on-screen 
at once.  However some users may need or desire a smaller application.  You can make the application window 2/3 size by editing the file 
"Z1R_Tracker_settings.json" in a text editor, and changing "SmallerAppWindow" from 'false' to 'true'.  A few of the elements may not look as good at this size,
but most of the app's graphics are in multiples of 3 pixels, so this still looks decent and can make the app usable on machines with smaller display resolutions.

