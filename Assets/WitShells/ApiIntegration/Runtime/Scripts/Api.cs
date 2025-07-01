namespace WitShells.ApiIntegration
{
    public class Api : ApiManager
    {
        public ApiExecutor apiExecutor;

        public ApiExecutor Executor
        {
            get
            {
                if (apiExecutor == null)
                {
                    apiExecutor = GetComponent<ApiExecutor>();
                    if (apiExecutor == null)
                    {
                        apiExecutor = gameObject.AddComponent<ApiExecutor>();
                    }
                }
                return apiExecutor;
            }
        }
    }
}