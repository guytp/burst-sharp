using System;
using System.Threading;
using System.Collections.Generic;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class represents a way to control the UI for the miner using a console window.
    /// </summary>
    public class ConsoleUi : IDisposable
    {
        #region Declarations
        /// <summary>
        /// Defines the current application wide instance.
        /// </summary>
        private static ConsoleUi _applicationInstance;

        /// <summary>
        /// Defines the thread we use to monitor size changes in the console.
        /// </summary>
        private Thread _consoleSizeThread;

        /// <summary>
        /// Defines whether or not our threads are alive.
        /// </summary>
        private bool _isAlive;

        /// <summary>
        /// Defines the last known window height.
        /// </summary>
        private int _windowHeight;

        /// <summary>
        /// Defines the last known window width.
        /// </summary>
        private int _windowWidth;

        /// <summary>
        /// Defines a list of all text we are displaying in the information area.
        /// </summary>
        private readonly List<string> _text = new List<string>();

        /// <summary>
        /// Defines an object used to ensure thread saftey.
        /// </summary>
        private readonly object _consoleLocker = new object();

        /// <summary>
        /// Defines whether the whole screen needs a redraw due to a rendering issue.
        /// </summary>
        private bool _redrawRequired;

        private int _textAreaWidth;

        private int _textAreaHeight;

        private int _textAreaStartRow;

        private int _textAreaStartCol;

        private int _nonceAreaWidth;

        private int _nonceAreaHeight;

        private int _nonceAreaStartRow;

        private int _nonceAreaStartCol;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public ConsoleUi()
        {
            // Store ourselves as app instance if not pre-existing
            if (_applicationInstance == null)
                _applicationInstance = this;

            // Create our size monitoring thread
            _isAlive = true;
            _consoleSizeThread = new Thread(ConsoleSizeThread) { IsBackground = true, Name = "Console Size Monitor", Priority = ThreadPriority.Lowest };
            _consoleSizeThread.Start();
        }
        #endregion

        /// <summary>
        /// This method is the main entry point for the console size thread whose main purpose is to determine whether or not the console has changed size and if so perform a full redraw.
        /// </summary>
        private void ConsoleSizeThread()
        {
            while (_isAlive)
            {
                bool update = false;
                if (_windowHeight != Console.WindowHeight)
                {
                    update = true;
                    _windowHeight = Console.WindowHeight;
                }
                if (_windowWidth != Console.WindowWidth)
                {
                    update = true;
                    _windowWidth = Console.WindowWidth;
                }
                if (update || _redrawRequired)
                {
                    RedrawScreen();
                    _redrawRequired = false;
                }
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Redraws all sections of the screen.
        /// </summary>
        private void RedrawScreen()
        {
            try
            {
                // Clear the window
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.CursorVisible = false;
                Console.Clear();

                // Draw function key areas
                Console.SetCursorPosition(0, _windowHeight - 2);
                Console.BackgroundColor = ConsoleColor.Black;
                for (int i = 0; i < _windowWidth * 2; i++)
                    Console.Write(" ");
                int widthPerFunctionKey = (_windowWidth - 20) / 10;
                string functionKeyBackground = string.Empty;
                for (int i = 0; i < widthPerFunctionKey; i++)
                    functionKeyBackground += " ";
                Console.SetCursorPosition(0, _windowHeight - 1);
                for (int i = 1; i <= 10; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write(i);
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.Write(functionKeyBackground);
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write(" ");
                }

                // Determine nonce area bounding
                _nonceAreaWidth = _windowWidth - 65;
                _nonceAreaHeight = _windowHeight - 4;
                if (_nonceAreaWidth < 0 || _nonceAreaHeight < 0)
                {
                    _nonceAreaWidth = 0;
                    _nonceAreaHeight = 0;
                }
                _nonceAreaStartCol = 63;
                _nonceAreaStartRow = 1;

                // Draw the border leaving bottom 2 rows empty
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.SetCursorPosition(1, 7);
                for (int i = 1; i < 61; i++)
                    Console.Write("═");
                Console.SetCursorPosition(0, 0);
                Console.Write("╔");
                for (int i = 1; i < _windowWidth - 1; i++)
                    Console.Write(i == 61 ? "╦" : "═");
                Console.Write("╗");
                for (int i = 1; i < _windowHeight - 3; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(i == 7 ? "╠" : "║");
                    Console.SetCursorPosition(61, i);
                    Console.Write(i == 7 ? "╣" : "║");
                    Console.SetCursorPosition(_windowWidth - 1, i);
                    Console.Write("║");
                }
                Console.SetCursorPosition(0, _windowHeight - 3);
                Console.Write("╚");
                for (int i = 1; i < _windowWidth - 1; i++)
                    Console.Write(i == 61 ? "╩" : "═");
                Console.Write("╝");

                // Draw the logo
                Console.SetCursorPosition(2, 1);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(@" _                    _            _                      ");
                Console.SetCursorPosition(2, 2);
                Console.Write(@"| |__  _   _ _ __ ___| |_      ___| |__   __ _ _ __ _ __  ");
                Console.SetCursorPosition(2, 3);
                Console.Write(@"| '_ \| | | | '__/ __| __|____/ __| '_ \ / _` | '__| '_ \ ");
                Console.SetCursorPosition(2, 4);
                Console.Write(@"| |_) | |_| | |  \__ \ ||_____\__ \ | | | (_| | |  | |_) |");
                Console.SetCursorPosition(2, 5);
                Console.Write(@"|_.__/ \__,_|_|  |___/\__|    |___/_| |_|\__,_|_|  | .__/ ");
                Console.SetCursorPosition(2, 6);
                Console.Write(@"                                                   |_|    ");

                // Now draw text
                _textAreaWidth = _windowWidth - _nonceAreaWidth - 7;
                _textAreaHeight = _windowHeight - 11;
                _textAreaStartRow = 8;
                _textAreaStartCol = 2;
                if (_textAreaWidth < 0 || _textAreaHeight < 0)
                {
                    _textAreaWidth = 0;
                    _textAreaHeight = 0;
                }
                RedrawTextArea();
            }
            catch (ArgumentOutOfRangeException)
            {
                _redrawRequired = true;
            }
        }

        /// <summary>
        /// Redraws the entire text area by filling it first and then writing new lines over the top.
        /// </summary>
        private void RedrawTextArea()
        {
            try
            {
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;
                string[] lines;
                lock (_consoleLocker)
                {
                    lines = _text.ToArray();
                }
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i >= _textAreaHeight)
                        break;
                    string text = lines[i];
                    if (text.Length > _textAreaWidth)
                        text = text.Substring(0, _textAreaWidth);
                    Console.SetCursorPosition(_textAreaStartCol, _textAreaStartRow + i);
                    while (text.Length < _textAreaWidth)
                        text += " ";
                    Console.Write(text);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                _redrawRequired = true;
            }
        }

        /// <summary>
        /// Adds a new line to our draw buffer and triggers a redraw.
        /// </summary>
        /// <param name="text">
        /// The text to draw.
        /// </param>
        private void InternalWriteLine(string text)
        {
            try
            {
                lock (_consoleLocker)
                {
                    string[] lines = text.Replace("\r", string.Empty).Split(new char[] { '\n' });
                    foreach (string line in lines)
                    {
                        int offset = 0;
                        while (offset < line.Length)
                        {
                            int toAdd = line.Length - offset < _textAreaWidth ? line.Length - offset : _textAreaWidth;
                            _text.Add(line.Substring(offset, toAdd));
                            offset += toAdd;
                        }
                    }
                    if (_text.Count > _textAreaHeight)
                        _text.RemoveRange(0, _text.Count - _windowHeight);
                }
                RedrawTextArea();
            }
            catch (ArgumentOutOfRangeException)
            {
                _redrawRequired = true;
            }
        }

        /// <summary>
        /// Write a line to the current console UI.
        /// </summary>
        /// <param name="text">
        /// The text to draw.
        /// </param>
        public static void WriteLine(string text)
        {
            _applicationInstance?.InternalWriteLine(text);
        }

        /// <summary>
        /// Run the console UI indefinitely until this object is disposed of.  This method blocks the caller.
        /// </summary>
        public void Run()
        {
            while (_isAlive)
                Console.ReadKey(true);
        }

        #region IDisposable Implementation
        /// <summary>
        /// Free up our used resources.
        /// </summary>
        public void Dispose()
        {
            _isAlive = false;
            if (_consoleSizeThread != null)
            {
                _consoleSizeThread.Join();
                _consoleSizeThread = null;
            }
        }
        #endregion
    }
}