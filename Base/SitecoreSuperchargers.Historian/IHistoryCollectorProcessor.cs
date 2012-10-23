namespace SitecoreSuperchargers.Historian
{
    public interface IHistoryCollectorProcessor
    {
        void Process(HistoryCollectorPipelineArgs args);
    }
}
