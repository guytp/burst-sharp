using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

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
        /// Defines an object used to ensure thread saftey with deadlines.
        /// </summary>
        private readonly object _deadlineLocker = new object();

        /// <summary>
        /// Defines a list of deadlines known by the system.
        /// </summary>
        private readonly List<Deadline> _deadlines = new List<Deadline>();

        /// <summary>
        /// Defines the labels for each of the function keys.
        /// </summary>
        private readonly string[] _functionKeyLabels = new string[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, "Quit" };

        /// <summary>
        /// Defines the key presses that each function is tied to.
        /// </summary>
        private readonly Action[] _functionKeyActions = new Action[10];

        /// <summary>
        /// Defines the key press that ends the console UI loop.
        /// </summary>
        private readonly Action _exitAction;

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

            // Setup action
            _exitAction = new Action(() =>
            {
                Logger.Info("Exiting burst-sharp miner");
                _isAlive = false;
            });
            _functionKeyActions[9] = _exitAction;

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

        #region UI Drawing
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
                Console.TreatControlCAsInput = true;
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

                // And redraw the deadlines
                RedrawDeadlines();

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
        /// Draws details of known deadlines.
        /// </summary>
        private void RedrawDeadlines()
        {
            // Skip if nothing to fit
            if (_nonceAreaWidth < 4)
                return;

            // First lets clone the list for redraw
            List<ColouredTextLine> deadlineText = new List<ColouredTextLine>();
            Deadline[] deadlines;
            lock (_deadlineLocker)
                deadlines = _deadlines.ToArray();
            
            // Define shared header/footers for each deadline
            string header = "┌";
            string footer = "└";
            string blankLine = "  ";
            for (int i = 0; i < _nonceAreaWidth - 2; i++)
            {
                header += "─";
                footer += "─";
                blankLine += " ";
            }
            header += "┐";
            footer += "┘";

            // Blank out the box
            Console.BackgroundColor = ConsoleColor.Blue;
            for (int i = 0; i < _nonceAreaHeight; i++)
            {
                Console.SetCursorPosition(_nonceAreaStartCol, _nonceAreaStartRow + i);
                Console.Write(blankLine);
            }

            // Now for each deadline calculate text to display and generate text lines for it
            foreach (Deadline deadline in deadlines)
            {
                ConsoleColor backgroundColour;
                ConsoleColor foregroundColor = ConsoleColor.White;
                string deadlineStatus;
                switch (deadline.Status)
                {
                    case DeadlineStatus.Accepted:
                        deadlineStatus = "Accepted by pool";
                        backgroundColour = ConsoleColor.Green;
                        foregroundColor = ConsoleColor.Black;
                        break;
                    case DeadlineStatus.Found:
                        deadlineStatus = "Found new deadline";
                        backgroundColour = ConsoleColor.DarkYellow;
                        break;
                    case DeadlineStatus.Rejected:
                        deadlineStatus = "Rejected by pool, will not retry";
                        backgroundColour = ConsoleColor.Red;
                        break;
                    case DeadlineStatus.Submitted:
                        deadlineStatus = "Submitted to pool, awaiting response";
                        foregroundColor = ConsoleColor.Black;
                        backgroundColour = ConsoleColor.Yellow;
                        break;
                    default:
                        deadlineStatus = "Submission failed, will retry";
                        backgroundColour = ConsoleColor.DarkRed;
                        break;
                }
                deadlineText.Add(new ColouredTextLine(header, foregroundColor, backgroundColour));
                deadlineText.Add(new ColouredTextLine(PrepareDeadlineTextLine("Account:   " + deadline.Scoop.AccountId), foregroundColor, backgroundColour));
                deadlineText.Add(new ColouredTextLine(PrepareDeadlineTextLine("Block:     " + deadline.MiningInfo.BlockHeight), foregroundColor, backgroundColour));
                deadlineText.Add(new ColouredTextLine(PrepareDeadlineTextLine("Deadline:  " + deadline.DeadlineDuration), foregroundColor, backgroundColour));
                deadlineText.Add(new ColouredTextLine(PrepareDeadlineTextLine(deadlineStatus), foregroundColor, backgroundColour));
                deadlineText.Add(new ColouredTextLine(footer, foregroundColor, backgroundColour));
                deadlineText.Add(new ColouredTextLine(blankLine, ConsoleColor.Blue, ConsoleColor.Blue));
            }

            // Now draw this text out to screen but only up to number of rows we can have
            int startOffset = 0;
            int printableLines = deadlineText.Count();
            if (printableLines > _nonceAreaHeight)
                startOffset = printableLines - _nonceAreaHeight;
            int outputLine = 0;
            for (int i = startOffset; i < deadlineText.Count - 1; i++)
            {
                Console.SetCursorPosition(_nonceAreaStartCol, _nonceAreaStartRow + outputLine);
                Console.BackgroundColor = deadlineText[i].BackgroundColour;
                Console.ForegroundColor = deadlineText[i].ForegroundColour;
                outputLine++;
                Console.Write(deadlineText[i].Text);
            }
            Console.SetCursorPosition(0, 0);
        }

        /// <summary>
        /// Encapsulate a line of text to appear in a "nonce found" box.
        /// </summary>
        /// <param name="text">
        /// The text line.
        /// </param>
        /// <returns>
        /// An appropriately formatted text string.
        /// </returns>
        private string PrepareDeadlineTextLine(string text)
        {
            string textCut = text;
            int maximumTextSize = _nonceAreaWidth - 4;
            if (text.Length > maximumTextSize)
                textCut = textCut.Substring(0, maximumTextSize);
            string returnValue = "│ " + textCut;
            while (returnValue.Length < _nonceAreaWidth - 1)
                returnValue += " ";
            return returnValue + "│";
        }
        #endregion

        #region Public Display Control
        /// <summary>
        /// Write a line to the current console UI.
        /// </summary>
        /// <param name="text">
        /// The text to draw.
        /// </param>
        public static void WriteLine(string text)
        {
            if (_applicationInstance == null || !_applicationInstance._isAlive)
                Console.WriteLine(text);
            else
                _applicationInstance.InternalWriteLine(text);
        }

        /// <summary>
        /// Adds deadline details.
        /// </summary>
        /// <param name="deadline">
        /// The deadline to add.
        /// </param>
        public static void AddDeadlineDetails(Deadline deadline)
        {
            if (_applicationInstance == null || !_applicationInstance._isAlive)
                return;
            lock (_applicationInstance._deadlineLocker)
            {
                Deadline matchingDeadline = _applicationInstance._deadlines.FirstOrDefault(d => d.MiningInfo.BlockHeight == deadline.MiningInfo.BlockHeight && d.DeadlineDuration.TotalSeconds == deadline.DeadlineDuration.TotalSeconds);
                if (matchingDeadline != null)
                    _applicationInstance._deadlines.Remove(matchingDeadline);
                _applicationInstance._deadlines.Add(deadline);
                if (_applicationInstance._deadlines.Count > 50)
                    _applicationInstance._deadlines.RemoveRange(0, _applicationInstance._deadlines.Count - 50);
            }
            _applicationInstance.RedrawDeadlines();
        }
        #endregion

        #region Lifecycle Management
        /// <summary>
        /// Run the console UI indefinitely until this object is disposed of.  This method blocks the caller.
        /// </summary>
        public void Run()
        {
            while (_isAlive)
            {
                ConsoleKeyInfo keyPress = Console.ReadKey(true);
                Action callbackAction = null;
                if (keyPress.Key == ConsoleKey.C && keyPress.Modifiers == ConsoleModifiers.Control)
                    callbackAction = _exitAction;
                else
                    switch (keyPress.Key)
                    {
                        case ConsoleKey.F1:
                            callbackAction = _functionKeyActions[0];
                            break;
                        case ConsoleKey.F2:
                            callbackAction = _functionKeyActions[1];
                            break;
                        case ConsoleKey.F3:
                            callbackAction = _functionKeyActions[2];
                            break;
                        case ConsoleKey.F4:
                            callbackAction = _functionKeyActions[3];
                            break;
                        case ConsoleKey.F5:
                            callbackAction = _functionKeyActions[4];
                            break;
                        case ConsoleKey.F6:
                            callbackAction = _functionKeyActions[5];
                            break;
                        case ConsoleKey.F7:
                            callbackAction = _functionKeyActions[6];
                            break;
                        case ConsoleKey.F8:
                            callbackAction = _functionKeyActions[7];
                            break;
                        case ConsoleKey.F9:
                            callbackAction = _functionKeyActions[8];
                            break;
                        case ConsoleKey.F10:
                            callbackAction = _functionKeyActions[9];
                            break;
                    }
                try
                {
                    if (callbackAction != null)
                        callbackAction.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error handling " + keyPress.Key + " key press.", ex);
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            Logger.Info("UI has terminated");
        }
        #endregion

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
            if (_applicationInstance == this)
                _applicationInstance = null;
        }
        #endregion
    }
}