namespace JamShared
{
    public interface ISampleStream
    {
        double[] ReadOneSample(double offset);

        void Reset();
    }
}
