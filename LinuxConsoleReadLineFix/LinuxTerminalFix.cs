using System.Runtime.CompilerServices;

namespace LinuxConsoleReadLineFix
{
    public static class LinuxTerminalFix
    {
        private static readonly List<char> _currentLine = new();
        private static int _linePosition 
        {
            get => _linePosition_f;
            set
            {
                if (value > _linePosition_f && value <= _currentLine.Count)
                {
                    for (int i = _linePosition_f; i < value; i++)
                        MoveCursorRight();

                    _linePosition_f = value;
                }
                else if (value < _linePosition_f && value >= 0)
                {
                    for (int i = _linePosition_f; i > value; i--)
                        MoveCursorLeft();

                    _linePosition_f = value;
                }
            }
        }
        private static int _linePosition_f = -1;
        private static readonly List<string> _history = new(); // TODO: implement persistence outside of runtime using settings
        private static int _historyPosition = -1;
        
        public static string ReadLine()
        {
            _currentLine.Clear();
            _linePosition_f = 0;
            _historyPosition = _history.Count - 1;

            while (true)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter: // Write newline and return
                        Console.WriteLine();
                        _history.Add(new string(_currentLine.ToArray()));
                        return new string(_currentLine.ToArray());
                    case ConsoleKey.Escape: // Clear the current line
                        Clear();
                        break;
                    case ConsoleKey.Delete:
                        Delete();
                        break;
                    case ConsoleKey.Backspace:
                        Backspace();
                        break;
                    case ConsoleKey.UpArrow:
                        // do thing
                        break;
                    case ConsoleKey.DownArrow:
                        // do other thing
                        break;
                    case ConsoleKey.LeftArrow:
                        _linePosition--;
                        break;
                    case ConsoleKey.RightArrow:
                        _linePosition++;
                        break;
                    case ConsoleKey.Tab:
                        WriteTab();
                        break;
                    default:
                        WriteChar(key.KeyChar);
                        break;
                }
            }
        }

        private static void MoveCursorLeft()
        {
            // Case: cursor is at the very start of the console buffer
            if (Console.CursorLeft == 0 && Console.CursorTop == 0)
                return;

            // Case: cursor is at the start of a line -> wrap around to the previous
            if (Console.CursorLeft == 0)
            {
                Console.CursorLeft = Console.BufferWidth - 1;
                Console.CursorTop--;
                return;
            }

            // All other cases
            Console.CursorLeft--;
        }

        private static void MoveCursorRight()
        {
            // Case: cursor is at the end of a line -> wrap around to the next
            if (Console.CursorLeft == Console.BufferWidth - 1)
            {
                Console.CursorLeft = 0;
                Console.CursorTop++;
                return;
            }

            // All other cases
            Console.CursorLeft++;
        }

        private static void RefreshFromCurrentPosition()
        {
            Console.CursorVisible = false;
            char[] toWrite = [.. _currentLine[_linePosition..], ' ']; // Add a space at the end to replace any characters that have been removed
            Console.Write(toWrite);
            _linePosition_f += toWrite.Length;
            _linePosition -= toWrite.Length;
            Console.CursorVisible = true;
        }

        private static void WriteChar(char c)
        {
            _currentLine.Insert(_linePosition, c);
            RefreshFromCurrentPosition();
            _linePosition++;
        }

        private static void WriteTab()
        {
            WriteChar('\t');
        }

        private static void Delete()
        {
            if (_linePosition >= _currentLine.Count) return;

            _currentLine.RemoveAt(_linePosition);
            RefreshFromCurrentPosition();
        }

        private static void Backspace()
        {
            _linePosition--;
            Delete();
        }

        private static void Clear()
        {
            // Return to beginning of line
            _linePosition = 0;

            // Write whitespace
            Console.Write(new string(Enumerable.Repeat(' ', _currentLine.Count).ToArray()));
            _linePosition_f = _currentLine.Count;

            // Return to beginning of line
            _linePosition = 0;

            // Clear _currentLine
            _currentLine.Clear();
        }

        private static void ReplaceLineWith(string line)
        {
            Clear();
            _currentLine.AddRange(line);
            RefreshFromCurrentPosition();
            _linePosition = _currentLine.Count - 1;
        }

        private static void AddToHistory(string str)
        {
            // Case: str is already contained in history -> remove all previous references
            while (_history.Contains(str))
                _history.Remove(str);

            // Add new and reset position
            _history.Add(str);
            _historyPosition = _history.Count - 1;
        }
    }
}
