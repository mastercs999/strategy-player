using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.TradingBase
{
    public class State
    {
        public Dictionary<string, StrategyStateBase> NameToState { get; private set; }
        public IEnumerable<StrategyStateBase> AllStates => NameToState.Select(x => x.Value);
        public List<Bundle> AllBundles => NameToState.SelectMany(x => x.Value.Bundles).ToList();
        



        public State()
        {
            NameToState = new Dictionary<string, StrategyStateBase>();
        }
        public State(Dictionary<string, StrategyStateBase> nameToState)
        {
            NameToState = nameToState;
        }

        public T GetStrategyState<T>(string name) where T : StrategyStateBase
        {
            return (T)NameToState[name];
        }
    }
}
