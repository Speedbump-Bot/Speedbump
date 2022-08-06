namespace Speedbump
{
    [Serializable]
    public class Dataset
    {
        public List<string> Columns = new();
        public List<object> ColumnTypes = new();
        public int RowCount;
        public double ReadTimeMS;
        public List<List<object>> Rows = new();

        public List<T> Bind<T>() where T : new()
        {
            var props = typeof(T).GetProperties();

            var instances = new List<T>();

            foreach (var row in Rows)
            {
                var instance = new T();
                var i = 0;
                foreach (var col in Columns)
                {
                    var prop = props.FirstOrDefault(p => p.Name.ToLower().Replace("_", "") == col.ToLower().Replace("_", ""));
                    if (prop == default)
                    {
                        i++; 
                        continue; 
                    }
                    prop.SetValue(instance, row[i]);
                    i++;
                }
                instances.Add(instance);
            }

            return instances;
        }
    }
}
