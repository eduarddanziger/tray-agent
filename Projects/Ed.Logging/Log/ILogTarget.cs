namespace Ed.Logging.Log
{
    /// <summary>Interface for log target.</summary>
    public interface ILogTarget
    {
        void Write(string newItem);
    }
}