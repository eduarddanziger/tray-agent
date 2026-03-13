namespace Ed.Logging.Log
{
    public class LogChannelWithTransformerAndFilter : LogChannel
    {
        protected ulong Counter;

        public override void PushLine(string line, int infoType)
        {
            if (!Filter(infoType))
            {
                return;
            }

            ++Counter;
            base.PushLine(Transform(line), infoType);
        }

        public virtual string Transform(string line)
        {
            return $"{Counter % uint.MaxValue,9} {HiTi.Default.Now:yyyy-MM-dd HH:mm:ss.ffffff} {line}";
        }

        public virtual bool Filter(int infoType)
        {
            return true;
        }
    }
}