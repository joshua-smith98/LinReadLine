﻿using System.Runtime.InteropServices;

namespace LinReadLine
{
    /// <summary>
    /// Container for <see cref="ReadLine()"/>.
    /// </summary>
    public static class Lin
    {
        /// <summary>
        /// The current value of the user-input line.
        /// </summary>
        private static readonly List<char> _currentLine = new();
        
        /// <summary>
        /// Converts _currentLine to string
        /// </summary>
        private static string _currentLine_str => new string(_currentLine.ToArray());

        /// <summary>
        /// Position of the cursor at the start of the current user-input line
        /// </summary>
        private static int _cursorStartIndex = -1;
        
        /// <summary>
        /// Linear position of the cursor in relation to the console buffer.
        /// </summary>
        private static int _cursorIndex
        {
            get => _cursorIndex_f;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                
                // JEBUS it took me a long time to figure this out but by golly gosh goshers its working...ITS WORKING
                // Here we correct the start index for the console scrolling down when crossing onto a new line - that way our local coords stay synced with the window
                // It's a bit of a hack, but since we don't have Console.SetWindowPosition() on Linux there's no other way!
                if (CursorIndexToCoords(value).top >= Console.BufferHeight)
                {
                    // Correct _cursorStartIndex
                    var moveAmount = CursorIndexToCoords(value).top - Console.BufferHeight + 1;
                    _cursorStartIndex -= moveAmount * Console.BufferWidth;
                    
                    // Correct _cursorIndex
                    var newCursorIndex = value - moveAmount * Console.BufferWidth;
                    var newCursorCoords = CursorIndexToCoords(newCursorIndex);
                    Console.CursorLeft = newCursorCoords.left;
                    Console.CursorTop = newCursorCoords.top;
                    _cursorIndex_f = newCursorIndex;
                    return;
                }

                // Otherwise, business as usual
                var cursorCoords = CursorIndexToCoords(value);
                Console.CursorLeft = cursorCoords.left;
                Console.CursorTop = cursorCoords.top;
                _cursorIndex_f = value;
            }
        }
        private static int _cursorIndex_f = -1;
        
        /// <summary>
        /// Linear position of the cursor in relation to the current user-input line.
        /// </summary>
        private static int _lineIndex
        {
            get => _cursorIndex - _cursorStartIndex;
            set
            {
                if (value >= 0 && value <= _currentLine.Count)
                    _cursorIndex = value + _cursorStartIndex;
            }
        }

        /// <summary>
        /// Current runtime history of returned lines.
        /// </summary>
        private static readonly List<string> _history = new(); // TODO: implement persistence outside of runtime using settings
        /// <summary>
        /// Current position in _history.
        /// </summary>
        private static int _historyPosition = -1;

        /// <summary>
        /// Reads a single user-typed line from the console, emulating Windows behaviour on Linux.<br/>
        /// If used on an OS outside of Linux, simply returns the result of <see cref="Console.ReadLine()"/>.
        /// <br/><br/>
        /// Note: Since <see cref="Console.CursorVisible.get"/> isn't available on Linux we can't save the cursor visibility when this method is called.<br/>
        /// The cursor will be visible upon return. If you need to cursor to be invisible upon return, you must (unfortunately) set it yourself.
        /// </summary>
        /// <returns></returns>
        public static string ReadLine()
        {
#if !DEBUG
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) // If we're not on Linux, then just use the standard Console.ReadLine()
                return Console.ReadLine()!;
#endif
            
            // Setup fields for reading
            _currentLine.Clear();
            _cursorStartIndex = CursorCoordsToIndex(Console.CursorLeft, Console.CursorTop);
            _cursorIndex_f = _cursorStartIndex;
            _historyPosition = -1;

            // Read loop
            while (true)
            {
                var key = Console.ReadKey(true); // Block writing of the key, we'll write it ourselves later

                // Make cursor invisible while make our changes, since it'll be moving all over the place
                Console.CursorVisible = false;
                
                switch (key.Key)
                {
                    // Enter -> Write newline and return
                    case ConsoleKey.Enter:
                        _lineIndex = _currentLine.Count; // Move cursor to the end of the user-input before writing newline
                        Console.WriteLine();
                        AddToHistory(_currentLine_str);
                        Console.CursorVisible = true; // Ensure cursor is visible before we return
                        return _currentLine_str;

                    // Escape -> Clear the current line
                    case ConsoleKey.Escape:
                        Clear();
                        break;

                    // Delete -> Remove the char at the cursor
                    case ConsoleKey.Delete:
                        Delete();
                        break;
                    
                    // Backspace -> Remove the char behind the cursor
                    case ConsoleKey.Backspace:
                        Backspace();
                        break;

                    // Up Arrow -> Cycle down through history
                    case ConsoleKey.UpArrow:
                        // Case: first time -> start at the top
                        if (_historyPosition == -1)
                            _historyPosition = _history.Count - 1;
                        // Otherwise, cycle down if within bounds
                        else if (_historyPosition > 0)
                            _historyPosition--;

                        // Only replace line if some history exists and is different to what's already there
                        if (_history.Count != 0 && _history[_historyPosition] != _currentLine_str)
                            ReplaceLineWith(_history[_historyPosition]);
                        break;

                    // Down Arrow -> Cycle up through history
                    case ConsoleKey.DownArrow:
                        // Case: first time -> start at the bottom
                        if (_historyPosition == -1) 
                            _historyPosition = 0;
                        // Otherwise, cycle up if within bounds
                        else if (_historyPosition < _history.Count -1)
                            _historyPosition++;

                        // Only replace line if some history exists and is different to what's already there
                        if (_history.Count != 0 && _history[_historyPosition] != _currentLine_str)
                            ReplaceLineWith(_history[_historyPosition]);
                        break;

                    // Left Arrow -> Move the cursor left
                    case ConsoleKey.LeftArrow:
                        _lineIndex--;
                        break;

                    // Right Arrow -> Move the cursor right
                    case ConsoleKey.RightArrow:
                        _lineIndex++;
                        break;

                    // Tab -> [do nothing for now]
                    // This is proving very hard to implement -> we'll sort this out at a later date!
                    case ConsoleKey.Tab:
                        break;

                    // Any other -> write to the console if valid
                    default:
                        if (key.KeyChar == '\u0000') break; // Ignore keys with no corresponding char
                        if (key.Modifiers == ConsoleModifiers.Alt || key.Modifiers == ConsoleModifiers.Control) break; // Ignore if control or alt is pressed
                        WriteChar(key.KeyChar);
                        break;
                }

                // Make cursor visible again while user types
                Console.CursorVisible = true;
            }
        }

        /// <summary>
        /// Refreshes the console line starting at the cursor position.
        /// </summary>
        private static void RefreshFromCurrentPosition()
        {
            var lineStartIndex = _lineIndex;
            char[] toWrite = [.. _currentLine[_lineIndex..], ' ']; // Add a space at the end to replace any characters that have been removed
            Console.Write(toWrite);
            
            // Apply scrolling compensation here as well (see _cursorIndex)
            var endWriteIndex = _cursorIndex + toWrite.Length - 1;

            if (CursorIndexToCoords(endWriteIndex).top >= Console.BufferHeight)
            {
                // Correct _cursorStartIndex
                var moveAmount = CursorIndexToCoords(endWriteIndex).top - Console.BufferHeight + 1;
                _cursorStartIndex -= moveAmount * Console.BufferWidth;
                
                // Correct _cursorIndex
                _cursorIndex_f = endWriteIndex - moveAmount * Console.BufferWidth;
            }

            _lineIndex = lineStartIndex;
        }

        /// <summary>
        /// Writes a char to the console at the cursor position and refreshes.
        /// </summary>
        /// <param name="c"></param>
        private static void WriteChar(char c)
        {
            _currentLine.Insert(_lineIndex, c);
            RefreshFromCurrentPosition();
            _lineIndex++;
        }

        /// <summary>
        /// Deletes the char at the cursor position and refreshes.
        /// </summary>
        private static void Delete()
        {
            if (_lineIndex >= _currentLine.Count) return;

            _currentLine.RemoveAt(_lineIndex);
            RefreshFromCurrentPosition();
        }

        /// <summary>
        /// Deletes the char just before the cursor position and refreshes.
        /// </summary>
        private static void Backspace()
        {
            if (_lineIndex == 0) return; // Only backspace if there is something to backspace

            _lineIndex--;
            Delete();
        }

        /// <summary>
        /// Clears the console line and returns the cursor to the start.
        /// </summary>
        private static void Clear()
        {
            // Return to beginning of line
            _lineIndex = 0;

            // Write whitespace
            Console.Write(new string(Enumerable.Repeat(' ', _currentLine.Count).ToArray()));

            // Return to beginning of line
            _lineIndex = 0;

            // Clear _currentLine
            _currentLine.Clear();
        }

        /// <summary>
        /// Clears the current console line and replaces it with the given string
        /// </summary>
        /// <param name="line"></param>
        private static void ReplaceLineWith(string line)
        {
            Clear();
            _currentLine.AddRange(line);
            RefreshFromCurrentPosition();
            _lineIndex = _currentLine.Count;
        }

        /// <summary>
        /// Adds the given string to the line history
        /// </summary>
        /// <param name="str"></param>
        private static void AddToHistory(string str)
        {
            // Don't add if str is empty
            if (str == string.Empty)
                return;
            
            // Case: str is already contained in history -> remove all previous references
            while (_history.Contains(str))
                _history.Remove(str);

            // Add new and reset position
            _history.Add(str);
            _historyPosition = -1;
        }

        /// <summary>
        /// Converts console cursor coords to a console buffer index
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        private static int CursorCoordsToIndex(int left, int top)
            => (Console.BufferWidth * top) + left;

        /// <summary>
        /// Converts a console buffer index to cursor coords
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private static (int left, int top) CursorIndexToCoords(int index)
            => (index % Console.BufferWidth, (int)Math.Floor((float)index / Console.BufferWidth));
    }
}
