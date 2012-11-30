namespace LogProxy.Lib.Inspection.Implementation
{
    public class SoapCsvMessageLoggerFields
    {
        public string Time { get; set; }

        public string Location { get; set; }

        public string SoapAction { get; set; }

        public string RequestContentLength { get; set; }

        public string ResponseContentLength { get; set; }

        public string RequestMiliseconds { get; set; }
    }
}
