# LinReadLine
.NET's Console.ReadLine() method is functionally incomplete on Linux, with no ability to move the cursor beyond typing and backspacing.

This library aims to fix this by emulating the complete Windows behaviour on Linux.

# Features
 - Type and use **Backspace** and **Delete** as normal
 - Move the cursor with **Left/Right Arrows**
 - Cycle through history (spanning runtime) with **Up/Down Arrows**
 - Clear the line with **ESC**
 - ~~Align text with **TAB**~~ _(Planned for future version)_
 - ~~Paste with **CTRL-V**~~ _(Planned for future version)_

# Installation and Usage
To use in your own project:
1. Download the latest release from the Releases Page.
2. Place _LinReadLine.dll_ into your solution folder.
3. Add a new dependancy to your project, click "Browse" and select _LinReadLine.dll_.

You can then use ReadLine() via the _Lin_ class as follows:

```C#
// Import Namespace
using LinReadLine;

// Prompt the user
Console.Write("What's your name? ");

// Get user input
var name = Lin.ReadLine();

// Write feedback to console
Console.WriteLine($"Your name is {name}!");
```
