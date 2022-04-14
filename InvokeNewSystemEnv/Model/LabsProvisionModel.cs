using Microsoft.Azure.Management.Compute.Fluent.Models;

namespace InvokeNewSystemEnv.Model
{
    public class LabsProvisionModel
    {
        public string VirtualMachineName { get; set; }

        public string ContactPerson { get; set; }
        
        public string SubscriptionId { get; set; }

        public string ApplicationId { get; set; }

        public string ApplicationKey { get; set; }

        public string TenantId { get; set; }

        public string Fqdn { get; set; }

        public string apiprefix { get; set; }

        public string ResourceGroupName { get; set; }

        public string location { get; set; }

        public string computerName { get; set; }

    }
}
