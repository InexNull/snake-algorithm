using SnakeAlgorithm;
using System.Diagnostics;

static class Program
{
    public static void Main(string[] args)
    {

        SnakeBoard board = new(10, 10);

        int startX, startY;

        startX = 0;
        startY = 0;

        Direction[] path = [
            Direction.Up, Direction.Right, Direction.Right, Direction.Up, Direction.Left, Direction.Left, Direction.Up,
            Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right,
            Direction.Up,
            Direction.Left, Direction.Left, Direction.Left, Direction.Left, Direction.Left, Direction.Left, Direction.Left, Direction.Left, Direction.Left,
            Direction.Up
        ];

        int snakeSize = path.Length + 1;

        var pathfinder = new BoardPathfinder(board);

        var sw = new Stopwatch();

        Console.WriteLine($"Searching...");
        sw.Start();
        Direction[] result = pathfinder.FindPath(startX, startY, path, 0, 0, out int opened, out int explored) ?? [];
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

        foreach (var boardText in PlaySnakeAnimation(path.Concat(result), startX, startY, snakeSize, board))
        {
            Console.Clear();
            Console.Write(boardText);
            Console.WriteLine($"Search completed in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Opened: {opened}, Explored: {explored}");
            Thread.Sleep(300);
        }
    }

    private static IEnumerable<string> PlaySnakeAnimation(IEnumerable<Direction> path, int x, int y, int snakeSize, SnakeBoard board)
    {
        Queue<(int x, int y)> body = new();

        foreach (var dir in path)
        {
            body.Enqueue((x, y));
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

            while (body.Count > snakeSize) body.Dequeue();

            char[] display = new char[(board.Width + 1) * board.Height];

            for (int i = 0; i < display.Length; i++) display[i] = ' '; // Fill with spaces
            for (int i = 0; i < board.Height; i++) display[(i + 1) * (board.Width + 1) - 1] = '\n'; // Fill newlines

            foreach (var (bx, by) in body)
            {
                display[(by * (board.Width + 1)) + bx] = '#';
            }

            yield return new string(display);
        }
    }
}