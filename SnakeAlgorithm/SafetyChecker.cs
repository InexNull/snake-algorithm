namespace SnakeAlgorithm
{
    using System.ComponentModel;
    using static BoardPathfinder;
    public static class SafetyChecker
    {
        public static bool IsSafeAfterApple(BaseSnake.SnakeMove currentMove) =>
            IsSafeAfterApple(currentMove, currentMove.Depth);

        public static bool IsSafeAfterApple(BaseSnake.SnakeMove currentMove, int baseDepth)
        {
            throw new NotImplementedException();
        }

        public static bool IsSafe(BaseSnake.SnakeMove currentMove, int appleX=-1, int appleY=-1)
        {
            // Moves use a "travel cost" when pathfinding, we don't care about this value for our purposes
            const ulong unusedTravelCost = ulong.MaxValue;
            BaseSnake snake = currentMove.Base;

            // If this *is* the apple capture, move to an IsSafeAfterApple check
            if (currentMove.x == appleX && currentMove.y == appleY)
            {
                if (currentMove.Previous != null)
                {
                    return IsSafeAfterApple(new BaseSnake.SnakeMove(currentMove.Previous, currentMove.value, unusedTravelCost, true), currentMove.Depth);
                }
                else
                {
                    return IsSafeAfterApple(new BaseSnake.SnakeMove(snake, currentMove.value, true), currentMove.Depth);
                }
            }

            // Does DFS to find a path at least as long as the snake, falls back to IsSafeAfterApple if it has to take the apple
            int baseDepth = currentMove.Depth;

            PriorityQueue<BaseSnake.SnakeMove, int> appleCaptures = new(); // Sorted by biggest depth first so there's less to check for the apple
            Stack<BaseSnake.SnakeMove> open = new(); // Always pull the latest/deepest because there's no advantage to searching uniformly

            open.Push(currentMove);

            while (open.Count > 0)
            {
                BaseSnake.SnakeMove move = open.Pop();

                // We found a walk of at least the snake's length
                if (move.Depth >= baseDepth + snake.Size)
                {
                    return true;
                }

                void TryOpenMove(Direction direction, int cx, int cy)
                {
                    if (cx == appleX && cy == appleY)
                    {
                        var next = new BaseSnake.SnakeMove(move, direction, unusedTravelCost, true);
                        // Queue the deepest captures sooner and soonest captures later since we want to run lighter verifications first
                        appleCaptures.Enqueue(next, -next.Depth);
                    }
                    else if (move.IsMoveLegal(direction))
                    {
                        open.Push(new BaseSnake.SnakeMove(move, direction, unusedTravelCost, false));
                    }
                }

                TryOpenMove(Direction.Up, move.x, move.y + 1);
                TryOpenMove(Direction.Down, move.x, move.y - 1);
                TryOpenMove(Direction.Left, move.x - 1, move.y);
                TryOpenMove(Direction.Right, move.x + 1, move.y);
            }

            return false;

            while (appleCaptures.Count > 0)
            {
                var capture = appleCaptures.Dequeue();
                if (IsSafeAfterApple(capture, baseDepth)) return true;
            }

            return false;
        }

    }
}