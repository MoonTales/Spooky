namespace System
{
    /// <summary>
    /// a static class used to hold all of the types used throughout the project:
    /// including but not limted to: scruts, enums, and other type definitions.
    /// </summary>
    public static class Types
    {
        /// <summary>
        /// an enum representing the different states of a game.
        /// </summary>
        public enum GameState
        {
            MainMenu,
            Gameplay,
            Paused,
            GameOver,
            Victory
        }
    }
}
