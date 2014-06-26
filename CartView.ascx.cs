// --- Copyright (c) notice NevoWeb ---
//  Copyright (c) 2014 SARL NevoWeb.  www.nevoweb.com. The MIT License (MIT).
// Author: D.C.Lee
// ------------------------------------------------------------------------
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ------------------------------------------------------------------------
// This copyright notice may NOT be removed, obscured or modified without written consent from the author.
// --- End copyright notice --- 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using NEvoWeb.Modules.NB_Store;
using Nevoweb.DNN.NBrightBuy.Base;
using Nevoweb.DNN.NBrightBuy.Components;
using DataProvider = DotNetNuke.Data.DataProvider;

namespace Nevoweb.DNN.NBrightBuy
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class CartView : NBrightBuyBase
    {

        private String _catid = "";
        private String _catname = "";
        private GenXmlTemplate _templateHeader;//this is used to pickup the meta data on page load.
        private String _templH = "";
        private String _templD = "";
        private String _templDfoot = "";
        private String _templF = "";
        private String _entryid = "";
        private String _tabid = "";
        private CartData _cartInfo;
        private AddressData _addressData;

        #region Event Handlers


        override protected void OnInit(EventArgs e)
        {
            base.EntityTypeCode = "CART";
            base.CtrlTypeCode = "CART";
            base.EntityTypeCodeLang = "";
            base.DisableUserInfo = true;

            base.OnInit(e);

            _cartInfo = new CartData(PortalId, StoreSettings.Current.Get("DataStorageType"));
            _addressData = new AddressData();

            if (ModSettings.Get("themefolder") == "")  // if we don't have module setting jump out
            {
                rpDataH.ItemTemplate = new GenXmlTemplate("NO MODULE SETTINGS");
                return;
            }

            try
            {
                _templH = ModSettings.Get("txtdisplayheader");
                _templD = ModSettings.Get("txtdisplaybody");
                _templDfoot = ModSettings.Get("txtdisplaybodyfoot");
                _templF = ModSettings.Get("txtdisplayfooter");

                const string templASH = "addressselectheader.html";
                const string templASB = "addressselectbody.html";
                const string templASF = "addressselectfooter.html";
                const string templAB = "checkoutaddrbill.html";
                const string templAS = "checkoutaddrship.html";
                const string templS = "checkoutship.html";
                const string templT = "checkouttax.html";
                const string templP = "checkoutpromo.html";
                const string templE = "checkoutextra.html";
                const string templD = "checkoutdetails.html";

                // Get Display Header
                var rpDataHTempl = ModCtrl.GetTemplateData(ModSettings, _templH, Utils.GetCurrentCulture(), DebugMode);

                rpDataH.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(rpDataHTempl, ModSettings.Settings(), PortalSettings.HomeDirectory);
                _templateHeader = (GenXmlTemplate) rpDataH.ItemTemplate;

                // insert page header text
                NBrightBuyUtils.IncludePageHeaders(ModCtrl, ModuleId, Page, _templateHeader, ModSettings.Settings(), null, DebugMode);

                // Get Display Body
                var rpDataTempl = ModCtrl.GetTemplateData(ModSettings, _templD, Utils.GetCurrentCulture(), DebugMode);
                rpData.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(rpDataTempl, ModSettings.Settings(), PortalSettings.HomeDirectory);

                // Get Display Footer
                var rpDataFTempl = ModCtrl.GetTemplateData(ModSettings, _templF, Utils.GetCurrentCulture(), DebugMode);
                rpDataF.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(rpDataFTempl, ModSettings.Settings(), PortalSettings.HomeDirectory);

                var carttype = ModSettings.Get("ddlcarttype");
                if (carttype == "2")
                {
                    rpAddrListH.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templASH, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpAddrListB.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templASB, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpAddrListF.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templASF, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpShip.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templS, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpTax.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templT, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpPromo.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templP, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpExtra.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templE, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpDetailDisplay.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templD, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpAddrB.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templAB, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                    rpAddrS.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(ModCtrl.GetTemplateData(ModSettings, templAS, Utils.GetCurrentCulture(), DebugMode), ModSettings.Settings(), PortalSettings.HomeDirectory);
                }
            }
            catch (Exception exc)
            {
                rpDataF.ItemTemplate = new GenXmlTemplate(exc.Message, ModSettings.Settings());
                // catch any error and allow processing to continue, output error as footer template.
            }

        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                if (Page.IsPostBack == false)
                {
                    PageLoad();
                }
            }
            catch (Exception exc) //Module failed to load
            {
                //display the error on the template (don;t want to log it here, prefer to deal with errors directly.)
                var l = new Literal();
                l.Text = exc.ToString();
                checkoutlayout.Controls.Add(l);
            }
        }

        private void PageLoad()
        {

            #region " Cart List Data Repeater"


            if (_templD.Trim() != "") // if we don;t have a template, don't do anything
            {
                var l = _cartInfo.GetCartItemList();
                rpData.DataSource = l;
                rpData.DataBind();
            }

            var cartL = new List<NBrightInfo>();
            cartL.Add(_cartInfo.GetCart());

            // display header
            rpDataH.DataSource = cartL;
            rpDataH.DataBind();

            // display footer
            rpDataF.DataSource = cartL;
            rpDataF.DataBind();

            #endregion

            var carttype =  ModSettings.Get("ddlcarttype");
            if (carttype == "2")
            {
                var l = new Literal();
                l.Text = ModCtrl.GetTemplateData(ModSettings, "checkoutlayout.html", Utils.GetCurrentCulture(), DebugMode);
                checkoutlayout.Controls.Add(l);

                #region "Address List Data Repeater"

                var addrlist = _addressData.GetAddressList();
                if (addrlist.Count == 0)
                {
                    rpAddrListH.Visible = false;
                    rpAddrListB.Visible = false;
                    rpAddrListF.Visible = false;
                }
                else
                {
                    rpAddrListB.DataSource = addrlist;
                    rpAddrListB.DataBind();
                    // display header
                    rpAddrListH.DataSource = cartL;
                    rpAddrListH.DataBind();
                    // display footer
                    rpAddrListF.DataSource = cartL;
                    rpAddrListF.DataBind();
                }

                #endregion

                var objl = new List<NBrightInfo>();
                objl.Add(_cartInfo.GetBillingAddress());
                rpAddrB.DataSource = objl;
                rpAddrB.DataBind();

                objl = new List<NBrightInfo>();
                objl.Add(_cartInfo.GetShippingAddress());
                rpAddrS.DataSource = objl;
                rpAddrS.DataBind();

                // display shipping input form
                objl = new List<NBrightInfo>();
                objl.Add(_cartInfo.GetShipData());
                rpShip.DataSource = objl;
                rpShip.DataBind();

                // display Promo input form
                objl = new List<NBrightInfo>();
                objl.Add(_cartInfo.GetPromoCode());
                rpPromo.DataSource = objl;
                rpPromo.DataBind();

                // display Tax input form
                objl = new List<NBrightInfo>();
                objl.Add(_cartInfo.GetTaxData());
                rpTax.DataSource = objl;
                rpTax.DataBind();

                // display extra input form
                objl = new List<NBrightInfo>();
                objl.Add(_cartInfo.GetExtraInfo());
                rpExtra.DataSource = objl;
                rpExtra.DataBind();

                // display cart details
                rpDetailDisplay.DataSource = cartL;
                rpDetailDisplay.DataBind();
            }

        }

        #endregion

        #region  "Events "

        protected void CtrlItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var cArg = e.CommandArgument.ToString();
            var param = new string[3];
            if (Utils.RequestParam(Context, "eid") != "") param[0] = "eid=" + Utils.RequestParam(Context, "eid"); 

            switch (e.CommandName.ToLower())
            {
                case "addqty":
                    if (!Utils.IsNumeric(cArg)) cArg = "1";
                    if (Utils.IsNumeric(cArg))
                    {
                        _cartInfo.UpdateItemQty(e.Item.ItemIndex,Convert.ToInt32(cArg));
                        _cartInfo.Save();
                    }
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "removeqty":
                    if (!Utils.IsNumeric(cArg)) cArg = "-1";
                    if (Utils.IsNumeric(cArg))
                    {
                        _cartInfo.UpdateItemQty(e.Item.ItemIndex, Convert.ToInt32(cArg));
                        _cartInfo.Save();
                    }
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "deletecartitem":
                    _cartInfo.RemoveItem(e.Item.ItemIndex);
                    _cartInfo.Save();
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "deletecart":
                    _cartInfo.DeleteCart();
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "updatecart":
                    UpdateCart();
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "confirm":
                    UpdateCartData();
                    UpdateCart();
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "order":
                    UpdateCartData();
                    UpdateCart();
                    var paytabid = ModSettings.Get("paymenttab");
                    if (!Utils.IsNumeric(paytabid)) paytabid = TabId.ToString("");
                    Response.Redirect(Globals.NavigateURL(Convert.ToInt32(paytabid), "", param), true);
                    break;
            }

        }

        #endregion

        #region "Methods"


        private void UpdateCart()
        {
            foreach (RepeaterItem i in rpData.Items)
            {
                var strXml = GenXmlFunctions.GetGenXml(i);
                var cInfo = new NBrightInfo();
                cInfo.XMLData = strXml;
                _cartInfo.MergeCartInputData(i.ItemIndex, cInfo);
                _cartInfo.Save(DebugMode);
            }           

        }

        private void UpdateCartData()
        {
            //update address
            _cartInfo.AddShippingAddress(rpAddrS); //add shipping address to cart
            _cartInfo.AddBillingAddress(rpAddrB); //add billing address to cart

            //update extra data
            _cartInfo.AddExtraInfo(rpExtra);
            _cartInfo.AddPromoCode(rpPromo);
            _cartInfo.AddTaxData(rpTax);
            _cartInfo.AddShipData(rpShip);
            _cartInfo.Save(DebugMode);
        }

        #endregion


    }

}
