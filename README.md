# umpvw

umpvw replicates mpv's macOS single-instance behavior on Windows.

It is a wrapped for mpv that uses the player's JSON IPC capabilities, that are used to replace the currently playing file. 

It also handles selecting multiple items in File Explorer and trying to play them - to handle this, it does IPC with itself. Files are launched in alphabetical order, as there isn't really a way to predict what order things will land in. 

## Requirements

.NET Framework, whatever the latest one is ¯\\\_(ツ)_/¯

## File associations and the compiled executable

Use my fork of the mpv-install script: https://github.com/SilverEzhik/mpv-install

## Fix 15 item selection limit

Follow this guide: https://support.microsoft.com/help/2022295/
