﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ZXing.Mobile;
using ZXing;
using ZXing.Common;

namespace BNWallet
{
    [Activity(Theme = "@style/MyTheme.BNWallet")]
    public class SendBurstScreen : Activity
    {
        EditText RecipientBurstAddress;
        EditText Message;
        EditText Amount;
        EditText Fee;
        EditText PassPhrase;
        BNWalletAPI BNWAPI;
        Toast toast;
        MobileBarcodeScanner scanner;
        View scanOverlay;
        Button buttonFlash;
        Button buttonCancelScan;
        Button CreateQRCode;
        string burstAddress;
        


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SendBurstScreen);
            MobileBarcodeScanner.Initialize(Application);
            scanner = new MobileBarcodeScanner();

            RuntimeVar RT = new RuntimeVar();
            RuntimeVarDB RTDB = new RuntimeVarDB();
            RT = RTDB.Get();


            burstAddress = Intent.GetStringExtra("BurstAddress");
            

            RecipientBurstAddress = FindViewById<EditText>(Resource.Id.sendBurstAddress);
            Message = FindViewById<EditText>(Resource.Id.sendMessage);
            Amount = FindViewById<EditText>(Resource.Id.sendAmount);
            Fee = FindViewById<EditText>(Resource.Id.sendFee);
            PassPhrase = FindViewById<EditText>(Resource.Id.sendPassphrase);
            CreateQRCode = FindViewById<Button>(Resource.Id.btnViewQRCode);


            PassPhrase.Text = RT.CurrentPassphrase;
            RecipientBurstAddress.Text = "BURST-LFYZ-4FK6-X32G-FZMHF";

            CreateQRCode.Click += delegate
            {
                Intent intent = new Intent(this, typeof(QRCodeView));
                intent.SetFlags(ActivityFlags.SingleTop);
                StartActivity(intent);
                
            };

            Button btnsend = FindViewById<Button>(Resource.Id.btnSend);
            btnsend.Click += delegate
            {
                AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                alertDialog.SetTitle("Confirmation");
                alertDialog.SetMessage("Are you sure all the details are correct?");
                alertDialog.SetPositiveButton("Yes", delegate
                {
                    BNWAPI = new BNWalletAPI();
                    GetsendMoneyResult gsmr = BNWAPI.sendMoney(RecipientBurstAddress.Text, Amount.Text+"00000000", "100000000", PassPhrase.Text, Message.Text);
                    if (gsmr.success)
                    {
                        GetTransactionResult gtr = BNWAPI.getTransaction(gsmr.transaction);
                        if (gtr.success)
                        {

                            AlertDialog.Builder ConfirmationDetailsDialog = new AlertDialog.Builder(this);
                            ConfirmationDetailsDialog.SetTitle("Confirmation Details");
                             ConfirmationDetailsDialog.SetMessage("Sender Address: "+ gtr.senderRS +"\n" + "Amount of Burst sent: " + gtr.amountNQT.Substring(0, gtr.amountNQT.Length - 8) +
                             "." + gtr.amountNQT.Substring(gtr.amountNQT.Length - 8) +"\n"+ "Fee: 1 Burst" + "\n" + "Recipient Address: " + gtr.recipientRS + "\n" + "Signature Hash: " + gsmr.signatureHash
                             + "\n" + "Transaction ID: " + gsmr.transaction + "\n" + "Block Height: " + gtr.ecBlockHeight);

                            ConfirmationDetailsDialog.SetPositiveButton("OK", delegate
                            {
                                GetAccountResult gar = BNWAPI.getAccount(gtr.senderRS);
                                if (gar.success)
                                {
                                    Intent intent = new Intent(this, typeof(InfoScreen));
                                    intent.PutExtra("WalletAddress", gar.accountRS);
                                    intent.PutExtra("WalletName", gar.name);
                                    intent.PutExtra("WalletBalance", gar.balanceNQT);
                                    intent.SetFlags(ActivityFlags.SingleTop);
                                    StartActivity(intent);
                                    Finish();
                                }
                            });
                            ConfirmationDetailsDialog.Show();
                        }
                    }
                    else
                    {
                        toast = Toast.MakeText(this, "Received API Error: " + gsmr.errorMsg, ToastLength.Long);
                        toast.Show();
                    }
                });
                alertDialog.SetNegativeButton("No", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
            };

            

            Button ScanQRCode = FindViewById<Button>(Resource.Id.btnSendQRCode);
            ScanQRCode.Click += async delegate
            {
                await ScanFunction();
            };




            // Create your application here
        }


        private async System.Threading.Tasks.Task ScanFunction()
        {
            //Tell our scanner we want to use a custom overlay instead of the default
            scanner.UseCustomOverlay = true;

            //Inflate our custom overlay from a resource layout
            scanOverlay = LayoutInflater.FromContext(this).Inflate(Resource.Layout.ScanLayout, null);

            //Find the button from our resource layout and wire up the click event
            buttonFlash = scanOverlay.FindViewById<Button>(Resource.Id.buttonFlash);
            buttonFlash.Click += (sender, e) =>
            {
                scanner.ToggleTorch();
            };

            buttonCancelScan = scanOverlay.FindViewById<Button>(Resource.Id.buttonCancel);
            buttonCancelScan.Click += (sender, e) => scanner.Cancel();

            //Set our custom overlay
            scanner.CustomOverlay = scanOverlay;

            //Set scanner possible modes
            var options = new MobileBarcodeScanningOptions();
            options.PossibleFormats = new List<BarcodeFormat>() {
                    BarcodeFormat.QR_CODE,
                    BarcodeFormat.PDF_417,
                    BarcodeFormat.CODE_39
                 };

            //Start scanning!
            var result = await scanner.Scan(options);


            string msg = "";

            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                msg = result.Text;
                if (result.BarcodeFormat == BarcodeFormat.QR_CODE)
                {
                    RecipientBurstAddress.Text = msg;
                }
            }
            else
            {
                msg = "Scanning Cancelled!";
                this.RunOnUiThread(() => Toast.MakeText(this, msg, ToastLength.Long).Show());
            }
        }

    }

}