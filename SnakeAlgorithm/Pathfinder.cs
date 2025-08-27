using System.Collections;
using System.Diagnostics;

namespace SnakeAlgorithm {
    public class BoardPathfinder(SnakeBoard board)
    {
        public readonly SnakeBoard Board = board;
        private readonly Random _random = new(board.Width ^ (board.Height << 16) ^ (board.Height >> 16)); // seed with board dimensions
        private readonly List<ulong[]> _zobristTable = [];

        private ulong GetSalt(int depth, Direction direction)
        {
            if (depth == _zobristTable.Count)
            {
                var newRow = new ulong[4];
                for (int i = 0; i < 4; i++)
                {
                    newRow[i] = ((ulong)(uint)_random.Next() << 32) | (uint)_random.Next();
                }
                _zobristTable.Add(newRow);
            }

            return _zobristTable[depth][(int)direction];
        }

        private class State(int x, int y, int tailX, int tailY, Direction[] path, int size, ulong cost)
        {
            public readonly Direction[] Path = path;
            public readonly int x = x, y = y;
            public readonly int tailX = tailX, tailY = tailY;
            public readonly int Size = size; // Size of snake 
            public readonly ulong Cost = cost;
            public override int GetHashCode() => (x, y, tailX, tailY, Size).GetHashCode();

            public virtual bool IsIdentical(State other, Direction[] initialMovement)
            {
                // Head position, tail position, size will quickly eliminate most non-matching states
                if (x != other.x || y != other.y || tailX != other.tailX || tailY != other.tailY || Size != other.Size)
                    return false;
                


                // Check that the last `Size` elements of the paths equal
                (var biggerPath, var smallerPath) = Path.Length > other.Path.Length ? (Path, other.Path) : (other.Path, Path);
                int maxBackSearch = Math.Min(Size, smallerPath.Length);
                // Check the ends of the path
                int i = 1;
                for (i = 1; i <= maxBackSearch; i++)
                {
                    if (biggerPath[^i] != smallerPath[^i])
                        return false;
                }
                // This is designed to check that two snake paths, assuming the same initialMovement, end in the same state.
                // What this condition sees is that:
                // - We already know the paths start at the same position
                // - We just checked that they took the same moves
                // - We know one of them entered the initialMovement
                // - Yet somehow, within the length of the snake, the other path took more moves and still ended up entering the initial path
                // - This is impossible for valid states9
                if (biggerPath.Length != smallerPath.Length && biggerPath.Length < Size)
                {
                    throw new InvalidOperationException("Mismatching path starting positions");
                }
                // If one path wasn't big enough, compare using the initial movement
                for (; i <= Size; i++)
                {
                    if (biggerPath[^i] != initialMovement[^(i - smallerPath.Length)])
                        return false;
                }
                // If both paths aren't big enough ... this will always be true, this can only happen if they match piece-for-piece
                // If the paths weren't quite literally the same, we would have 
                // for (; i <= Size; i++)
                // {
                //     // Both paths are too small, compare against each other
                //     if (initialMovement[^(i - maxBackSearch)] != initialMovement[^(i - maxBackSearch)])
                //         return false;
                // }

                return true;
            }

            public State Next(Direction direction, int newX, int newY, ulong cost)
            {
                Direction[] newPath = new Direction[Path.Length + 1];
                Array.Copy(Path, newPath, Path.Length);
                newPath[Path.Length] = direction;
                // Move the tail piece
                var tailMove = Path.Length == 0 ? direction : Path[0];
                (int newTailX, int newTailY) = tailMove switch
                {
                    Direction.Up => (tailX, tailY + 1),
                    Direction.Down => (tailX, tailY - 1),
                    Direction.Left => (tailX - 1, tailY),
                    Direction.Right => (tailX + 1, tailY),
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction")
                };
                // Move the head piece
                return new State(newX, newY, newTailX, newTailY, newPath, Size, cost);
            }
        }

        // Finds a path, using the path travelled as a dimension to allow "denying" certain moves from certain permutations of the body
        // Also means additional conditions can be added, mainly, adding a check that the snake isn't trapped and verify with this before each move to make the trip safely

        // TODO: This evaluates ALL paths including loops for example, consider trying to detect these early?
        internal Direction[]? FindPath(int startX, int startY, IEnumerable<Direction> path, int targetX, int targetY, out int cellsOpened, out int cellsExplored)
        {
            // For performance debug
            cellsOpened = 0;
            cellsExplored = 0;

            if (!WithinBounds(startX, startY))
                throw new ArgumentOutOfRangeException(nameof(startX), "Start is out of bounds");

            // Unpack path to array
            Direction[] initialPath = [.. path];

            // Tracks which cells are occupied due to the snake's initial positioning, and on which move it became occupied (negative if it started this way)
            Dictionary<(int x, int y), int> initialClosedSet = [];

            // Travel path to ensure it is valid and mark cells as occupied
            int x = startX, y = startY;
            for (int i = 0; i < initialPath.Length; i++)
            {
                var direction = initialPath[i];
                if (!initialClosedSet.TryAdd((x, y), i - initialPath.Length))
                {
                    throw new ArgumentException("Path intersects itself", nameof(path));
                }

                switch (direction)
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
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction");
                }

                if (!WithinBounds(x, y))
                    throw new ArgumentOutOfRangeException(nameof(path), "Path goes out of bounds");
            }

            if (!initialClosedSet.TryAdd((x, y), 0))
            {
                throw new ArgumentException("Path intersects itself", nameof(path));
            }

            ulong[] GenerateBfsHeuristic()
            {
                ulong GetCost(int cx, int cy, ulong current)
                {
                    if (!initialClosedSet.TryGetValue((cx, cy), out int occupiedUntil)) return current + 1;
                    return Math.Max(current + 1, (ulong)(occupiedUntil + initialPath.Length - 1));
                }
                // Run BFS from the target position that takes into consideration the starting snake position to create a more
                // accurate heuristic than manhattan distance.
                ulong[] bfsHeuristic = new ulong[Board.Width * Board.Height];
                BitArray closed = new(board.Width * Board.Height);
                PriorityQueue<(int x, int y), ulong> bfsQueue = new();
                bfsQueue.Enqueue((targetX, targetY), 0);

                while (bfsQueue.TryDequeue(out var current, out var dist))
                {

                    int index = current.x + current.y * Board.Width;
                    if (closed[index]) continue; // Already visited
                    closed[index] = true;
                    bfsHeuristic[index] = dist;

                    // Check neighbors
                    if (WithinBounds(current.x + 1, current.y))
                        bfsQueue.Enqueue((current.x + 1, current.y), GetCost(current.x + 1, current.y, dist));
                    if (WithinBounds(current.x - 1, current.y))
                        bfsQueue.Enqueue((current.x - 1, current.y), GetCost(current.x - 1, current.y, dist));
                    if (WithinBounds(current.x, current.y + 1))
                        bfsQueue.Enqueue((current.x, current.y + 1), GetCost(current.x, current.y + 1, dist));
                    if (WithinBounds(current.x, current.y - 1))
                        bfsQueue.Enqueue((current.x, current.y - 1), GetCost(current.x, current.y - 1, dist));
                }

                return bfsHeuristic;
            }

            ulong[] bfsHeuristic = GenerateBfsHeuristic();
            PriorityQueue<State, ulong> openSet = new();
            openSet.Enqueue(new State(x, y, x, y, [], initialPath.Length + 1, 0), 0); // +1 because the move list doesn't include our *first* cell
            cellsOpened++;
            // No two paths can ever visit the same node, revisiting is *not possible*
            // HashSet<ulong> closedSet = [];
            // Similarly, we do not need a gscore.
            // fScore is the only value that matters, and is stored in the openSet priority queue

            while (openSet.TryDequeue(out var current, out _))
            {
                cellsExplored++;
                if (current.x == targetX && current.y == targetY)
                {
                    // Reached the target, return the path
                    return current.Path;
                }

                // current.Path.Length is what move we're on, subtract the size for the last occupied space, +1 because the oldest tail block moves out of the way as we advance
                int occupiedCutoff = current.Path.Length - current.Size + 1;
                // Checks if a cell is blocked off by the snake body
                bool Occupied(int cx, int cy)
                {
                    if (initialClosedSet.TryGetValue((cx, cy), out int occupiedUntil))
                    {
                        // If the cell was occupied by the initial path, check if it is still occupied
                        if (occupiedUntil > occupiedCutoff)
                        {
                            return true;
                        }
                    }

                    int headX = current.x, headY = current.y;
                    // Don't check before the start of the path, that was done in the initial set and will be out of bounds here
                    int occupiedPositiveCutoff = Math.Max(0, occupiedCutoff);
                    // Follow the path backwards to see if the cell is occupied
                    for (int i = current.Path.Length - 1; i >= occupiedPositiveCutoff; i--)
                    {
                        var direction = current.Path[i];
                        switch (direction)
                        {
                            case Direction.Up:
                                headY--;
                                break;
                            case Direction.Down:
                                headY++;
                                break;
                            case Direction.Left:
                                headX++;
                                break;
                            case Direction.Right:
                                headX--;
                                break;
                            default:
                                // This should never happen
                                throw new UnreachableException();
                        }

                        if (headX == cx && headY == cy)
                            return true;
                    }

                    return false;
                }

                void TryOpen(Direction dir, int cx, int cy, ref int cellsOpened)
                {
                    // Keeps movement very large so smaller penalties can be added without impacting the length
                    // Basically small costs will act as tiebreakers
                    const ulong baseCostMultiplier = 1ul << 31;
                    const ulong nonHugCost = 1ul;

                    if (!WithinBounds(cx, cy) || Occupied(cx, cy))
                        return;

                    bool isHorizontal = dir is Direction.Left or Direction.Right;

                    // Get g score
                    var cost = current.Cost + baseCostMultiplier;

                    // Add micro costs to tidy path
                    if (isHorizontal)
                    {
                        bool leftClosed = !WithinBounds(current.x - 1, current.y) || Occupied(current.x - 1, current.y);
                        bool rightClosed = !WithinBounds(current.x + 1, current.y) || Occupied(current.x + 1, current.y);

                        // Reward taking paths that are up against itself or a wall (with a 1 point cost)
                        if (!(leftClosed ^ rightClosed))
                            cost += nonHugCost;
                    }
                    else
                    {
                        bool topClosed = !WithinBounds(current.x, current.y - 1) || Occupied(current.x, current.y - 1);
                        bool bottomClosed = !WithinBounds(current.x, current.y + 1) || Occupied(current.x, current.y + 1);

                        // Reward taking paths that are up against itself or a wall (with a 1 point cost)
                        if (!(topClosed ^ bottomClosed))
                            cost += nonHugCost;
                    }

                    // Make next
                    var next = current.Next(dir, cx, cy, cost);
                    // Enqueue with f score
                    openSet.Enqueue(next, cost + bfsHeuristic[cx + cy * Board.Width] * baseCostMultiplier);
                    cellsOpened++;
                }

                TryOpen(Direction.Up, current.x, current.y + 1, ref cellsOpened);
                TryOpen(Direction.Down, current.x, current.y - 1, ref cellsOpened);
                TryOpen(Direction.Left, current.x - 1, current.y, ref cellsOpened);
                TryOpen(Direction.Right, current.x + 1, current.y, ref cellsOpened);
            }

            return null; // No path found
        }

        internal bool WithinBounds(int x, int y)
        {
            return x >= 0 && x < Board.Width && y >= 0 && y < Board.Height;
        }

        internal static int ManhattanHeuristic(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }
    }
}