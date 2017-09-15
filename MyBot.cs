using System;
using System.Collections.Generic;
using System.IO;
using Battleships.Player.Interface;
using System.Linq;
using System.Threading;
using System.Xml.Schema;


namespace BattleshipBot
{
    public class MyBot : IBattleshipsBot
    {
        private IGridSquare lastTarget;
        private static int _boardSize = 100;
        private Item[] battleMap;
        private int[] likeliHood = Enumerable.Repeat(0, _boardSize).ToArray();

        private List<int> enemyLeftLength;
        private System.Random rnd;

        private List<Orientation> possibleOri;
        private Orientation curOrientation;
        private State fightState;
        private IGridSquare firstHit;
        private int lengthCount;

        private int gameCount = 0;

        public IEnumerable<IShipPosition> GetShipPositions()
        {
            enemyLeftLength = new List<int> { 5, 4, 3, 3, 2 };
            lastTarget = null;
            fightState = State.Explore;
            rnd = new Random();
            battleMap = Enumerable.Repeat(Item.Fog, _boardSize).ToArray();
            lengthCount = 0;
            gameCount++;
            if (gameCount > 5)
            {
                gameCount = 0;
                likeliHood = Enumerable.Repeat(0, _boardSize).ToArray();
            }

            var airCrafts = new List<IShipPosition>()
            {
                //GetShipPosition('A', 4, 'A', 8),
                //GetShipPosition('A', 1, 'A', 5),
                //GetShipPosition('A', 5, 'A', 9),
                GetShipPosition('B', 6, 'B', 10),
                GetShipPosition('B', 5, 'B', 9),

            };


            var battleships = new List<IShipPosition>()
            {
                GetShipPosition('D', 1, 'G', 1),
                GetShipPosition('D', 2, 'G', 2),
                GetShipPosition('D', 3, 'G', 3),
            };

            var destroyers = new List<IShipPosition>()
            {
                //GetShipPosition('E', 6, 'G', 6),
                //GetShipPosition('E', 7, 'G', 7),
                GetShipPosition('E', 8, 'G', 8),
                GetShipPosition('E', 9, 'G', 9),
                GetShipPosition('E', 10, 'G', 10),
            };

            var submarines = new List<IShipPosition>
            {
                GetShipPosition('I', 2, 'I', 4),
                GetShipPosition('J', 2, 'J', 4),
                GetShipPosition('J', 1, 'J', 3),
                GetShipPosition('I', 1, 'I', 3)
            };

            var patrols = new List<IShipPosition>
            {
                GetShipPosition('I', 10, 'J', 10),
                GetShipPosition('I', 9, 'J', 9),
                GetShipPosition('A', 1, 'A', 2),
                GetShipPosition('A', 1, 'B', 1),


            };

            var randomposition =  new List<IShipPosition>
                  {
                      SelectPositionForShip(airCrafts),
                      SelectPositionForShip(battleships),
                      SelectPositionForShip(submarines),
                      SelectPositionForShip(destroyers),
                      SelectPositionForShip(patrols)
                  };

            var outside = new List<IShipPosition>
            {
                GetShipPosition('A',1, 'A', 5),
                GetShipPosition('E',1, 'H', 1),
                GetShipPosition('J',1, 'J', 3),
                GetShipPosition('C',5, 'E', 5),
                GetShipPosition('A',9, 'A', 10),
            };

            var inside = new List<IShipPosition>
            {
                GetShipPosition('B',4, 'B', 8),
                GetShipPosition('E',8, 'H', 8),
                GetShipPosition('I',4, 'I', 6),
                GetShipPosition('D',5, 'F', 5),
                GetShipPosition('A',1, 'A', 2),
            };

            var chose = rnd.Next(10);
            if (chose < 4)
            {
                return outside;
            }else if (chose < 5)
            {
                return inside;
            }
            else
            {
                return randomposition;
            }

        }

        private IShipPosition SelectPositionForShip(List<IShipPosition> positions)
        {
            return positions[rnd.Next(positions.Count)];
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
                    if (ShouldNotContinue(target))
                    {
                        curOrientation = OppositeOrientation(curOrientation);
                        target = GetNeighbourSquare(firstHit, curOrientation);
                        fightState = State.FoundOneEnd;
                        if (ShouldNotContinue(target))
                        {
                            target = GetRandomTarget();
                            Finishedbot(lengthCount);
                            fightState = State.Explore;
                        }
                    }
                    break;
                case State.FoundOneEnd:
                    curOrientation = OppositeOrientation(curOrientation);
                    target = GetNeighbourSquare(firstHit, curOrientation);
                    if (ShouldNotContinue(target))
                    {
                        Finishedbot(lengthCount);
                        target = GetRandomTarget();
                        fightState = State.Explore;
                    }
                    break;
                case State.ContinueToAnotherENd:
                    target = GetNeighbourSquare(lastTarget, curOrientation);
                    if (ShouldNotContinue(target))
                    {
                        Finishedbot(lengthCount);
                        target = GetRandomTarget();
                        fightState = State.Explore;
                    }
                    break;
            }

            return target;

        }

        private bool ShouldNotContinue(IGridSquare target)
        {
            return IsOffBoard(target) || battleMap[GridToMap(target)] == Item.Sea || lengthCount==enemyLeftLength.Max();
        }

        public void HandleShotResult(IGridSquare square, bool wasHit)
        {
            int idx = GridToMap(square);
            if (wasHit)
            {
                likeliHood[idx]++;
                lengthCount++;
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
                    case State.ContinueHit:
                        if (lengthCount == enemyLeftLength.Max())
                        {
                            Finishedbot(lengthCount);
                            fightState = State.Explore;
                        }
                        break;
                    case State.ContinueToAnotherENd:
                        if (lengthCount == enemyLeftLength.Max())
                        {
                            Finishedbot(lengthCount);
                            fightState = State.Explore;
                        }
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
                        Finishedbot(lengthCount);
                        fightState = State.Explore;
                        break;
                    case State.ContinueToAnotherENd:
                        Finishedbot(lengthCount);
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

        private void Finishedbot(int length)
        {
            enemyLeftLength.Remove(length);
            lengthCount = 0;
        }

        public string Name => "Not a simple Bot";

        private IGridSquare GetRandomTarget()
        {
            Dictionary<IGridSquare, float> pickRanks = new Dictionary<IGridSquare, float>();
            var highestRank = 0.0;
            for (int i = 0; i < _boardSize; i++)
            {
                if (battleMap[i] == Item.Fog)
                {
                    IGridSquare sq = MapToGrid(i);
                    var rank = GetSquareRank(sq);
                    highestRank = Math.Max(highestRank, rank);
                    pickRanks.Add(sq, rank);
                }
            }

            var onlyThehighest = pickRanks.Where(c => c.Value == highestRank).Select(c => c.Key);
            var randomPick = rnd.Next(onlyThehighest.Count());

            return onlyThehighest.ElementAt(randomPick);
        }

        private List<Orientation> GetPossibleOrientation(IGridSquare square)
        {
            List<Orientation> result = new List<Orientation>(){
                Orientation.North,Orientation.East,Orientation.South,Orientation.West
            };

            return GetRankedOrientation(square, result);
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

        private bool IsAtCorner(IGridSquare g)
        {
            var corners = new int[] { 0, 9, 90, 99 };
            if (corners.Contains(GridToMap(g)))
                return true;

            return false;
        }


        private List<Orientation> GetRankedOrientation(IGridSquare square, List<Orientation> orientations)
        {

            Dictionary<Orientation,int> RankDictionary = new Dictionary<Orientation,int>();
            for (int i = 0; i < orientations.Count; i++)
            {
                Orientation ori = orientations.ElementAt(i);
                RankDictionary.Add(ori, GetNumberOfFreeSpaceInDirection(square, ori));
            }

            var result = new List<Orientation>();
            foreach (var pair in RankDictionary.OrderByDescending(c => c.Value))
            {
                result.Add(pair.Key);
            }
            return result;

        }

        private int GetNumberOfFreeSpaceInDirection(IGridSquare square, Orientation ori)
        {
            var count = 0;
            var head = GetNeighbourSquare(square, ori);
            while (!IsOffBoard(head) && battleMap[GridToMap(head)] == Item.Fog)
            {
                count++;
                head = GetNeighbourSquare(head, ori);
            }

            if (!IsOffBoard(head) && battleMap[GridToMap(head)] == Item.Ship)
                count--;

            return count;
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

        private float GetSquareRank(IGridSquare square)
        {
            
            // no adjacent ship when pick random target
            List<IGridSquare> surroundings = GetAllSurroundings(square);
            foreach (IGridSquare sur in surroundings)
            {
                if (!IsOffBoard(sur) && battleMap[GridToMap(sur)] == Item.Ship)
                {
                    return 0;
                }
            }


            if ((square.Row == 'J' && square.Column == 9)
                || (square.Row == 'I' && square.Column == 10))
                return 4;

            if (IsAtCorner(square))
                return 2;

            List<IGridSquare> immediate = GetAllImmediateNeighbour(square);
            int count = 0;
            foreach (IGridSquare sur in immediate)
            {
                int idx = GridToMap(sur);
                if (IsOffBoard(sur))
                {
                    //count++;
                }
                else if (battleMap[idx] == Item.Sea)
                {
                    count++;
                }
            }

            //var percentageMiss = GetPercentOfMissesWithinRange(square, 3);

            return 4 - count + likeliHood[GridToMap(square)]/2 ;
        }

        private float GetPercentOfMissesWithinRange(IGridSquare square, int range)
        {
            var centerRow = square.Row - 'A';
            var centerCol = square.Column;
            var count = 0;
            var total = 0;
            for (int r = centerRow - range; r <= centerRow + range; r++)
            {
                for (int c = centerCol - range; c <= centerCol + range; c++)
                {
                    var idx = r * 10 + centerCol - 1;

                    if (!IsOffBoard(MapToGrid(idx)))
                    {
                        total++;
                        if (battleMap[idx] == Item.Sea)
                            count++;
                    }
                }
            }
            return count / total;
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
