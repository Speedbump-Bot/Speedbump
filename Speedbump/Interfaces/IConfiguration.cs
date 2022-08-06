namespace Speedbump
{
    public interface IConfiguration
    {
        public T Get<T>(string path);
    }
}
