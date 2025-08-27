namespace SnakeAlgorithm {
    public readonly struct SnakeBoard {
        public readonly int Width;
        public readonly int Height;

        public SnakeBoard(int width, int height) {
            // throw if width or height is less than 2
            if (width < 2) {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 2.");
            }
            if (height < 2) {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 2.");
            }
            Width = width;
            Height = height;
        }
    }
}