using SnakeAlgorithm;
using System.Diagnostics;

static class Program
{
    public static void Main(string[] args)
    {

        SnakeBoard board = new(11, 10);

        BoardPathfinder.BaseSnake snake = new(board, (0, 0), (0, 1), (1, 1), (2, 1), (2, 2), (1, 2), (0, 2),
            (0, 3), (1, 3), (2, 3), (3, 3), (4, 3), (5, 3), (6, 3), (7, 3), (8, 3), (9, 3),
            (9, 4), (8, 4), (7, 4), (6, 4), (5, 4), (4, 4), (3, 4), (2, 4), (1, 4), (0, 4)
        );

        var sw = new Stopwatch();

        Console.WriteLine($"Searching...");
        sw.Start();
        Direction[] result = BoardPathfinder.FindPath(snake, 0, 0, out int opened, out int explored) ?? [];
        sw.Stop();

        // On a 10x10 pattern where the snake cuts itself off:
        // Before BFS heuristic:
        //     Search completed in 5019 ms
        //     Opened: 5501987, Explored: 3522046
        // After BFS heuristic (improves self-awareness on its body's initial positioning, huge speed and efficiency bonus):
        //     Search completed in 58 ms
        //     Opened: 53844, Explored: 28465
        // After self-hugging bonus (makes cleaner patterns, was a slight efficiency improvement at the cost of speed):
        //     Search completed in 93 ms
        //     Opened: 52158, Explored: 27582
        //     That 3.1% decrease in explored cells might matter more than the 1.33µs cost per exploration once "safety checks" are added.
        // After full rewrite (marginally faster) (I'm not entierly sure the paths are the same)
        //     Search completed in 75ms
        //     Opened: 51383, Explored: 27582

        foreach (var boardText in PlaySnakeAnimation(snake.Body, result, board))
        {
            Console.Clear();
            Console.Write(boardText);
            Console.WriteLine($"Search completed in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Opened: {opened}, Explored: {explored}");
            Thread.Sleep(300);
        }
    }

    private static IEnumerable<string> PlaySnakeAnimation(IEnumerable<(int x, int y)> path, IEnumerable<Direction> moves, SnakeBoard board)
    {
        int x = 0, y = 0;
        Queue<(int x, int y)> body = new();
        foreach (var pos in path)
        {
            body.Enqueue(pos);
            x = pos.x;
            y = pos.y;
        }

        char[] baseDisplay = new char[(board.Width + 1) * board.Height];
        for (int i = 0; i < baseDisplay.Length; i++) baseDisplay[i] = ' '; // Fill with spaces
        for (int i = 0; i < board.Height; i++) baseDisplay[(i + 1) * (board.Width + 1) - 1] = '\n'; // Fill newlines

        char[] display = new char[(board.Width + 1) * board.Height];

        Array.Copy(baseDisplay, display, baseDisplay.Length);
        foreach (var (bx, by) in body)
        {
            display[((board.Height - by - 1) * (board.Width + 1)) + bx] = '#';
        }
        yield return new string(display);


        foreach (var dir in moves)
        {
            switch (dir)
            {
                case Direction.Up:
                    y++;
                    break;
                case Direction.Down:
                    y--;
                    break;
                case Direction.Left:
                    x--;
                    break;
                case Direction.Right:
                    x++;
                    break;
            }
            body.Enqueue((x, y));
            body.Dequeue();

            Array.Copy(baseDisplay, display, baseDisplay.Length);
            foreach (var (bx, by) in body)
            {
                display[((board.Height - by - 1) * (board.Width + 1)) + bx] = '#';
            }
            yield return new string(display);
        }
    }
}