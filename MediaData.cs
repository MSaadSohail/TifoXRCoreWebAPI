namespace TifoXRCoreWebAPI
{
    [System.Serializable]
    public class MediaData
    {
        public required string Sku { get; set; }

        public required string Name { get; set; }

        public required int MediaType { get; set; }

        public required string MediaLink { get; set; }
    }
}
