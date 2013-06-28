STEP 1: Install Plugin
Once you've extracted the contents of the zip file, navigate to the version folder that matches your nopcommerce installation's version number. Inside of that folder you will see a folder named "Payments.Paymill". You will need to copy this entire folder into the "Plugins" directory of your nopcommerce website.

Once you've copied the folder into your "Plugins" directory you should bring up a web browser and go to your nopcommerce store's admin area. Once you're in admin go to Configuration > Plugins. Once you're on the plugins page click the button in the top right corner labeled "Reload List of Plugins". This should refresh your available plugins. Navigate to the plugin named "Credit Card (P)" with the system name "Payments.Paymill". (note: if you wish to change how this appears to your customers, simply change the friendly name in the description.txt file in the plugin folder). Once you've found the plugin click the "Install" link to install the plugin.

Step 2: Configure Plugin
Hopefully step 1 went smoothly and you're ready to configure your plugin. Configuration is a breeze since there is only 3 items to enter. All three of these items you can get directly from the Paymill site by logging into your account and going to the settings area. You will need the following 3 items.
1: Public Key
2: Private Key
3: API URL (as of now this is https://api.paymill.com/v2/ but we left it configurable in case that changes in the future).

Once you have these 3 items entered simply click save. You should now be ready to use paymill to process your store payments. You will need to go to Configuration > Payment Methods and enable the paymill payment method for it to show up as a payment option on your store.

Step 3: String Resources
If you go to your admin, and then go to Configuration > Languages and then click on the "string resources" link for your desired language you will need to add the following items and set their values to whatever you desire..
PaymentInfo.InvalidCard 
PaymentInfo.ExpiredCard
These items were added to take care of error items returned by the paymill gateway.


Note: As of right now this plugin only supports Capture, Refund, and Partial Refund. Future enhanclements may include recurring payment support.