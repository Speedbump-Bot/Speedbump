namespace Speedbump
{
    public class Lifetime
    {
        List<(ExitOrder, Action<ExitCause>)> Handlers = new();

        public void Add(Action<ExitCause> act, ExitOrder ord = ExitOrder.Normal) =>
            Handlers.Add((ord, act));

        public void End(ExitCause cause)
        {
            foreach (var c in Enum.GetValues<ExitOrder>().OrderBy(c => (int)c))
            {
                foreach (var handler in Handlers.Where(h => h.Item1 == c))
                {
                    handler.Item2?.Invoke(cause);
                }
            }
        }

        public enum ExitCause
        {
            Normal = 0,
            Exception = 1,
        }

        public enum ExitOrder
        {
            Normal = 0,
            Data = 1,
            Logging = 2,
            Configuration = 3
        }
    }
}
