using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.PayMill
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.Payments.PayMill.Configure",
                 "Plugins/PaymentPayMill/Configure",
                 new { controller = "PaymentPayMill", action = "Configure" },
                 new[] { "Nop.Plugin.Payments.PayMill.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.PayMill.PaymentInfo",
                 "Plugins/PaymentPayMill/PaymentInfo",
                 new { controller = "PaymentPayMill", action = "PaymentInfo" },
                 new[] { "Nop.Plugin.Payments.PayMill.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
