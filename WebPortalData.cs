namespace TifoXRCoreWebAPI
{
    [System.Serializable]
    public class WebPortalData
    {
        public required string Sku {  get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }

        public required string PortalLink { get; set; }

        public required int MediaType { get; set; }

        public required string MediaLink { get; set; }

        public string? ThumbnailLink { get; set; }
    }
}
