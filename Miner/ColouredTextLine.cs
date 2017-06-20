using System;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class represents a single line of text with colour attached.
    /// </summary>
    public class ColouredTextLine
    {
        #region Properties
        /// <summary>
        /// Gets the colour to display the text.
        /// </summary>
        public ConsoleColor ForegroundColour { get; private set; }

        /// <summary>
        /// Gets the background colour to use for this text.
        /// </summary>
        public ConsoleColor BackgroundColour { get; private set; }

        /// <summary>
        /// Gets the text to display.
        /// </summary>
        public string Text { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="text">
        /// The text to display.
        /// </param>
        /// <param name="foregroundColour">
        /// The colour to display the text.
        /// </param>
        /// <param name="backgroundColour">
        /// The background colour to use for this text.
        /// </param>
        public ColouredTextLine(string text, ConsoleColor foregroundColour, ConsoleColor backgroundColour)
        {
            ForegroundColour = foregroundColour;
            BackgroundColour = backgroundColour;
            Text = text;
        }
        #endregion
    }
}