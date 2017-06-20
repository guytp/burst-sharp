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
        /// Defines the width of the logo, excluding borders.
        /// </summary>
        private const int LogoWidth = 58;

        /// <summary>
        /// Defines the height of the logo, excluding borders.
        /// </summary>
        private const int LogoHeight = 6;

        /// <summary>
        /// Defines the maximum width of the nonce section.
        /// </summary>
        private const int MaximumNonceSectionWidth = 80;

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
        /// Defines the labels for each of the function keys.
        /// </summary>
        private readonly string[] _functionKeyLabels = new string[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, "Quit" };

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
                    _redrawRequired = false;
                    RedrawScreen();
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
                // Clear the window and set its state
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.BufferHeight = _windowHeight;
                Console.BufferWidth = _windowWidth;
                Console.CursorVisible = false;
                Console.Title = "burst-sharp miner";
                Console.Clear();

                // We fill entire thing black as that's what we want at bottom and bottom right corner weirdly clips - but main window we actually want to have a blue background
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Blue;
                string blankLine = string.Empty;
                for (int i = 0; i < _windowWidth * (_windowHeight - 2); i++)
                    blankLine += " ";
                Console.Write(blankLine);

                // Determine nonce area bounding - we want a maximum width of 80
                int availableNonceWidth = _windowWidth - LogoWidth - 7; // 7 for: 2 - left/right window borders, 1 - central border, 4 - padding in each section
                _nonceAreaWidth = availableNonceWidth > MaximumNonceSectionWidth ? MaximumNonceSectionWidth : availableNonceWidth;
                _nonceAreaHeight = _windowHeight - 4;
                if (_nonceAreaWidth < 0 || _nonceAreaHeight < 0)
                {
                    _nonceAreaWidth = 0;
                    _nonceAreaHeight = 0;
                }
                _nonceAreaStartCol = _windowWidth - _nonceAreaWidth - 2;
                _nonceAreaStartRow = 1;

                // Draw the border leaving bottom 2 rows empty as we've already drawn function keys here
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.BackgroundColor = ConsoleColor.Blue;

                // First draw the horizontal borders across the center row underneath the logo up to where it meets the main content / nonce border
                int logoBottomBorderRow = LogoHeight + 1;
                int mainContentRightBorderColumn = _windowWidth - _nonceAreaWidth - 4; // 3 for two padding spaces in nonce area plus the rightmost border and one for offset
                Console.SetCursorPosition(1, logoBottomBorderRow);
                for (int i = 1; i < mainContentRightBorderColumn; i++)
                    Console.Write("═");

                // Now draw our top left corner notch
                Console.SetCursorPosition(0, 0);
                Console.Write("╔");

                // Go across from there filling the top of the window with a downward notch to meet the other horizontal we just drew
                for (int i = 1; i < _windowWidth - 1; i++)
                    Console.Write(i == mainContentRightBorderColumn ? "╦" : "═");

                // Now at the very edge lets draw our down section
                Console.Write("╗");

                // Now draw all of our down sections
                for (int i = 1; i < _windowHeight - 3; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(i == logoBottomBorderRow ? "╠" : "║");
                    Console.SetCursorPosition(mainContentRightBorderColumn, i);
                    Console.Write(i == logoBottomBorderRow ? "╣" : "║");
                    Console.SetCursorPosition(_windowWidth - 1, i);
                    Console.Write("║");
                }

                // Draw our bottom left border
                Console.SetCursorPosition(0, _windowHeight - 3);
                Console.Write("╚");

                // Go across from there to complete the bottom border
                for (int i = 1; i < _windowWidth - 1; i++)
                    Console.Write(i == mainContentRightBorderColumn ? "╩" : "═");

                // And finally bottom right joint
                Console.Write("╝");

                // Draw the logo
                int logoColumn = 2 + ((mainContentRightBorderColumn - LogoWidth - 4) / 2);
                Console.SetCursorPosition(logoColumn, 1);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(@" _                    _            _                      ");
                Console.SetCursorPosition(logoColumn, 2);
                Console.Write(@"| |__  _   _ _ __ ___| |_      ___| |__   __ _ _ __ _ __  ");
                Console.SetCursorPosition(logoColumn, 3);
                Console.Write(@"| '_ \| | | | '__/ __| __|____/ __| '_ \ / _` | '__| '_ \ ");
                Console.SetCursorPosition(logoColumn, 4);
                Console.Write(@"| |_) | |_| | |  \__ \ ||_____\__ \ | | | (_| | |  | |_) |");
                Console.SetCursorPosition(logoColumn, 5);
                Console.Write(@"|_.__/ \__,_|_|  |___/\__|    |___/_| |_|\__,_|_|  | .__/ ");
                Console.SetCursorPosition(logoColumn, 6);
                Console.Write(@"                                                   |_|    ");

                // Add nonce header
                string nonceHeaderText = " Found Nonces ";
                Console.SetCursorPosition(mainContentRightBorderColumn + 1 + (_nonceAreaWidth / 2) - (nonceHeaderText.Length / 2), 0);
                Console.Write(nonceHeaderText);

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

                // Draw function key areas
                int functionKeyReservedSpacing = 31; // "FX" for all keys plus 9 spacing bars where FX can be F10 as well as FX and one extra space due to weird bottom right corner.
                int widthPerFunctionKey = (_windowWidth - functionKeyReservedSpacing) / 10;
                if (widthPerFunctionKey > 0)
                {
                    int startX = (_windowWidth - (widthPerFunctionKey * 10) - functionKeyReservedSpacing) / 2;
                    Console.SetCursorPosition(startX, _windowHeight - 1);
                    for (int i = 1; i <= 10; i++)
                    {
                        // Write "FX" label outside the button
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write("F");
                        Console.Write(i);

                        // Pad the label to required width
                        string label = _functionKeyLabels[i - 1];
                        while (label.Length < widthPerFunctionKey)
                            label += " ";
                        if (label.Length > widthPerFunctionKey)
                            label = label.Substring(0, widthPerFunctionKey);

                        // And draw the label
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.DarkCyan;
                        Console.Write(label);

                        // Unless we're on the last label, add a space
                        if (i != 10)
                        {
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.Write(" ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentOutOfRangeException))
                    Logger.Error("Error updating console", ex);
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
                Console.SetCursorPosition(0, 0);
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
            if (_textAreaWidth < 1)
                return;
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