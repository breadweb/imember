## Bread Window Positioner v1.1.0
https://github.com/breadweb/imember

Copyright (c) 2019, Adam "Bread" Slesinger http://www.breadweb.net

All rights reserved.

Date: 6/29/2019 7:59:15

<br>

### TL;DR:

This is a small .NET application that saves location and size of all open application windows and restores them when display settings change. It mimics the behavior you get for free with macOS when displays disconnect and reconnect.  

<br>

### Download

Please feel free to use this app if you'd find it useful. I have only tested it on my PC and with my hardware so your experience may be different. If you find any bugs, please let me know! 

Right-click the following link and select "Save link as..."

[imember-v1.0.0.zip](https://github.com/breadweb/imember/releases/download/1.1.0/imember-v1.1.0.zip) (96 KB)

<br>

### How to Use

Start the app and it will minimize to the system tray. You can do all actions by right-clicking the system tray icon. Left-click the system tray icon to bring up info window.

When running, the app saves arrangement of your windows every minute.

If you want to save your arrangement instantly, click the "Save Now" option.

You can temporarily disable the app. This is handy if you're going to be running applications that change the resolution and you don't want window arrangements to be saved when that happens.

To completely close the application, right-click on the system tray icon and select "Exit".
   


<br>

### More Info

I use a dual 4K displayport KVM switch to share my two monitors between my PC and my MacBook Pro.  The switch is great except that it does not do monitor emulation. When the monitors switch to my MacBook, Windows looses connection and sets the display configuration to one monitor at 640x480. This resizes all windows to fit in that small area and shoves everything in the top left corner. When I switch back to my PC and the monitors are detected and resolution is restored, the windows aren't. This is aggravating, especially when my MacBook Pro is smart enough to restore last-known window positions and sizes.

I tried looking for an application that would save and restore window arrangements but couldn't find one that did only that. Usually that feature was packed into an app that wasn't free, or that came with dozens of extra features that I didn't want. This was annoying me multiple times a day so I took an afternoon to make this app. :)

<br>

### Known Issues and Future Optimizations

The program could be optimized by only saving arrangements when display settings are about to change instead of on an interval. It would be less processing and also avoid an issue where the most recent arrangement may not be saved before displays change. To do that, I need to address the problem identified in the comments:

```
// Saves the current window arrangement on an interval. This is a lazy solution to a problem
// with the display events. The SystemEvents.DisplaySettingsChanging event fires after user
// display scaling (such as 150% typically used on 4K monitors) resets and so positions saved
// at that time would not be correct when applied after the SystemEvents.DisplaySettingsChanged
// event fires which is after scaling is reapplied. We could try to figure out the DPI and
// adjust all positions and sizes, but this works well enough for now.
```
