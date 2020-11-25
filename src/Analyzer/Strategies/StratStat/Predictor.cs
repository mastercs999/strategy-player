using Analyzer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.StratStat
{
    public class Predictor
    {
        private Func<Table, int, int, bool> OpenCondition;
        private Func<Table, int, int, bool> Objective;
        private Func<Table, int, int, int, bool> CanExitFunc;
        private int HistoryLength;
        private Queue<bool> StatHistory;
        private int Correct;
        private int Wrong;

        public double ProbabilityOfSuccess => Correct + Wrong == 0 ? 0 : Correct / (double)(Correct + Wrong);

        public Predictor(int historyLength, Func<Table, int, int, bool> openCondition, Func<Table, int, int, bool> objective, Func<Table, int, int, int, bool> canExit)
        {
            HistoryLength = historyLength;
            StatHistory = new Queue<bool>(HistoryLength + 1);
            OpenCondition = openCondition;
            Objective = objective;
            CanExitFunc = canExit;
        }

        public bool HasMatch(Table table, int row, int col) => OpenCondition(table, row, col);
        public bool AssumptionWorks(Table table, int row, int col) => Objective(table, row, col);
        public bool CanExit(Table table, int startRow, int col, int endRow) => CanExitFunc(table, startRow, col, endRow);

        public void UpdateStatistics(Table table, int row, int col)
        {
            if (HasMatch(table, row, col))
            {
                if (AssumptionWorks(table, row, col))
                {
                    ++Correct;
                    StatHistory.Enqueue(true);
                }
                else
                {
                    ++Wrong;
                    StatHistory.Enqueue(false);
                }

                if (StatHistory.Count > HistoryLength)
                {
                    if (StatHistory.Dequeue())
                        --Correct;
                    else
                        --Wrong;
                }
            }
        }

        public PredictorResult CurrentGuess(Table table, int row, int col)
        {
            bool match = HasMatch(table, row, col);

            return new PredictorResult()
            {
                PatternMatch = match,
                ProbabilityOfSuccess = !match ? 0 : ProbabilityOfSuccess,
                CanExitFunc = CanExitFunc
            };
        }
    }
}
