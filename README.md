## I Member! v0.0.1
https://github.com/breadweb/imember

Copyright (c) 2013 - 2019, Adam "Bread" Slesinger http://www.breadweb.net

All rights reserved.

Date: 2/13/2019 9:21:45

<br>

### TL;DR:

This is a small .NET application that saves location and size of all open application windows and restores them when display settings change. It mimics the behavior I've noticed of MacOS when displays disconnect and reconnect.  

![](https://github.com/breadweb/imember/blob/master/images/imember.png) 

<br>

### Download

Please feel free to use this app if you'd find it useful. I have only tested it on my PC and with my hardware so your experience may be different. If you find any bugs, please let me know! 

Right-click the following link and select "Save link as..."

[imember-v0.0.1.zip](https://github.com/breadweb/imember/releases/download/0.0.1/imember-v0.0.1.zip) (93 KB)

<br>

### How to Use

Simply start the app and it will minimize to the system tray. Click the tray icon to show the window or right click and choose "Exit" to exit the app completely.  When running, the app saves configuration of your windows every 60 seconds. If you want to save your configuration instantly, click the "Save Now" button.

<br>

### More Info

I use a dual 4K displayport KVM switch to share my two monitors between my PC and my MacBook Pro.  The switch is great except that it does not do monitor emulation. When the monitors switch to my MacBook, Windows looses connection and sets the display configuration to one monitor at 640x480. This resizes all windows to fit in that small area and shoves everything in the top left corner. When I switch back to my PC and the monitors are dectected and resolution is resotred, the windows aren't. That was super tilting... My MacBook Pro is smart enough to restore window positions so why can't Windows do the same? 

I tried looking for an application that would save and restore window arragements but couldn't find one that did only that. Usually that feature was packed into an app that wasn't free, or that came with dozens of extra features that I didn't want. This was annoying me multiple times a day so I took an afternoon to make this app. :)

<br>

### Known Issues

I added an option to set a registry key so the app will start when Windows starts. Using this caused my anti-virus to freak out and quarantinne the application and I was required to add an exception. If you click that checkbox multiple times in quick succession, Windows also thinks something is suspicious and will block further attempts until your reboot. The code for this is below:

```
private const string REG_ENTRY = "imember";

private void checkBox1_CheckedChanged(object sender, EventArgs e)
{
    RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

    if (checkBox1.Checked)
    {
        rk.SetValue(REG_ENTRY, Application.ExecutablePath);
    }
    else
    {
        rk.DeleteValue(REG_ENTRY, false);
    }
}
```
