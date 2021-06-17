using System.Threading.Tasks;

namespace OrchardCore.Rules
{
    public interface IConditionEvaluator
    {
        ValueTask<bool> EvaluateAsync(Condition condition);
    }

    public abstract class ConditionEvaluator<T> : IConditionEvaluator where T : Condition
    {
        protected static readonly ValueTask<bool> False = new ValueTask<bool>(false);
        protected static readonly ValueTask<bool> True = new ValueTask<bool>(true);

        ValueTask<bool> IConditionEvaluator.EvaluateAsync(Condition condition)
            => EvaluateAsync(condition as T);

        public abstract ValueTask<bool> EvaluateAsync(T condition);
    }
}
