namespace StatsBro.Domain.Config
{
    public class ServiceConfig
    {
        public string Pepper { get; set; } = null!;

        public string PepperCertSubjectName { get; set; } = "StatsBroP";
    }
}
