using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics;

namespace SnakeAlgorithm {
    public static class BoardPathfinder
    {
        public sealed class BaseSnake
        {
            // Represents possible movements
            public sealed class SnakeMove
            {
                public readonly int Depth;
                public readonly BaseSnake Base;
                public readonly SnakeMove? Previous;


                public readonly Direction value;
                public readonly int x;
                public readonly int y;
                public ulong gScore;

                public SnakeMove(BaseSnake baseBoard, Direction direction)
                {
                    Depth = 0;
                    Base = baseBoard;
                    Previous = null;
                    value = direction;
                    (x, y) = direction switch
                    {
                        Direction.Up => (baseBoard.x, baseBoard.y + 1),
                        Direction.Down => (baseBoard.x, baseBoard.y - 1),
                        Direction.Left => (baseBoard.x - 1, baseBoard.y),
                        Direction.Right => (baseBoard.x + 1, baseBoard.y),
                        _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction")
                    };
                }

                public SnakeMove(SnakeMove previous, Direction direction, ulong travelCost)
                {
                    Depth = previous.Depth + 1;
                    Base = previous.Base;
                    Previous = previous;
                    value = direction;
                    gScore = travelCost;
                    (x, y) = direction switch
                    {
                        Direction.Up => (previous.x, previous.y + 1),
                        Direction.Down => (previous.x, previous.y - 1),
                        Direction.Left => (previous.x - 1, previous.y),
                        Direction.Right => (previous.x + 1, previous.y),
                        _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction")
                    };
                }

                public bool IsLegal()
                {
                    // Bounds check                    
                    if (x < 0 || x >= Base.Board.Width || y < 0 || y >= Base.Board.Height)
                        return false;

                    // Quick check if our base body is in the way
                    if (Base.OccupiedMap.TryGetValue((x, y), out int occupiedUntil) && occupiedUntil > Depth)
                        return false;

                    // Reverse check back down the body
                    for (var piece = Previous; piece != null && piece.Depth > Depth - Base.Size; piece = piece.Previous)
                        if (piece.x == x && piece.y == y)
                            return false;

                    return true;
                }

                public bool IsPositionLegal(int cx, int cy)
                {
                    if (cx < 0 || cx >= Base.Board.Width || cy < 0 || cy >= Base.Board.Height)
                        return false;

                    if (Base.OccupiedMap.TryGetValue((cx, cy), out int occupiedUntil) & occupiedUntil > Depth + 1)
                        return false;

                    for (var piece = Previous; piece != null && piece.Depth > Depth + 1 - Base.Size; piece = piece.Previous)
                        if (piece.x == cx && piece.y == cy)
                            return false;

                    return true;
                }

                public bool IsMoveLegal(Direction direction)
                {
                    (int cx, int cy) = direction switch
                    {
                        Direction.Up => (x, y + 1),
                        Direction.Down => (x, y - 1),
                        Direction.Left => (x - 1, y),
                        Direction.Right => (x + 1, y),
                        _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction")
                    };

                    return IsPositionLegal(cx, cy);
                }

                public Direction[] MakePath()
                {
                    Direction[] path = new Direction[Depth + 1];
                    var current = this;
                    for (int i = Depth; i >= 0; i--)
                    {
                        path[i] = current.value;
                        current = current.Previous!;
                    }
                    return path;
                }
            }
            public readonly SnakeBoard Board;
            private (int x, int y)[] _body;
            private Direction[] _path;
            private readonly int _size;

            public IReadOnlyList<(int x, int y)> Body => _body;
            public IReadOnlyList<Direction> Path => _path;
            public readonly FrozenDictionary<(int x, int y), int> OccupiedMap;
            public int Size => _size;
            public readonly int x, y;

            public bool WithinBounds(int x, int y)
            {
                return x >= 0 && x < Board.Width && y >= 0 && y < Board.Height;
            }

            public BaseSnake(SnakeBoard board, params (int x, int y)[] body)
            {
                if (body.Length < 1)
                    throw new ArgumentException("Snake must have at least one body part", nameof(body));

                Board = board;

                _size = body.Length;
                _body = [.. body];
                _path = new Direction[body.Length - 1];

                Dictionary<(int x, int y), int> occupiedMap = [];

                for (int i = 0; i < _path.Length; i++)
                {
                    int yOffset = _body[i + 1].y - _body[i].y;
                    _path[i] = yOffset switch
                    {
                        1 => Direction.Up,
                        -1 => Direction.Down,
                        0 when _body[i + 1].x - _body[i].x == 1 => Direction.Right,
                        0 when _body[i + 1].x - _body[i].x == -1 => Direction.Left,
                        _ => throw new ArgumentOutOfRangeException(nameof(body), "Consecutive nodes are not connected")
                    };


                    if (!occupiedMap.TryAdd(_body[i], i))
                        throw new ArgumentException("Body intersects itself", nameof(body));
                }

                if (!occupiedMap.TryAdd(_body[^1], _body.Length - 1))
                    throw new ArgumentException("Body intersects itself", nameof(body));

                OccupiedMap = occupiedMap.ToFrozenDictionary();

                (x, y) = _body[^1];
            }

            public ulong[] GenerateDijkstraHeuristic(int targetX, int targetY)
            {
                int width = Board.Width;
                int height = Board.Height;
                ulong[] heuristic = new ulong[width * height];
                BitArray seen = new(width * height);

                PriorityQueue<(int x, int y), ulong> queue = new();
                queue.Enqueue((targetX, targetY), 0); // Start from the target
                seen.Set(targetX + targetY * width, true);

                while (queue.TryDequeue(out var current, out var dist))
                {
                    heuristic[current.x + current.y * width] = dist;
                    void TryOpen(int x, int y)
                    {
                        if (!WithinBounds(x, y)) return;

                        int index = x + y * width;
                        if (seen.Get(index)) return; // Already visited

                        ulong cost = dist + 1;
                        // If the cell is occupied by the snake, we cannot reach it until at minimum the turn it moves on from the cell
                        if (OccupiedMap.TryGetValue((x, y), out int occupiedUntil) && (ulong)occupiedUntil > cost)
                            cost = (ulong)occupiedUntil;

                        queue.Enqueue((x, y), cost);
                        seen.Set(index, true);
                    }

                    TryOpen(current.x + 1, current.y);
                    TryOpen(current.x - 1, current.y);
                    TryOpen(current.x, current.y + 1);
                    TryOpen(current.x, current.y - 1);
                }

                return heuristic;
            }

            public bool IsMoveLegal(Direction direction)
            {
                // Moved position
                (int cx, int cy) = direction switch
                {
                    Direction.Up => (x, y + 1),
                    Direction.Down => (x, y - 1),
                    Direction.Left => (x - 1, y),
                    Direction.Right => (x + 1, y),
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction")
                };

                if (cx < 0 || cx >= Board.Width || cy < 0 || cy >= Board.Height)
                    return false;

                if (OccupiedMap.TryGetValue((cx, cy), out int occupiedUntil) && occupiedUntil > 0)
                    return false;

                return true;
            }
        }


        // Finds a path, using the path travelled as a dimension to allow "denying" certain moves from certain permutations of the body
        // Also means additional conditions can be added, mainly, adding a check that the snake isn't trapped and verify with this before each move to make the trip safely

        // TODO: This evaluates ALL paths including loops for example, consider trying to detect these early?
        public static Direction[]? FindPath(BaseSnake snake, int targetX, int targetY, out int cellsOpened, out int cellsExplored)
        {
            // For performance debug
            cellsOpened = 0;
            cellsExplored = 0;

            int width = snake.Board.Width;

            ulong[] hScoreMap = snake.GenerateDijkstraHeuristic(targetX, targetY);
            PriorityQueue<BaseSnake.SnakeMove, ulong> openSet = new();

            // Open first 4 cells
            if (snake.IsMoveLegal(Direction.Up))
            {
                openSet.Enqueue(new BaseSnake.SnakeMove(snake, Direction.Up), hScoreMap[snake.x + (snake.y + 1) * width]);
                cellsOpened++;
            }

            if (snake.IsMoveLegal(Direction.Down))
            {
                openSet.Enqueue(new BaseSnake.SnakeMove(snake, Direction.Down), hScoreMap[snake.x + (snake.y - 1) * width]);
                cellsOpened++;
            }

            if (snake.IsMoveLegal(Direction.Left))
            {
                openSet.Enqueue(new BaseSnake.SnakeMove(snake, Direction.Left), hScoreMap[snake.x - 1 + snake.y * width]);
                cellsOpened++;
            }

            if (snake.IsMoveLegal(Direction.Right))
            {
                openSet.Enqueue(new BaseSnake.SnakeMove(snake, Direction.Right), hScoreMap[snake.x + 1 + snake.y * width]);
                cellsOpened++;
            }

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
                    return current.MakePath();
                }

                void TryOpen(Direction dir, int cx, int cy, ref int cellsOpened)
                {
                    // Keeps movement very large so smaller penalties can be added without impacting the length
                    // Basically small costs will act as tiebreakers
                    const ulong baseCostMultiplier = 1ul << 31;
                    const ulong nonHugCost = 1ul;

                    if (!current.IsMoveLegal(dir))
                        return;

                    bool isHorizontal = dir is Direction.Left or Direction.Right;

                    // Get g score
                    var cost = current.gScore + baseCostMultiplier;

                    // Add micro costs to tidy path
                    if (isHorizontal)
                    {
                        bool leftClosed = current.IsPositionLegal(current.x - 1, current.y);
                        bool rightClosed = current.IsPositionLegal(current.x + 1, current.y);

                        // Reward taking paths that are up against itself or a wall (with a 1 point cost)
                        if (!(leftClosed ^ rightClosed))
                            cost += nonHugCost;
                    }
                    else
                    {
                        bool topClosed = current.IsPositionLegal(current.x, current.y - 1);
                        bool bottomClosed = current.IsPositionLegal(current.x, current.y + 1);

                        // Reward taking paths that are up against itself or a wall (with a 1 point cost)
                        if (!(topClosed ^ bottomClosed))
                            cost += nonHugCost;
                    }

                    // Make next
                    var next = new BaseSnake.SnakeMove(current, dir, cost);
                    // Enqueue with f score
                    openSet.Enqueue(next, cost + hScoreMap[cx + cy * width] * baseCostMultiplier);
                    cellsOpened++;
                }

                TryOpen(Direction.Up, current.x, current.y + 1, ref cellsOpened);
                TryOpen(Direction.Down, current.x, current.y - 1, ref cellsOpened);
                TryOpen(Direction.Left, current.x - 1, current.y, ref cellsOpened);
                TryOpen(Direction.Right, current.x + 1, current.y, ref cellsOpened);
            }

            return null; // No path found
        }
    }
}