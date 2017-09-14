using System.Collections.Generic;
using Battleships.Player.Interface;
using System.Linq;


namespace BattleshipBot
{
    public class MyBot : IBattleshipsBot
    {
        private IGridSquare lastTarget;
        private static int _boardSize = 100;
        private Item[] battleMap;

        private List<int> enemyLeftLength;
        private System.Random rnd;

        private List<Orientation> possibleOri;
        private Orientation curOrientation;
        private State fightState;
        private IGridSquare firstHit;

        public IEnumerable<IShipPosition> GetShipPositions()
        {
            enemyLeftLength = new List<int> { 5, 4, 3, 3, 2 };
            lastTarget = null;
            fightState = State.Explore;
            rnd = new System.Random();
            battleMap = Enumerable.Repeat(Item.Fog, _boardSize).ToArray();
            return new List<IShipPosition>
                  {
                    GetShipPosition('B', 4, 'B', 8), // Aircraft Carrier
				    GetShipPosition('C', 2, 'F', 2), // Battleship
				    GetShipPosition('E', 6, 'G', 6), // Destroyer
				    GetShipPosition('H', 2, 'H', 4), // Submarine
				    GetShipPosition('I', 10, 'J', 10)  // Patrol boat
				  };
        }

        private static ShipPosition GetShipPosition(char startRow, int startColumn, char endRow, int endColumn)
        {
            return new ShipPosition(new GridSquare(startRow, startColumn), new GridSquare(endRow, endColumn));
        }

        public IGridSquare SelectTarget()
        {
            var nextTarget = GetNextTarget();
            lastTarget = nextTarget;
            return nextTarget;
        }

        private IGridSquare GetNextTarget()
        {
            IGridSquare target = GetRandomTarget();
            switch (fightState)
            {
                case State.Explore:
                    target = GetRandomTarget();
                    break;
                case State.FindingDirection:
                    target = GetNeighbourSquare(firstHit, this.possibleOri[0]);
                    break;
                case State.ContinueHit:
                    target = GetNeighbourSquare(lastTarget, curOrientation);
                    if (ShouldContinue(target))
                    {
                        curOrientation = OppositeOrientation(curOrientation);
                        target = GetNeighbourSquare(firstHit, curOrientation);
                        fightState = State.FoundOneEnd;
                    }
                    break;
                case State.FoundOneEnd:
                    curOrientation = OppositeOrientation(curOrientation);
                    target = GetNeighbourSquare(firstHit, curOrientation);
                    if (ShouldContinue(target))
                    {
                        target = GetRandomTarget();
                        fightState = State.Explore;
                    }
                    break;
                case State.ContinueToAnotherENd:
                    target = GetNeighbourSquare(lastTarget, curOrientation);
                    if (ShouldContinue(target))
                    {
                        target = GetRandomTarget();
                        fightState = State.Explore;
                    }
                    break;
            }

            return target;

        }

        private bool ShouldContinue(IGridSquare target)
        {
            return IsOffBoard(target) || battleMap[GridToMap(target)] == Item.Sea;
        }

        public void HandleShotResult(IGridSquare square, bool wasHit)
        {
            int idx = GridToMap(square);
            if (wasHit)
            {
                battleMap[idx] = Item.Ship;
                switch (fightState)
                {
                    case State.Explore:
                        firstHit = square;
                        this.possibleOri = GetPossibleOrientation(square);
                        fightState = State.FindingDirection;
                        break;

                    case State.FindingDirection:
                        fightState = State.ContinueHit;
                        curOrientation = this.possibleOri[0];
                        break;
                    case State.FoundOneEnd:
                        fightState = State.ContinueToAnotherENd;
                        break;
                    default:
                        break;
                }

            }
            else
            {
                battleMap[idx] = Item.Sea;
                switch (fightState)
                {
                    case State.FindingDirection:
                        possibleOri.RemoveAt(0);
                        break;
                    case State.ContinueHit:
                        fightState = State.FoundOneEnd;
                        break;
                    case State.FoundOneEnd:
                        fightState = State.Explore;
                        break;
                    case State.ContinueToAnotherENd:
                        fightState = State.Explore;
                        break;
                    default:
                        break;
                }

            }
            //Ignore whether we're successful
        }

        public void HandleOpponentsShot(IGridSquare square)
        {
            // Ignore what our opponent does
        }

        public string Name => "Not a simple Bot";

        private IGridSquare GetRandomTarget()
        {
            List<IGridSquare> candites = new List<IGridSquare> { };
            for (int i = 0; i < _boardSize; i++)
            {
                if (battleMap[i] == Item.Fog)
                {
                    IGridSquare sq = MapToGrid(i);
                    if (IsGoodPick(sq))
                    {
                        candites.Add(sq);
                    }
                }
            }

            int pick = rnd.Next(0, candites.Count);
            IGridSquare res = candites[pick];
            return res;
        }

        private List<Orientation> GetPossibleOrientation(IGridSquare square)
        {
            List<Orientation> result = new List<Orientation>(){
                Orientation.North,Orientation.East,Orientation.South,Orientation.West
            };
            
            return RemoveImpossibleOrientation(square, result);
        }


        private IGridSquare GetNeighbourSquare(IGridSquare g, Orientation o, int step = 1)
        {
            int col = g.Column;
            char row = g.Row;
            switch (o)
            {
                case Orientation.East:
                    col += step;
                    break;
                case Orientation.West:
                    col -= step;
                    break;
                case Orientation.North:
                    row = (char)(row - step);
                    break;
                case Orientation.South:
                    row = (char)(row + step);
                    break;
            }
            GridSquare result = new GridSquare(row, col);
            return result;

        }

        private int GridToMap(IGridSquare g)
        {
            return (int)(g.Row - 'A') * 10 + g.Column - 1;
        }

        private IGridSquare MapToGrid(int square)
        {
            char row = (char)('A' + square / 10);
            int column = square % 10 + 1;
            return new GridSquare(row, column);
        }


        private bool IsOffBoard(IGridSquare g)
        {
            if (g.Row < 'A' || g.Row > 'J' || g.Column > 10 || g.Column < 1)
            {
                return true;
            }
            return false;
        }


        private List<Orientation> RemoveImpossibleOrientation(IGridSquare square, List<Orientation> os)
        {

            List<Orientation> result = new List<Orientation>();
            for (int i = 0; i < os.Count; i++)
            {
                Orientation ori = os.ElementAt(i);
                IGridSquare poke = GetNeighbourSquare(square, ori);
                if (!IsOffBoard(poke))
                {
                    int idx = GridToMap(poke);
                    if (battleMap[idx] == Item.Fog)
                    {
                        result.Add(ori);
                    }
                }

            }
            return result;
        }

        private Orientation OppositeOrientation(Orientation o)
        {
            switch (o)
            {
                case Orientation.South:
                    return Orientation.North;
                case Orientation.North:
                    return Orientation.South;
                case Orientation.West:
                    return Orientation.East;
                default:
                    return Orientation.West;
            }
        }

        private int GetSquareRank(IGridSquare square)
        {
            // no adjacent ship when pick random target
            List<IGridSquare> surroundings = GetAllSurroundings(square);
            foreach (IGridSquare sur in surroundings)
            {
                if (!IsOffBoard(sur))
                {
                    int idx = GridToMap(sur);
                    if (battleMap[idx] == Item.Ship)
                    {
                        return 0;
                    }
                }
            }
            
            List<IGridSquare> immediate = GetAllImmediateNeighbour(square);
            int count = 0;
            foreach (IGridSquare sur in immediate)
            {
                int idx = GridToMap(sur);
                if (IsOffBoard(sur))
                {
                    count++;
                }
                else if (battleMap[idx] == Item.Sea)
                {
                    count++;
                }
            }
            return 5 - count;
        }

        private bool IsGoodPick(IGridSquare square)
        {
            // no adjacent ship when pick random target
            List<IGridSquare> surroundings = GetAllSurroundings(square);
            foreach (IGridSquare sur in surroundings)
            {
                if (!IsOffBoard(sur))
                {
                    int idx = GridToMap(sur);
                    if (battleMap[idx] == Item.Ship)
                    {
                        return false;
                    }
                }
            }

            //don't pick if there are three surrounding sea/boarder
            List<IGridSquare> immediate = GetAllImmediateNeighbour(square);
            int count = 0;
            foreach (IGridSquare sur in immediate)
            {
                int idx = GridToMap(sur);
                if (IsOffBoard(sur))
                {
                    count++;
                }
                else if (battleMap[idx] != Item.Fog)
                {
                    count++;
                }
            }
            if (count > 1)
            {
                return false;
            }

            return true;
        }

        private List<IGridSquare> GetAllSurroundings(IGridSquare center)
        {

            int[] surroundingIdx = { -11, -10, -9, -1, +1, +9, +10, +11 };
            return GetAll(center, surroundingIdx);
        }

        private List<IGridSquare> GetAllImmediateNeighbour(IGridSquare center)
        {
            int[] surroundingIdx = { -10, -1, +1, +10 };
            return GetAll(center, surroundingIdx);
        }

        private List<IGridSquare> GetAll(IGridSquare center, int[] idxes)
        {
            List<IGridSquare> result = new List<IGridSquare>();
            int cenIdx = GridToMap(center);
            int[] surroundingIdx = idxes;
            for (int i = 0; i < idxes.Length; i++)
            {
                int idx = cenIdx + surroundingIdx[i];
                result.Add(MapToGrid(idx));
            }
            return result;
        }

    }

    public enum Orientation
    {
        North = 0,
        South = 1,
        West = 2,
        East = 3,
        Unkown
    }

    public enum Item
    {
        Fog,
        Ship,
        Sea
    }

    public enum State
    {
        Explore,
        FindingDirection,
        ContinueHit,
        FoundOneEnd,
        ContinueToAnotherENd,
        Finished
    }
}
