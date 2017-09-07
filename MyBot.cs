using System.Collections.Generic;
using Battleships.Player.Interface;
using System.Linq;


namespace BattleshipBot
{
    public class MyBot : IBattleshipsBot
    {
        private IGridSquare lastTarget;
        private static int boardSize = 100;
        private item[] battleMap = Enumerable.Repeat(item.fog,boardSize).ToArray();

        private List<int> enemyLeftLength = new List<int>{5,4,3,3,2};
        private System.Random rnd = new System.Random();

        private List<orientation> possible_ori;
        private orientation curOrientation;
        private state fightState = state.explore;
        private IGridSquare firstHit;

        public IEnumerable<IShipPosition> GetShipPositions()
        {
            lastTarget = null; // Forget all our history when we start a new game
            return new List<IShipPosition>
				  {
				    GetShipPosition('B', 4, 'B', 8), // Aircraft Carrier
				    GetShipPosition('C', 2, 'F', 2), // Battleship
				    GetShipPosition('E', 6, 'G', 6), // Destroyer
				    GetShipPosition('H', 2, 'H', 4), // Submarine
				    GetShipPosition('H', 9, 'I', 9)  // Patrol boat
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
            IGridSquare target = getRandomTarget();
            switch (fightState)
            {
                case state.explore:
                    target = getRandomTarget();
                    break;
                case state.findingDirection:
                    target = getNeighbourSquare(firstHit, this.possible_ori[0]);
                    break;
                case state.continueHit:
                    target = getNeighbourSquare(lastTarget, curOrientation);
                    if (isOffBoard(target))
                    {
						curOrientation = oppositeOrientation(curOrientation);
						target = getNeighbourSquare(firstHit, curOrientation);
                        fightState = state.FoundOneEnd;
                    }
                       
                    break;
                case state.FoundOneEnd:
                    curOrientation = oppositeOrientation(curOrientation);
                    target = getNeighbourSquare(firstHit, curOrientation);
					if (isOffBoard(target))
					{
                        target = getRandomTarget();
                        fightState = state.explore;
					}
                    break;
                case state.continueToAnotherENd:
                    target = getNeighbourSquare(lastTarget, curOrientation);
                    if (isOffBoard(target))
                    {
                        target = getRandomTarget();
                        fightState = state.explore;
                    }
                    break;
            }


            return target;

        }

        public void HandleShotResult(IGridSquare square, bool wasHit)
        {
			int idx = gridToMap(square);
			if(wasHit){
                battleMap[idx] = item.ship;
                switch (fightState)
                {
                    case state.explore:
                        firstHit = square;
                        this.possible_ori = getPossibleOrientation(square);
                        fightState = state.findingDirection;
                        break;

                    case state.findingDirection:
                        fightState = state.continueHit;
                        curOrientation = this.possible_ori[0];
                        break;
                    case state.FoundOneEnd:
                        fightState = state.continueToAnotherENd;
                        break;
                    default:
                        break;
                }

            }else{
                battleMap[idx] = item.sea;
                switch (fightState)
                {
                    case state.findingDirection:
                        possible_ori.RemoveAt(0);
                        break;
                    case state.continueHit:
                        fightState = state.FoundOneEnd;
                        break;
                    case state.FoundOneEnd:
                        fightState = state.explore;
                        break;
                    case state.continueToAnotherENd:
                        fightState = state.explore;
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

        private IGridSquare getRandomTarget(){
            List<IGridSquare> candites = new List<IGridSquare> { };
			for (int i = 0; i < boardSize; i++)
			{
                if (battleMap[i] == item.fog)
                {
                   IGridSquare sq = mapToGrid(i);
	               if(isGoodPick(sq))
	               {
						candites.Add(sq);
					}
				}
			}

            int pick = rnd.Next(0,candites.Count);
            IGridSquare res = candites[pick];
            return res;
        }

        private List<orientation> getPossibleOrientation(IGridSquare square){
            List<orientation> result = new List<orientation>(){
                orientation.North,orientation.East,orientation.South,orientation.West
            };

            //possible exception
            return removeImpossibleOrientation(square, result);
        }


        private IGridSquare getNeighbourSquare(IGridSquare g, orientation o, int step = 1){
            int col = g.Column;
            char row = g.Row;
            switch (o)
            {
                case orientation.East:
                    col += step; 
                    break;
                case orientation.West:
                    col -= step;
                    break;
                case orientation.North:
                    row = (char) (row - step);
                    break;
                case orientation.South:
                    row = (char) (row + step);
                    break;
            }
            GridSquare result = new GridSquare(row, col);
            return result;

        }

        private int gridToMap(IGridSquare g)
        {
            return (int)(g.Row-'A')*10 + g.Column-1; 
        }

        private IGridSquare mapToGrid(int square)
        {
			char row = (char)('A' + square / 10);
			int column = square % 10 + 1;
            return new GridSquare(row, column);
        }


        private bool isOffBoard(IGridSquare g)
        {
            if(g.Row < 'A' || g.Row > 'J' || g.Column>10 || g.Column<1)
            {
                return true;
            }else
            {
                return false;
            }

        }


        private List<orientation> removeImpossibleOrientation(IGridSquare square, List<orientation> os)
		{

            List<orientation> result = new List<orientation>();
            for (int i = 0; i < os.Count; i++ )
            {
                orientation ori = os.ElementAt(i);
                IGridSquare poke = getNeighbourSquare(square, ori);
                if (!isOffBoard(poke))
                {
                    int idx = gridToMap(poke);
                    if (battleMap[idx] == item.fog)
                    {
                        result.Add(ori);
                    }
                }

            }
            return result;
        }

        private orientation oppositeOrientation(orientation o)
        {
            switch(o)
            {
                case orientation.South:
                    return orientation.North;
                case orientation.North:
                    return orientation.South;
                case orientation.West:
                    return orientation.East;
                default:
                    return orientation.West;
            }
        }

        private bool isGoodPick(IGridSquare square)
        {
            // no adjacent ship when pick random target
            List<IGridSquare> surroundings = getAllSurroundings(square);
            foreach (IGridSquare sur in surroundings)
            {
                if (!isOffBoard(sur))
                {
                    int idx = gridToMap(sur);
                    if(battleMap[idx] == item.ship)
                    {
                        return false;
                    }
                }
            }

            //don't pick if there are three surrounding sea/boarder
            List<IGridSquare> immediate = getAllImmediateNeighbour(square);
            int count = 0;
            foreach (IGridSquare sur in immediate)
			{
                int idx = gridToMap(sur);
                if (isOffBoard(sur))
                {
                    count++;
                }else if(battleMap[idx] != item.fog)
                {
                    count++;
                }
			}
            if(count>2)
            {
                return false;
            }

            return true;
        }

        private List<IGridSquare> getAllSurroundings(IGridSquare center)
        {
            
            int[] surrounding_idx = { -11, -10, -9, -1, +1, +9, +10, +11 };
            return getAll(center, surrounding_idx);
        }

        private List<IGridSquare> getAllImmediateNeighbour(IGridSquare center)
        {
			int[] surrounding_idx = { -10, -1, +1, +10};
			return getAll(center, surrounding_idx);
        }

        private List<IGridSquare> getAll(IGridSquare center, int[] idxes)
        {
			List<IGridSquare> result = new List<IGridSquare>();
			int cen_idx = gridToMap(center);
            int[] surrounding_idx = idxes;
            for (int i = 0; i < idxes.Length; i++)
			{
				int idx = cen_idx + surrounding_idx[i];
				result.Add(mapToGrid(idx));
			}
			return result;
        }

    }

    public enum orientation
    {
        North=0,
        South=1,
        West=2,
        East=3,
        unkown
    }

    public enum item
    {
        fog,
        ship,
        sea
    }

    public enum state
    {
        explore,
        findingDirection,
        continueHit,
        FoundOneEnd,
        continueToAnotherENd,
        Finished
    }
}
