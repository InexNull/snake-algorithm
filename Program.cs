using SnakeAlgorithm;
using System.Diagnostics;

static class Program
{
    public static void Main(string[] args)
    {

        SnakeBoard board = new(10, 10);

        BoardPathfinder.BaseSnake snake = new(board, (0, 0), (0, 1), (1, 1), (2, 1), (2, 2), (1, 2), (0, 2),
            (0, 3), (1, 3), (2, 3), (3, 3), (4, 3), (5, 3), (6, 3), (7, 3), (8, 3), (9, 3),
            (9, 4), (8, 4), (7, 4), (6, 4), (5, 4), (4, 4), (3, 4), (2, 4), (1, 4), (0, 4)
        );

        // BoardPathfinder.BaseSnake snake = new(board, (1, 1), (0, 1), (0, 2));

        var sw = new Stopwatch();

        Console.WriteLine($"Searching...");
        sw.Start();
        Direction[] result = BoardPathfinder.FindPath(snake, 0, 0, out int opened, out int explored) ?? [];
        sw.Stop();
        Console.WriteLine($"Search completed in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Opened: {opened}, Explored: {explored}");

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
        // After full rewrite (marginally faster)
        //     Search completed in 75ms
        //     Opened: 51383, Explored: 27582

        foreach (var dir in result)
        {
            Console.Write(dir switch
            {
                Direction.Up => "U",
                Direction.Right => "R",
                Direction.Down => "D",
                Direction.Left => "L",
                _ => "?"
            });
        }

        foreach (var boardText in PlaySnakeAnimation(snake.Path.Concat(result), snake.Body[0].x, snake.Body[0].y, snake.Size, board))
        {
            Console.Clear();
            Console.Write(boardText);
            Console.WriteLine($"Search completed in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Opened: {opened}, Explored: {explored}");
            Thread.Sleep(300);
        }
    }

    // This is disgusting, TODO: replace me o.o
    private static IEnumerable<string> PlaySnakeAnimation(IEnumerable<Direction> path, int x, int y, int snakeSize, SnakeBoard board)
    {
        Queue<(int x, int y)> body = new();

        body.Enqueue((x, y));
        foreach (var dir in path)
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

            while (body.Count > snakeSize) body.Dequeue();

            char[] display = new char[(board.Width + 1) * board.Height];

            for (int i = 0; i < display.Length; i++) display[i] = ' '; // Fill with spaces
            for (int i = 0; i < board.Height; i++) display[(i + 1) * (board.Width + 1) - 1] = '\n'; // Fill newlines

            foreach (var (bx, by) in body)
            {
                Console.WriteLine($"Drawing at {bx}, {by}");
                display[((board.Height - by - 1) * (board.Width + 1)) + bx] = '#';
            }

            yield return new string(display);
        }
    }
}