namespace StatsBro.Domain.Config
{
    public class ESConfig
    {
        public string Uris { get; set; } = "http://localhost:9200;";

        public string PipelineNameProcessing { get; set; } = "statsbro_request-cleanup";

        public int PipelineVersion { get; set; } = 1;

        public string IndexTemplateName { get; set; } = "statsbro_index_template";

        public string IndexPrefix { get; set; } = "";

        public int IndexTemplateVersion { get; set; } = 1;

        public string LifecyclePolicyName { get; set; } = "statsbro_default_lifecycle-policy";
    }
}
