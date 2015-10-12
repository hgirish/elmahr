namespace ElmahR.Core
{
    public class Envelope
    {
        public int Id { get; set; }
        public string ApplicationName { get; set; }
        public string InfoUrl { get; set; }
        public string SourceId { get; set; }
        public string ErrorId { get; set; }
        public Error Error { get; set; }
        public string Class { get; set; }
    }
}