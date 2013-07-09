using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Plugin.Payments.PayMill.Models;
using Nop.Plugin.Payments.PayMill.Validators;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Services.Orders;
using Nop.Core.Domain.Orders;
using System.Collections;
using System.Text.RegularExpressions;

namespace Nop.Plugin.Payments.PayMill.Controllers
{
    public class PaymentPayMillController : BaseNopPaymentController
    {
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly PayMillPaymentSettings _PayMillPaymentSettings;
        private readonly IWorkContext _workContext;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        public PaymentPayMillController(ISettingService settingService, 
            ILocalizationService localizationService, PayMillPaymentSettings PayMillPaymentSettings, IWorkContext WorkContext, IOrderTotalCalculationService OrderTotalCalculationService)
        {
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._PayMillPaymentSettings = PayMillPaymentSettings;
            this._workContext = WorkContext;
            this._orderTotalCalculationService = OrderTotalCalculationService;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.publicKey = _PayMillPaymentSettings.publicKey;
            model.privateKey = _PayMillPaymentSettings.privateKey;
            model.apiUrl = _PayMillPaymentSettings.apiUrl;
            
            return View("Nop.Plugin.Payments.PayMill.Views.PaymentPayMill.Configure", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();
            
            //save settings
            _PayMillPaymentSettings.publicKey = model.publicKey;
            _PayMillPaymentSettings.privateKey = model.privateKey;
            _PayMillPaymentSettings.apiUrl = model.apiUrl;
            _settingService.SaveSetting(_PayMillPaymentSettings);
            

            return View("Nop.Plugin.Payments.PayMill.Views.PaymentPayMill.Configure", model);
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            
            //CC types
            model.CreditCardTypes.Add(new SelectListItem()
                {
                    Text = "Visa",
                    Value = "Visa",
                });
            model.CreditCardTypes.Add(new SelectListItem()
            {
                Text = "Master card",
                Value = "MasterCard",
            });
            model.CreditCardTypes.Add(new SelectListItem()
            {
                Text = "Discover",
                Value = "Discover",
            });
            model.CreditCardTypes.Add(new SelectListItem()
            {
                Text = "Amex",
                Value = "Amex",
            });
            
            //years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem()
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i.ToString() : i.ToString();
                model.ExpireMonths.Add(new SelectListItem()
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }


            //set postback values
            var form = this.Request.Form;
            model.CardholderName = form["CardholderName"];
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];
            var selectedCcType = model.CreditCardTypes.Where(x => x.Value.Equals(form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedCcType != null)
                selectedCcType.Selected = true;
            var selectedMonth = model.ExpireMonths.Where(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedMonth != null)
                selectedMonth.Selected = true;
            var selectedYear = model.ExpireYears.Where(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedYear != null)
                selectedYear.Selected = true;
            string publickey = _PayMillPaymentSettings.publicKey;
            var customer = _workContext.CurrentCustomer;
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            string amount = _orderTotalCalculationService.GetShoppingCartTotal(cart).ToString();
            Regex digitsOnly = new Regex(@"[^\d]");
            amount = digitsOnly.Replace(amount, "");
            ViewBag.publickey = publickey;
            ViewBag.amount = amount;
            ViewBag.currency = _workContext.WorkingCurrency.CurrencyCode;
            return View("Nop.Plugin.Payments.PayMill.Views.PaymentPayMill.PaymentInfo", model);
        }



        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel()
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                    warnings.Add(error.ErrorMessage);
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];
            paymentInfo.CreditCardNumber = form["CardNumber"];
            paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            paymentInfo.CreditCardCvv2 = form["CardCode"];
            paymentInfo.CustomValues.Add("paymenttoken",form["paymenttoken"]);
            return paymentInfo;
        }
    }
}