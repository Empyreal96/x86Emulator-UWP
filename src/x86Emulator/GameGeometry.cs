using System.Runtime.InteropServices;

namespace x86Emulator
{
    public class GameGeometry
    {
        /// <summary>
        /// Nominal video width of game
        /// </summary>
        public uint BaseWidth;

        /// <summary>
        /// Nominal video height of game
        /// </summary>
        public uint BaseHeight;

        /// <summary>
        /// Maximum possible width of game
        /// </summary>
        public uint MaxWidth;

        /// <summary>
        /// Maximum possible height of game
        /// </summary>
        public uint MaxHeight;

        /// <summary>
        /// Nominal aspect ratio of game.
        /// If aspect_ratio is <= 0.0, an aspect ratio
        /// of base_width / base_height is assumed.
        /// A frontend could override this setting
        /// if desired
        /// </summary>
        public float AspectRatio;
    }
}
