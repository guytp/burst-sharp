using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class represents a way to control the UI for the miner using a console window.
    /// </summary>
    public class ConsoleUi : IDisposable
    {
        #region Declarations
        #region Constants
        /// <summary>
        /// Defines the width of the logo, excluding borders.
        /// </summary>
        private const int LogoWidth = 66;

        /// <summary>
        /// Defines the height of the logo, excluding borders.
        /// </summary>
        private const int LogoHeight = 9;

        /// <summary>
        /// Defines the maximum width of the nonce section.
        /// </summary>
        private const int MaximumNonceSectionWidth = 80;

        /// <summary>
        /// Defines the maximum width of text to allocate on the right of the percentage bar.
        /// </summary>
        private const int ProgressBarTextWidth = 25;
        #endregion

        /// <summary>
        /// Defines the current application wide instance.
        /// </summary>
        private static ConsoleUi _applicationInstance;

        /// <summary>
        /// Defines the thread we use to monitor size changes in the console.
        /// </summary>
        private Thread _thread;

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

        #region Redraw Flags
        /// <summary>
        /// Defines whether the whole screen needs a redraw due to a rendering issue.
        /// </summary>
        private bool _redrawRequired;

        /// <summary>
        /// Defines a flag that can be used to signify redrawing of main text is needed.
        /// </summary>
        private bool _redrawTextRequired;

        /// <summary>
        /// Defines a flag that can be used to signify redrawing of deadlines is needed.
        /// </summary>
        private bool _redrawDeadlinesRequired;

        /// <summary>
        /// Defines a flag that can be used to signify redrawing of the progress bar is needed.
        /// </summary>
        private bool _redrawProgressBarRequired;

        /// <summary>
        /// Defines an object to ensure thread saftey when modifying redraw requirements.
        /// </summary>
        private readonly object _redrawLocker = new object();
        #endregion

        #region Text Rendering
        /// <summary>
        /// Defines the text to display in the header of the main log area.
        /// </summary>
        private string _textAreaHeader;

        /// <summary>
        /// Defines a list of all text we are displaying in the information area.
        /// </summary>
        private readonly List<ColouredTextLine> _text = new List<ColouredTextLine>();

        /// <summary>
        /// Defines an object used to ensure thread saftey when accessing displayed text.
        /// </summary>
        private readonly object _textLocker = new object();

        /// <summary>
        /// Defines how many columns wide the text area is, excluding margins.
        /// </summary>
        private int _textAreaWidth;

        /// <summary>
        /// Defines how many rows high the text area is, excluding margins.
        /// </summary>
        private int _textAreaHeight;

        /// <summary>
        /// Defines which row the text area starts on.
        /// </summary>
        private int _textAreaStartRow;

        /// <summary>
        /// Defines which column the text area start on.
        /// </summary>
        private int _textAreaStartCol;
        #endregion

        #region Deadline Rendering
        /// <summary>
        /// Defines an object used to ensure thread saftey with deadlines.
        /// </summary>
        private readonly object _deadlineLocker = new object();

        /// <summary>
        /// Defines a list of deadlines known by the system.
        /// </summary>
        private readonly List<Deadline> _deadlines = new List<Deadline>();

        /// <summary>
        /// Defines the width of the deadline area, excluding margins.
        /// </summary>
        private int _deadlineAreaWidth;

        /// <summary>
        /// Defines the height of the deadline area, excluding margins.
        /// </summary>
        private int _deadlineAreaHeight;

        /// <summary>
        /// Defines which row the deadline area start on.
        /// </summary>
        private int _deadlineAreaStartRow;

        /// <summary>
        /// Defines which column the deadline area start on.
        /// </summary>
        private int _deadlineAreaStartCol;
        #endregion

        #region Function Keys
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
        #endregion

        #region Progress Bar
        /// <summary>
        /// Defines whether or not the progress bar is currently visible.
        /// </summary>
        private bool _progressBarVisible;

        /// <summary>
        /// Defines which end-animation is currently shown on the progress bar end piece when it is animating - a value 0-7 inclusive.
        /// </summary>
        private byte _progressBarAnimationIndex;

        /// <summary>
        /// Defines the progress bar from - from 0 to 1.
        /// </summary>
        private decimal _progressBarValue;

        /// <summary>
        /// Defines the text to show at the right of the progress bar after the percentage text - a maximum of 10 characters.
        /// </summary>
        private string _progressBarText;

        /// <summary>
        /// Defines an object to use for thread saftey.
        /// </summary>
        private readonly object _progressBarLocker = new object();
        #endregion
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
            _thread = new Thread(ConsoleDrawingThread) { IsBackground = true, Name = "Console Drawing", Priority = ThreadPriority.Highest };
            _thread.Start();
        }
        #endregion

        /// <summary>
        /// This method is the main entry point for the console size thread whose main purpose is to determine whether or not the console has changed size and if so perform a full redraw.
        /// </summary>
        private void ConsoleDrawingThread()
        {
            while (_isAlive)
            {
                // Check if size has changed anywhere
                bool redrawRequired = false;
                if (_windowHeight != Console.WindowHeight)
                {
                    redrawRequired = true;
                    _windowHeight = Console.WindowHeight;
                }
                if (_windowWidth != Console.WindowWidth)
                {
                    redrawRequired = true;
                    _windowWidth = Console.WindowWidth;
                }

                // Determine what needs redrawing
                bool redrawDeadlinesRequired;
                bool redrawTextRequired;
                bool redrawProgressBarRequired;
                lock (_redrawLocker)
                {
                    // Set local states first
                    redrawRequired = redrawRequired || _redrawRequired;
                    redrawDeadlinesRequired = _redrawDeadlinesRequired;
                    redrawTextRequired = _redrawTextRequired;
                    redrawProgressBarRequired = _redrawProgressBarRequired;

                    // Reset all states back
                    _redrawRequired = false;
                    _redrawProgressBarRequired = false;
                    _redrawTextRequired = false;
                    _redrawDeadlinesRequired = false;
                }

                // Redraw as appropriate
                if (redrawRequired)
                {
                    _redrawRequired = false;
                    RedrawScreen();
                    Console.SetCursorPosition(0, 0);
                }
                else
                {
                    if (redrawDeadlinesRequired)
                        RedrawDeadlines();
                    if (redrawProgressBarRequired)
                        RedrawProgressBar();
                    if (redrawTextRequired)
                        RedrawTextArea();
                    Console.SetCursorPosition(0, 0);
                }
                Thread.Sleep(20);
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.BufferHeight = _windowHeight;
                    Console.BufferWidth = _windowWidth;
                }
                Console.TreatControlCAsInput = true;
                Console.CursorVisible = false;
                Console.Title = "burst-sharp miner";
                int totalCharacters = _windowHeight * _windowWidth;
                StringBuilder blankingText = new StringBuilder(totalCharacters);
                for (int i = 0; i < totalCharacters; i++)
                    blankingText.Append(" ");
                Console.Write(blankingText.ToString());

                // We fill entire thing black as that's what we want at bottom and bottom right corner weirdly clips - but main window we actually want to have a blue background
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Blue;
                string blankLine = string.Empty;
                for (int i = 0; i < _windowWidth * (_windowHeight - 2); i++)
                    blankLine += " ";
                Console.Write(blankLine);

                // Determine nonce area bounding - we want a maximum width of 80
                int availableNonceWidth = _windowWidth - LogoWidth - 7; // 7 for: 2 - left/right window borders, 1 - central border, 4 - padding in each section
                _deadlineAreaWidth = availableNonceWidth > MaximumNonceSectionWidth ? MaximumNonceSectionWidth : availableNonceWidth;
                _deadlineAreaHeight = _windowHeight - 4;
                if (_deadlineAreaWidth < 0 || _deadlineAreaHeight < 0)
                {
                    _deadlineAreaWidth = 0;
                    _deadlineAreaHeight = 0;
                }
                _deadlineAreaStartCol = _windowWidth - _deadlineAreaWidth - 2;
                _deadlineAreaStartRow = 1;

                // Draw the border leaving bottom 2 rows empty as we've already drawn function keys here
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.BackgroundColor = ConsoleColor.Blue;

                // First draw the horizontal borders across the center row underneath the logo up to where it meets the main content / nonce border
                int logoBottomBorderRow = LogoHeight + 1;
                int mainContentRightBorderColumn = _windowWidth - _deadlineAreaWidth - 4; // 3 for two padding spaces in nonce area plus the rightmost border and one for offset
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
                Console.Write(@"     _                    _            _                      ");
                Console.SetCursorPosition(logoColumn, 2);
                Console.Write(@"    | |__  _   _ _ __ ___| |_      ___| |__   __ _ _ __ _ __  ");
                Console.SetCursorPosition(logoColumn, 3);
                Console.Write(@"    | '_ \| | | | '__/ __| __|____/ __| '_ \ / _` | '__| '_ \ ");
                Console.SetCursorPosition(logoColumn, 4);
                Console.Write(@"    | |_) | |_| | |  \__ \ ||_____\__ \ | | | (_| | |  | |_) |");
                Console.SetCursorPosition(logoColumn, 5);
                Console.Write(@"    |_.__/ \__,_|_|  |___/\__|    |___/_| |_|\__,_|_|  | .__/ ");
                Console.SetCursorPosition(logoColumn, 6);
                Console.Write(@"                                                       |_|    ");
                Console.SetCursorPosition(logoColumn, 8);
                Console.Write("   Written by guytp - development donations are always welcome.");
                Console.SetCursorPosition(logoColumn, 9);
                Console.Write("[1HmJjK9nNvEstQNLBfxJyvTfkgGBCmngHZ]  [BURST-DXGM-62RW-X8G6-A493G]");

                // Add text area header if defined
                if (_textAreaHeader != null)
                {
                    int availableWidth = mainContentRightBorderColumn - 1;
                    string textToWrite = _textAreaHeader.Length + 2 > availableWidth ? " " + _textAreaHeader.Substring(0, availableWidth - 2) + " " : " " + _textAreaHeader + " ";
                    int centerPoint = (availableWidth / 2) + 1;
                    Console.SetCursorPosition(centerPoint - (textToWrite.Length / 2), LogoHeight + 1);
                    Console.Write(textToWrite);
                }

                // Add nonce header
                string nonceHeaderText = " Found Deadlines ";
                Console.SetCursorPosition(mainContentRightBorderColumn + 2 + (_deadlineAreaWidth / 2) - (nonceHeaderText.Length / 2), 0);
                Console.Write(nonceHeaderText);

                // Now draw text
                _textAreaWidth = _windowWidth - _deadlineAreaWidth - 7;
                _textAreaHeight = _windowHeight - 5 - LogoHeight;
                _textAreaStartRow = LogoHeight + 2;
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

                // Draw the progress bar
                RedrawProgressBar();
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
                // Get a handle to the text we'll draw
                ColouredTextLine[] lines;
                lock (_textLocker)
                    lines = _text.ToArray();

                // We need to count bototm to top to determine which lines to display as we may not be able to fit them all in
                int startOffset = 0;
                int totalLinesCounted = 0;
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string text = lines[i].Text;
                    totalLinesCounted += text.Length / _textAreaWidth;
                    if (text.Length % _textAreaWidth > 0)
                        totalLinesCounted++;
                    if (totalLinesCounted >= _textAreaHeight)
                    {
                        startOffset = i;
                        break;
                    }
                }
                int linesToSkip = totalLinesCounted <= _textAreaHeight ? 0 : totalLinesCounted - _textAreaHeight;

                // Now draw as appropriate
                int linesWritten = 0;
                int initialLinesSkipped = 0;
                for (int i = startOffset; i < lines.Length; i++)
                {
                    ColouredTextLine colouredText = lines[i];
                    Console.BackgroundColor = colouredText.BackgroundColour;
                    Console.ForegroundColor = colouredText.ForegroundColour;
                    string text = colouredText.Text;
                    for (int characterOffset = 0; characterOffset < text.Length;)
                    {
                        int remainingChars = text.Length - characterOffset;
                        int toRead = remainingChars > _textAreaWidth ? _textAreaWidth : remainingChars;
                        string thisChunk = text.Substring(characterOffset, toRead);
                        while (thisChunk.Length < _textAreaWidth)
                            thisChunk += " ";
                        if (linesToSkip <= initialLinesSkipped)
                        {
                            Console.SetCursorPosition(_textAreaStartCol, _textAreaStartRow + linesWritten);
                            Console.Write(thisChunk);
                            linesWritten++;
                        }
                        else
                            initialLinesSkipped++;
                        characterOffset += toRead;
                    }
                }
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
            if (_deadlineAreaWidth < 4)
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
            for (int i = 0; i < _deadlineAreaWidth - 2; i++)
            {
                header += "─";
                footer += "─";
                blankLine += " ";
            }
            header += "┐";
            footer += "┘";

            // Blank out the box
            Console.BackgroundColor = ConsoleColor.Blue;
            for (int i = 0; i < _deadlineAreaHeight; i++)
            {
                Console.SetCursorPosition(_deadlineAreaStartCol, _deadlineAreaStartRow + i);
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
            if (printableLines > _deadlineAreaHeight)
                startOffset = printableLines - _deadlineAreaHeight;
            int outputLine = 0;
            for (int i = startOffset; i < deadlineText.Count - 1; i++)
            {
                Console.SetCursorPosition(_deadlineAreaStartCol, _deadlineAreaStartRow + outputLine);
                Console.BackgroundColor = deadlineText[i].BackgroundColour;
                Console.ForegroundColor = deadlineText[i].ForegroundColour;
                outputLine++;
                Console.Write(deadlineText[i].Text);
            }
            Console.SetCursorPosition(0, 0);
        }

        /// <summary>
        /// Redraws the progress bar based on current settings.
        /// </summary>
        private void RedrawProgressBar()
        {
            lock (_progressBarLocker)
            {
                // We have a 2 spaces - one at each side of screen, 2 brackets as containers for the bar, 5 characters for percentage text, 1 space before that, 5 spaces after that and then the progress text at ProgressBarTextWidth characters.
                const int reservedSpaced = 2 + 2 + 5 + 1 + 5 + ProgressBarTextWidth;
                int remainingSpace = _windowWidth - reservedSpaced;

                // We draw this full width of the window in the row above function keys on a black background - if we're not even showing progress bar then we just want to blank that whole line out
                Console.SetCursorPosition(0, _windowHeight - 2);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (!_progressBarVisible || remainingSpace < 0)
                {
                    StringBuilder blankLine = new StringBuilder(_windowWidth);
                    for (int i = 0; i < _windowWidth; i++)
                        blankLine.Append(" ");
                    Console.Write(blankLine.ToString());
                    return;
                }

                // We know we can fit it, so let's calculate how much for each block
                decimal percentagePerBlock = 100m / remainingSpace;
                byte blocksToUse = (byte)Math.Round((_progressBarValue * 100m) / percentagePerBlock);
                StringBuilder percentageBar = new StringBuilder(_windowWidth);
                percentageBar.Append(" [");
                for (int i = 0; i < blocksToUse; i++)
                    percentageBar.Append("=");
                for (int i = blocksToUse; i < remainingSpace; i++)
                    percentageBar.Append(" ");
                percentageBar.Append("] ");

                // Add in right hand label
                StringBuilder percentageText = new StringBuilder(5);
                percentageText.Append(Math.Round(_progressBarValue * 100m, 1).ToString());
                percentageText.Append("%");
                while (percentageText.Length < 5)
                    percentageText.Append(" ");
                percentageBar.Append(percentageText.ToString());
                percentageBar.Append("     ");

                // Now draw additional text if present
                if (!string.IsNullOrWhiteSpace(_progressBarText))
                    percentageBar.Append((_progressBarText.Length < ProgressBarTextWidth ? _progressBarText : _progressBarText.Substring(0, ProgressBarTextWidth)));

                // Display this
                while (percentageBar.Length < _windowWidth)
                    percentageBar.Append(" ");
                Console.Write(percentageBar.ToString());

                // Go back and paint the indicator chevron in green instead of white if applicable
                if (blocksToUse != remainingSpace)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.SetCursorPosition(blocksToUse + 2, _windowHeight - 2);
                    if (_progressBarAnimationIndex == 0 || _progressBarAnimationIndex == 4)
                        Console.Write("|");
                    else if (_progressBarAnimationIndex == 1 || _progressBarAnimationIndex == 5)
                        Console.Write("/");
                    else if (_progressBarAnimationIndex == 2 || _progressBarAnimationIndex == 6)
                        Console.Write("-");
                    else
                        Console.Write("\\");
                }
            }
        }
        #endregion

        #region Public Display Control
        /// <summary>
        /// Hides the progress bar.
        /// </summary>
        public static void ProgressBarHide()
        {
            if (_applicationInstance == null)
                return;
            _applicationInstance._progressBarVisible = false;
            lock (_applicationInstance._redrawLocker)
                _applicationInstance._redrawProgressBarRequired = true;
        }

        /// <summary>
        /// Displays the progress bar with the specified settings.
        /// </summary>
        /// <param name="animationIndex">
        /// Which end-animation is currently shown on the progress bar end piece when it is animating - a value 0-7 inclusive.
        /// </param>
        /// <param name="value">
        /// The progress bar from - from 0 to 1.
        /// </param>
        /// <param name="text">
        /// The text to show at the right of the progress bar after the percentage text - a maximum of 10 characters.
        /// </param>
        public static void ProgressBarSetup(byte animationIndex, decimal value, string text)
        {
            if (_applicationInstance == null)
                return;
            lock (_applicationInstance._progressBarLocker)
            {
                _applicationInstance._progressBarVisible = true;
                _applicationInstance._progressBarAnimationIndex = animationIndex;
                _applicationInstance._progressBarValue = value;
                _applicationInstance._progressBarText = text;
                lock (_applicationInstance._redrawLocker)
                    _applicationInstance._redrawProgressBarRequired = true;
            }
        }

        /// <summary>
        /// Write a line to the current console UI.
        /// </summary>
        /// <param name="text">
        /// The text to draw.
        /// </param>
        /// <param name="foreground">
        /// The optional foreground colour of the text.
        /// </param>
        /// <param name="background">
        /// The optional background colour of the text.
        /// </param>
        public static void WriteLine(string text, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Blue)
        {
            if (_applicationInstance == null || !_applicationInstance._isAlive)
                Console.WriteLine(text);
            else
            {
                lock (_applicationInstance._textLocker)
                {
                    string[] split = text.Replace("\r\n", "\n").Split(new char[] { '\r', '\n' });
                    foreach (string textLine in split)
                        _applicationInstance._text.Add(new ColouredTextLine(textLine, foreground, background));
                    if (_applicationInstance._text.Count > 500)
                        _applicationInstance._text.RemoveRange(0, _applicationInstance._text.Count - 500);
                }
                lock (_applicationInstance._redrawLocker)
                    _applicationInstance._redrawTextRequired = true;
            }
        }

        /// <summary>
        /// Sets the header to display within the main text area on the border bars.
        /// </summary>
        /// <param name="text">
        /// The text to display.
        /// </param>
        public static void SetTextAreaHeader(string text)
        {
            if (_applicationInstance == null)
                return;
            lock (_applicationInstance._redrawLocker)
            {
                _applicationInstance._textAreaHeader = !string.IsNullOrWhiteSpace(text) ? text : null;
                _applicationInstance._redrawRequired = true;
            }
        }

        /// <summary>
        /// Adds deadline details.
        /// </summary>
        /// <param name="deadline">
        /// The deadline to add.
        /// </param>
        public static void DisplayDeadline(Deadline deadline)
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
            lock (_applicationInstance._redrawLocker)
                _applicationInstance._redrawDeadlinesRequired = true;
        }
        #endregion

        #region Helper Methods
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
            int maximumTextSize = _deadlineAreaWidth - 4;
            if (text.Length > maximumTextSize)
                textCut = textCut.Substring(0, maximumTextSize);
            string returnValue = "│ " + textCut;
            while (returnValue.Length < _deadlineAreaWidth - 1)
                returnValue += " ";
            return returnValue + "│";
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
            Console.TreatControlCAsInput = false;
            Logger.Debug("UI has terminated");
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Free up our used resources.
        /// </summary>
        public void Dispose()
        {
            _isAlive = false;
            if (_thread != null)
            {
                _thread.Join();
                _thread = null;
            }
            if (_applicationInstance == this)
                _applicationInstance = null;
        }
        #endregion
    }
}