using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.PayMill.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.PayMill.Fields.publicKey")]
        public string publicKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayMill.Fields.privateKey")]
        public string privateKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayMill.Fields.APIUrl")]
        public string apiUrl { get; set; }

    }
}