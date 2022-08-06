namespace Speedbump
{
    public class DataCache<T>
    {
        private DateTime LoadTime;
        private T _Data;
        private bool Invalid;

        public T Data
        {
            get
            {
                if (LoadTime == default || Age > MaxAge || Invalid)
                {
                    LoadTime = DateTime.Now;
                    _Data = Load.Invoke();
                    Invalid = false;
                }
                return _Data;
            }
        }

        public TimeSpan Age => DateTime.Now - LoadTime;

        public Func<T> Load { get; set; }
        public TimeSpan MaxAge { get; set; }
        
        public DataCache(Func<T> load, TimeSpan maxAge = default)
        {
            if (load is null) { throw new ArgumentException("load cannot be null"); }
            maxAge = maxAge == default ? TimeSpan.FromMinutes(30) : maxAge;
            Load = load;
            MaxAge = maxAge;
        }

        public void Invalidate() => Invalid = true;
    }
}
