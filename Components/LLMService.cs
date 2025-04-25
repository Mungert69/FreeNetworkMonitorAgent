namespace NetworkMonitorAgent
{

    public interface ILLMService
{
    List<string> GetLLMTypes();
    string GetLLMServerUrl(string siteId);
}
    public  class LLMService : ILLMService
    {
        public string GetLLMServerUrl(string siteId)
        {
            // Implement your logic to get the LLM server URL
            return $"wss://devoauth.freenetworkmonitor.click/LLM/llm-stream";
        }

         public string GetLLMServerAuthUrl(string siteId)
        {
            // Implement your logic to get the LLM server URL
            return $"wss://devoauth.freenetworkmonitor.click/LLM/llm-stream-auth";
        }

        public  List<string> GetLLMTypes()
        {
            return new List<string> { "TurboLLM", "HugLLM", "TestLLM" };
        }
    }
}