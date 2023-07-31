# Stream-Capturing Z-Tracker with OBS

You should use OBS 'Window Capture' to capture the Z-Tracker window for streaming.  

For best results, set the 'Window Match Priority' to 'Window title must match'.  (In streamlabs, scroll down in the initial source selection and pick "Window Capture." 
The options for Window Match Priority comes up for that, and then you pick "Window title must match.")

Be sure that your emulator does not need window focus (e.g. in FCEUX Config, Enable 'Run in Background' and 'Background Input'), as Z-Tracker requires window focus to
respond to mousing and hotkeys.

### Preventing Z-Tracker reminder sounds/speech from being restreamed

If you are playing in a race that will be restreamed, and you want to hear Z-Tracker reminder sounds or speech, but don't want those sounds to be streamed, you can do this
(assuming you have OBS version 28 or higher):

- Add a Source, of type "Application Audio Capture (BETA)"
- optionally rename it
- Choose the source to be from e.g. your NES Console Emulator

This will cause a new Element to appear in the Audio Mixer, so that you can

- Mute the Desktop Audio channel (that typically outputs all computer audio to your stream)
- Just enable the new Source, which captures Audio only from one specific application (e.g. your emulator)

After the restreamed race is completed, you can switch back to normal streaming by muting the new audio source and unmuting Desktop Audio.

(To permanently remove this new source from your Audio Mixer, just remove the Source you added to your Sources back in the first step.)

### For those using the Broadcast Window...

Read the [Broadcast Window section](extras.md#broadcast-window) for more capture and sizing options.  If you do use the Broadcast Window, then you should probably turn OFF the
'Capture cursor' checkbox in the OBS Window Capture of the Broadcast Window (the Broadcast Window displays its own virtual mouse cursor as window content).  If you capture the Z-Tracker 
main app window directly, then you should probably turn ON the 'Capture cursor' checkbox in the OBS Window Capture of the main app window, so that viewers can see 'where you are',
as typically during normal gameplay, your cursor will naturally be resting on or near a dungeon room or overworld map tile where you currently are.

OBS can typically only capture windows that are drawn on the screen.  This means that you should not minimize the Broadcast Window, nor should you have any part of it 'off your desktop'.
Since the streamer's physical display real estate is typically at a premium, one of the best places to place the Broadcast Window on your desktop is _behind_ the main application window.
This way it remains on the drawing surface of your desktop (to get drawing updates that OBS can capture), but also does not take up extra visible screen real estate (since it's behind another 
window).  Here is an example:

![sample streamer desktop layout screenshot](screenshots/sample-desktop-layout.png)

(In this example, OBS and my twitch channel dashboard are on a second monitor.)  The main application window is fully visible to the streamer on the right hand side.  Just slightly down 
and left of it, you can see the Broadcast Window peeking out from behind.  Leaving an edge peeking out makes it possible for the streamer to see at a glance if the upper 'overworld view' or 
the lower 'dungeon view' is currently being broadcast on stream.

