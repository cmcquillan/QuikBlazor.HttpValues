namespace QuikBlazor.HttpValues.Internal
{
    internal class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
