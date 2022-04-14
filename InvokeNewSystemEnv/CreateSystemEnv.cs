using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using InvokeNewSystemEnv.Model;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json.Linq;

namespace InvokeNewSystemEnv
{
    public static class CreateSystemEnv
    {
        [FunctionName("CreateSystemEnv")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Start of Provisioning");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            LabsProvisionModel labsProvision = JsonConvert.DeserializeObject<LabsProvisionModel>(requestBody);
            if (string.IsNullOrEmpty(labsProvision.SubscriptionId) ||
                string.IsNullOrEmpty(labsProvision.TenantId) ||
                string.IsNullOrEmpty(labsProvision.ApplicationId) ||
                string.IsNullOrEmpty(labsProvision.ApplicationKey) ||
                string.IsNullOrEmpty(labsProvision.VirtualMachineName) ||
                string.IsNullOrEmpty(labsProvision.ContactPerson) ||
                string.IsNullOrEmpty(labsProvision.location) ||
                string.IsNullOrEmpty(labsProvision.Fqdn) ||
                string.IsNullOrEmpty(labsProvision.apiprefix) ||
                string.IsNullOrEmpty(labsProvision.ResourceGroupName))
            {
                log.LogInformation("Incorect Request Body.");

                return new BadRequestObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        message = "Incorect Request Body.",
                        requestBody = JsonConvert.SerializeObject(labsProvision)
                    })
                );
            }

            string subscriptionId       = labsProvision.SubscriptionId;
            string tenantId             = labsProvision.TenantId;
            string applicationId        = labsProvision.ApplicationId;
            string applicationKey       = labsProvision.ApplicationKey;
            string virtualMachineName   = labsProvision.VirtualMachineName.ToUpper();
            string resourceGroupName    = labsProvision.ResourceGroupName.ToUpper();
            string ContactPerson        = labsProvision.ContactPerson;
            string Fqdn                 = labsProvision.Fqdn;
            string apiprefix            = labsProvision.apiprefix;
            string location             = labsProvision.location;
            string computerName         = labsProvision.computerName;


            log.LogInformation($"subscriptionId: {subscriptionId}");
            log.LogInformation($"tenantId: {tenantId}");
            log.LogInformation($"applicationId: {applicationId}");
            log.LogInformation($"virtualMachineName: {virtualMachineName}");
            log.LogInformation($"resourceGroupName: {resourceGroupName}");
            log.LogInformation($"Fqdn: {Fqdn}");
            log.LogInformation($"apiprefix: {apiprefix}");


            string createEnvironmentVariablesPsUrl = ResourceHelper.GetEnvironmentVariable("CreateEnvironmentVariablesPsUrl");
            try
            {
                ServicePrincipalLoginInformation principalLogIn = new ServicePrincipalLoginInformation();
                principalLogIn.ClientId = applicationId;
                principalLogIn.ClientSecret = applicationKey;

                AzureEnvironment azureEnvironment = AzureEnvironment.AzureGlobalCloud;
                AzureCredentials credentials = new AzureCredentials(principalLogIn, tenantId, azureEnvironment);


                IAzure _azure = Azure.Configure()
                      .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                      .Authenticate(credentials)
                      .WithSubscription(subscriptionId);

                
                JObject templateParameterObjectCustomExtension = ResourceHelper.GetJObject(Properties.Resources.windows_template_custom_extension);

                templateParameterObjectCustomExtension.SelectToken("parameters.vmName")["defaultValue"] = virtualMachineName;
                templateParameterObjectCustomExtension.SelectToken("parameters.location")["defaultValue"] = location;
                templateParameterObjectCustomExtension.SelectToken("parameters.fileUris")["defaultValue"] = createEnvironmentVariablesPsUrl;
                templateParameterObjectCustomExtension.SelectToken("parameters.arguments")["defaultValue"] = $"-ResourceGroupName {resourceGroupName} -VirtualMachineName {virtualMachineName} -ComputerName {computerName} -TenantId {tenantId} -GroupCode {apiprefix} -Fqdn {Fqdn}";

                string uniqueId = Guid.NewGuid().ToString().Replace("-", "");
                string deploymentName = $"virtual-machine-extension-{uniqueId}".ToLower();
                log.LogInformation($"Deploying virtual-machine-extension-{uniqueId}");

                log.LogInformation("Setting up system environment");

                IDeployment vmExtensionDeployment = _azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithTemplate(templateParameterObjectCustomExtension)
                    .WithParameters("{}")
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                    .Create();

                log.LogInformation("Setting up system environment is done");

                return new OkObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        message = $"virtual-machine-extension-{uniqueId} deployment is done"
                    })
                );

            }
            catch (Exception e)
            {

                log.LogError(e.Message);

                return new BadRequestObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        message = e.Message
                    })
                );
            }

        }
    }
}
