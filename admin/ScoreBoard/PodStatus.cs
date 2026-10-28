using k8s;

namespace ScoreBoard;

/// <summary>
/// Reports replica counts and ready pods for the nba-scoreboard Deployment, using the pod's
/// own service account. Outside a cluster every count is -1.
/// </summary>
public sealed class PodStatus
{
    private const string NamespaceFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
    private const string DeploymentName = "nba-scoreboard";

    private readonly Kubernetes? _client;
    private readonly string _namespace = "unknown";

    public PodStatus()
    {
        try
        {
            _namespace = File.ReadAllText(NamespaceFile).Trim();
            _client = new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
        }
        catch (Exception)
        {
            _client = null;
        }
    }

    public async Task<object> GetAsync()
    {
        if (_client is null)
        {
            return new { replicas = -1, ready = -1, available = -1, @namespace = _namespace };
        }

        try
        {
            var deployment = await _client.ReadNamespacedDeploymentAsync(DeploymentName, _namespace);
            var pods = await _client.ListNamespacedPodAsync(_namespace, labelSelector: $"app={DeploymentName}");
            var readyPods = pods.Items
                .Where(p => p.Status?.Phase == "Running"
                            && (p.Status.ContainerStatuses ?? []).All(c => c.Ready))
                .Select(p => p.Metadata.Name)
                .ToList();

            return new
            {
                replicas = deployment.Status?.Replicas ?? 0,
                ready = deployment.Status?.ReadyReplicas ?? 0,
                available = deployment.Status?.AvailableReplicas ?? 0,
                pods = readyPods,
                @namespace = _namespace
            };
        }
        catch (Exception e)
        {
            return new
            {
                replicas = -1,
                ready = -1,
                available = -1,
                pods = Array.Empty<string>(),
                error = e.Message,
                @namespace = _namespace
            };
        }
    }
}
