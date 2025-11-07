using SnakeAlgorithm;
using System.Diagnostics;
using static SnakeAlgorithm.BoardPathfinder;

static class Program
{
    public static void Main(string[] args)
    {
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

        // It is at this point I realized my Dijstra's heuristic is not admissable, However, fixing it is orders of magnitudes slower,
        // so I'm just going to accept slightly less accurate pathing for the efficiency.

        // After the easy safety check (ignores the concept of apples, simple "can I move" checks):
        //     Search completed in 299ms
        //     Opened: 52158, Explored: 27582

        // There was no improvement in searching which I was somewhat suprised by, I figured it might circle itself blindly
        // and notice a few cells earlier it was a bad idea than normal. Maybe this is in part due to the Dijstra weight.


        int width = ReadInt("Board Width (3-20): ", 3, 20);
        int height = ReadInt("Board Height (3-20): ", 3, 20);
        int maxLength = width * height / 2; // It could handle something greater, however, the way it spawns the snake is suboptimal
        int length = ReadInt($"Snake Length (4-{maxLength}): ", 4, maxLength);

        Console.WriteLine("Starting!");
        Thread.Sleep(150);
        foreach (var boardText in InfiniteSnake(width, height, length))
        {
            Console.Clear();
            Console.Write(boardText);
            Console.WriteLine("Press CTRL-C to stop.");
            Thread.Sleep(150);
        }
        Console.WriteLine("Stopping!");
    }

    private static int ReadInt(string prompt, int min, int max)
    {
        Console.Write(prompt);
        while (true)
        {
            string? input = Console.ReadLine();
            if (int.TryParse(input, out int value) && value >= min && value <= max)
                return value;
            Console.Write($"Please enter an integer between {min} and {max}: ");
        }
    }

    private static IEnumerable<string> InfiniteSnake(int width, int height, int snakeLength)
    {
        Random rand = new Random();
        if (snakeLength > width * height) throw new ArgumentOutOfRangeException(nameof(snakeLength), "Snake length cannot be larger than the board size");

        IEnumerable<(int x, int y)> GenerateBody()
        {
            int len = snakeLength;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (len == 0) yield break;
                    yield return (y % 2 == 0 ? x : width - 1 - x, y);
                    len--;
                }
            }
        }

        SnakeBoard board = new(width, height);
        BaseSnake snake = new(board, [.. GenerateBody()]);
        
        Queue<(int x, int y)> body = new();
        foreach (var pos in snake.Body) {
            body.Enqueue(pos);
        }

        // Pick first path
        (int x, int y) target = snake.PickRandomCell(rand);
        Task<BaseSnake.SnakeMove?> pathTask = Task.Run(() => FindPath(snake, target.x, target.y, out int opened, out int explored, Environment.ProcessorCount));
        
        char[] displayTemplate = new char[(board.Width + 1) * board.Height];
        for (int i = 0; i < displayTemplate.Length; i++) displayTemplate[i] = ' '; // Fill with spaces
        for (int i = 0; i < board.Height; i++) displayTemplate[(i + 1) * (board.Width + 1) - 1] = '\n'; // Fill newlines

        char[] display = new char[displayTemplate.Length];


        int headX = 0, headY = 0;
        while (true)
        {
            headX = snake.Body[^1].x;
            headY = snake.Body[^1].y;
            var path = pathTask.Result;
            if (path == null)
            {
                break;
            }
            // Move the snake and start the next search
            var ogSnake = snake;
            snake = path.BuildSnake();
            (int x, int y) newTarget = snake.PickRandomCell(rand);
            pathTask = Task.Run(() => FindPath(snake, newTarget.x, newTarget.y, out int opened, out int explored));

            foreach (var move in path.MakePath())
            {
                switch (move)
                {
                    case Direction.Up:
                        headY++;
                        break;
                    case Direction.Down:
                        headY--;
                        break;
                    case Direction.Left:
                        headX--;
                        break;
                    case Direction.Right:
                        headX++;
                        break;
                }

                string Draw()
                {
                    Array.Copy(displayTemplate, display, displayTemplate.Length);

                    // mark target
                    display[((board.Height - target.y - 1) * (board.Width + 1)) + target.x] = '\u00B7'; // Center dot

                    foreach (var (bx, by) in body)
                    {
                        display[((board.Height - by - 1) * (board.Width + 1)) + bx] = '#';
                    }
                    Console.WriteLine(snake.Size);
                    return new string(display);
                }

                body.Dequeue();
                yield return Draw();
                body.Enqueue((headX, headY));
                yield return Draw();
            }

            target = newTarget;
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
        // yield return new string(display);


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