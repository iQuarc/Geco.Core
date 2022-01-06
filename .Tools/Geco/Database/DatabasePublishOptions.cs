namespace Geco.Database
{
    public class DatabasePublishOptions
    {
        public string ProjectName { get; set; }
        public string PublishProfile { get; set; }
        public string ConnectionName { get; set; }
        public bool? BlockOnPossibleDataLoss { get; set; }
    }
}