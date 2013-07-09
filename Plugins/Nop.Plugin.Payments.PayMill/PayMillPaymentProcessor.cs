using System;
using System.Collections.Generic;
using System.Web.Routing;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.PayMill.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Plugin.Payments.PayMill.Models;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using Nop.Services.Orders;
using Nop.Services.Customers;
using Nop.Services.Logging;
using System.Text.RegularExpressions;
using Nop.Services.Directory;
using Nop.Core.Domain.Directory;
using Nop.Core;
using System.Globalization;

namespace Nop.Plugin.Payments.PayMill
{
    /// <summary>
    /// PayMill payment processor
    /// </summary>
    public class PayMillPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly PayMillPaymentSettings _PayMillPaymentSettings;
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;


        #endregion

        #region Ctor

        public PayMillPaymentProcessor(PayMillPaymentSettings PayMillPaymentSettings,
            ISettingService settingService, PaymentPayMillController PayMillController, IOrderService OrderService, ICustomerService CustomerService, ILogger logger, ICurrencyService CurrencyService, CurrencySettings CurrencySettings)
        {
            this._PayMillPaymentSettings = PayMillPaymentSettings;
            this._settingService = settingService;
            this._orderService = OrderService;
            this._customerService = CustomerService;
            this._logger = logger;
            this._currencyService = CurrencyService;
            this._currencySettings = CurrencySettings;
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            try
            {
                result.AllowStoringCreditCardNumber = false;
                var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
                var order = _orderService.GetOrderByGuid(processPaymentRequest.OrderGuid);
                string currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode;
                PaymentModel pm = new PaymentModel();
                pm = getPayment("?token=" + processPaymentRequest.CustomValues["paymenttoken"]);
                string paymentid = pm.id;
                string amount = processPaymentRequest.OrderTotal.ToString();
                amount = Regex.Replace(amount, @"[^\d]", "");
                
                //string urlappend = "?payment=" + payment + "&amount=" + amount + "&currency=" + currency + "&description=" + description;
                string urlbuilder = "?payment=" + paymentid + "&amount=" + amount + "&currency=" + currency + "&description=Order from Store ID: " + processPaymentRequest.StoreId.ToString() + " PaymentID: " + pm.id;
                TransactionModel trans = new TransactionModel();
                //_logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "url for transaction", urlbuilder);
                trans = getTransaction(urlbuilder, pm);
                string responsecode = trans.response_code;
                string transactionid = trans.id;
                //set the transaction variables
                if (responsecode == "20000")
                {
                    //successful response
                    result.AuthorizationTransactionCode = transactionid;
                    result.AuthorizationTransactionResult = responsecode;
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    //_logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "success at paymill proceessorder" + responsecode + urlbuilder + paymentid, trans.status + urlbuilder + paymentid, null);
                }
                else
                {
                    //failed transaction
                    _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "failure at paymill proceessorder" + responsecode, trans.status, null);
                    result.AddError(getErrorcodes(responsecode, trans.status));
                }
            }
            catch (Exception ex)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Error, ex.Message, ex.ToString());
                result.AddError(ex.ToString());
            }
            return result;
        }

        public string getErrorcodes(string responsecode, string status)
        {
            string errormessage = "";
            switch (responsecode)
            {
                case "10001":
                    errormessage = "General undefined response";
                    break;
                case "10002":
                    errormessage = "Waiting on something";
                    break;
                case "40000":
                    errormessage = "General data problem";
                    break;
                case "40100":
                    errormessage = "Problem w/credit card data";
                    break;
                case "40101":
                    errormessage =  "Problem with cvv.";
                    break;
                case "40102":
                    errormessage = "Card expired or not yet valid";
                    break;
                case "40103":
                    errormessage = "Limit exceeded";
                    break;
                case "40200":
                    errormessage = "Problem with bank account data";
                    break;
                case "40300":
                    errormessage = "Problem with 3D secure data";
                    break;
                case "50000":
                    errormessage = "General server problems";
                    break;
                case "50100":
                    errormessage = "Technical error with credit card";
                    break;
                case "50101":
                    errormessage = "Error limit exceeded";
                    break;
                case "50200":
                    errormessage = "Technical error with bank account";
                    break;
                case "50300":
                    errormessage = "Technical error with 3D secure";
                    break;
                case "50400":
                    errormessage = "Decline because of risk issues";
                    break;
            }
            return errormessage;
        }

        public PaymentModel getPayment(string urlvalues)
        {
            PaymentModel pmodel = new PaymentModel();
            try
            {
                string apiobject = "payments";
                string response = GetPMAPIResponse(_PayMillPaymentSettings.apiUrl + apiobject + urlvalues, _PayMillPaymentSettings.privateKey);
                if (response != "noresponse" && response.Substring(0, 5) != "ERROR")
                {
                    JObject jObject = JObject.Parse(response);
                    JToken thedata = jObject["data"];
                    pmodel.id = thedata["id"].ToString().Replace("\"", "");
                    pmodel.type = thedata["type"].ToString().Replace("\"", "");
                    pmodel.client = thedata["client"].ToString().Replace("\"", "");
                    pmodel.card_type = thedata["card_type"].ToString().Replace("\"", "");
                    pmodel.country = thedata["country"].ToString().Replace("\"", "");
                    pmodel.expire_month = thedata["expire_month"].ToString().Replace("\"", "");
                    pmodel.expire_year = thedata["expire_year"].ToString().Replace("\"", "");
                    pmodel.card_holder = thedata["card_holder"].ToString().Replace("\"", "");
                    pmodel.last4 = thedata["last4"].ToString().Replace("\"", "");
                    pmodel.created_at = thedata["created_at"].ToString().Replace("\"", "");
                    pmodel.updated_at = thedata["updated_at"].ToString().Replace("\"", "");
                }
                //_logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "getpayment successful " + response, "getpayment successful" + response, null);
            }
            catch (Exception ex)
            {
                pmodel = null;
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Error, ex.Message + " error in getpayment", ex.ToString(), null);
            }

            return pmodel;
        }

        public TransactionModel getTransaction(string urlvalues, PaymentModel pm)
        {
            TransactionModel tm = new TransactionModel();
            string apiobject = "transactions";
            string response = GetPMAPIResponse(_PayMillPaymentSettings.apiUrl + apiobject + urlvalues, _PayMillPaymentSettings.privateKey);
            if (response != "noresponse" && response.Substring(0, 5) != "ERROR")
            {
                JObject jObject = JObject.Parse(response);
                JToken thedata = jObject["data"];
                tm.id = thedata["id"].ToString().Replace("\"", "");
                tm.amount = thedata["amount"].ToString().Replace("\"", "");
                tm.origin_amount = thedata["origin_amount"].ToString().Replace("\"", "");
                tm.currency = thedata["currency"].ToString().Replace("\"", "");
                tm.status = thedata["status"].ToString().Replace("\"", "");
                tm.description = thedata["description"].ToString().Replace("\"", "");
                tm.livemode = thedata["livemode"].ToString().Replace("\"", "");
                tm.created_at = thedata["created_at"].ToString().Replace("\"", "");
                tm.updated_at = thedata["updated_at"].ToString().Replace("\"", "");
                tm.response_code = thedata["response_code"].ToString().Replace("\"", "");
                tm.payment = pm;
            }
            return tm;
        }


        public string GetPMAPIResponse(string url, string uid)
        {
            string responseData = "noresponse";
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Credentials = new NetworkCredential(uid, "");
                WebResponse webResponse = webRequest.GetResponse();
                using (StreamReader streamReader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseData = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                responseData = "ERROR: " + ex.ToString();
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, ex.Message + "from get api response", ex.ToString(), null);

            }
            return responseData;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> items)
        {
            return 0;//_PayMillPaymentSettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            //result.AddError("Refund method not supported");
            try
            {
                NumberFormatInfo nfi = CultureInfo.CurrentCulture.NumberFormat;
                nfi = (NumberFormatInfo)nfi.Clone();
                nfi.CurrencySymbol = "";
                var order = refundPaymentRequest.Order;
                string transactioncode = order.AuthorizationTransactionCode;
                string amount = string.Format(nfi, "{0:c}", refundPaymentRequest.AmountToRefund);
                Regex digitsOnly = new Regex(@"[^\d]");
                amount = digitsOnly.Replace(amount, "");
                string apiobject = "refunds";
                string buildurl = _PayMillPaymentSettings.apiUrl + apiobject + "/" + transactioncode + "?amount=" + amount;
                string response = GetPMAPIResponse(buildurl, _PayMillPaymentSettings.privateKey);
                if (response != null)
                {

                }
            }
            catch (Exception ex)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Error, ex.Message, ex.ToString());
                result.AddError(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();

            result.AllowStoringCreditCardNumber = true;
            
            
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            //always success
            return new CancelRecurringPaymentResult();
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentPayMill";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.PayMill.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentPayMill";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.PayMill.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentPayMillController);
        }

        public override void Install()
        {
            //settings
            //var settings = new PayMillPaymentSettings()
            //{
            //    TransactMode = TransactMode.Pending
            //};
            //_settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PayMill.Fields.publicKey", "Public Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PayMill.Fields.publicKey.Hint", "Enter your paymill public key here.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PayMill.Fields.privateKey", "Private Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PayMill.Fields.privateKey.Hint", "enter your paymill private key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PayMill.Fields.apiUrl", "API Url");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PayMill.Fields.apiUrl.Hint", "Enter the url for the paymill API");
            

            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.PayMill.Fields.publicKey");
            this.DeletePluginLocaleResource("Plugins.Payments.PayMill.Fields.publicKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PayMill.Fields.privateKey");
            this.DeletePluginLocaleResource("Plugins.Payments.PayMill.Fields.privateKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PayMill.Fields.apiUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.PayMill.Fields.apiUrl.Hint");
            base.Uninstall();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.Manual;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Standard;
            }
        }

        #endregion
        
    }
}
