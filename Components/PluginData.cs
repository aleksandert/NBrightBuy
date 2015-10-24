﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using System.Xml;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.UI.UserControls;
using DotNetNuke.UI.WebControls;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components.Interfaces;

namespace Nevoweb.DNN.NBrightBuy.Components
{
    public class PluginData
    {
        private List<NBrightInfo> _pluginList;
        private NBrightCore.TemplateEngine.TemplateGetter _templCtrl;
        private Boolean portallevel;
        private StoreSettings storeSettings;
 
        public PluginData(int portalId, Boolean systemlevel = false)
        {
            _templCtrl = NBrightBuyUtils.GetTemplateGetter(portalId,"config");

            portallevel = !systemlevel;

            if (StoreSettings.Current == null)
            {
                storeSettings = new StoreSettings(portalId);   
            }
            else
            {
                storeSettings = StoreSettings.Current;
            }

            var cachekey = "pluginlist" + portalId + "*" + systemlevel;
            var pList = NBrightBuyUtils.GetModCache(cachekey);

            if (pList != null)
            {
                // if we've created an empty cache record, clear cache data
                _pluginList = (List<NBrightInfo>)pList;
                if (_pluginList.Count == 0) DotNetNuke.Common.Utilities.DataCache.ClearCache();
            }

            if (pList != null && !storeSettings.DebugMode)
            {                
                _pluginList = (List<NBrightInfo>)pList;
            }
            else
            {
                var menuplugin = _templCtrl.GetTemplateData("menuplugin.xml", Utils.GetCurrentCulture(), true, true, portallevel, storeSettings.Settings());
                if (menuplugin != "")
                {
                    var info = new NBrightInfo();
                    info.XMLData = menuplugin;
                    _pluginList = new List<NBrightInfo>();
                    _pluginList = CalcPluginList(info);
                }
                else
                {
                    // no menuplugin.xml exists, so must be new install, get new config
                    var pluginfoldermappath = System.Web.Hosting.HostingEnvironment.MapPath(StoreSettings.NBrightBuyPath() + "/Plugins");
                    if (pluginfoldermappath != null && Directory.Exists(pluginfoldermappath))
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.Load(pluginfoldermappath + "\\menu.config");
                        pluginfoldermappath = System.Web.Hosting.HostingEnvironment.MapPath(StoreSettings.NBrightBuyPath() + "/Themes/config/default");
                        xmlDoc.Save(pluginfoldermappath + "\\menuplugin.xml");
                        //load new config
                        menuplugin = _templCtrl.GetTemplateData("menuplugin.xml", Utils.GetCurrentCulture(), true, true, portallevel, storeSettings.Settings());
                        if (menuplugin != "")
                        {
                            var Info = new NBrightInfo();
                            Info.XMLData = menuplugin;
                            _pluginList = new List<NBrightInfo>();
                            _pluginList = GetPluginList();
                        }
                    }
                }
                NBrightBuyUtils.SetModCache(0,cachekey,_pluginList);
            }
        }


        /// <summary>
        /// Search filesystem for any new plugins that have been added. Removed any deleted ones.
        /// </summary>
        public Boolean UpdateSystemPlugins()
        {
            var result = false;
            if (!portallevel) // only want to edit system level file
            {
                // remove delete plugins.
                var updated = false;
                foreach (var p in _pluginList)
                {
                    var remove = false;
                    var ctrlpath = p.GetXmlProperty("path");
                    if (ctrlpath != "")
                    {
                        var ctrlmappath = System.Web.Hosting.HostingEnvironment.MapPath(ctrlpath);
                        var assembly = p.GetXmlProperty("assembly");
                        if (ctrlpath == "" || !File.Exists(ctrlmappath))
                        {
                            if (assembly != "")
                            {
                                if (!assembly.EndsWith(".dll")) assembly = assembly + ".dll";
                                var binmappath = System.Web.Hosting.HostingEnvironment.MapPath("/bin/" + assembly);
                                if (!File.Exists(binmappath)) remove = true;
                            }
                            else
                                remove = true;
                        }

                        if (remove)
                        {
                            updated = true;
                            _pluginList.Remove(p);
                        }
                    }
                }

                if (updated)
                {
                    Save();
                    result = true;
                }

                // Add new plugins
                updated = false;
                var pluginfoldermappath = System.Web.Hosting.HostingEnvironment.MapPath(StoreSettings.NBrightBuyPath() + "/Plugins");
                if (pluginfoldermappath != null && Directory.Exists(pluginfoldermappath))
                {
                    var flist = Directory.GetFiles(pluginfoldermappath);
                    foreach (var f in flist)
                    {
                        if (f.EndsWith(".xml"))
                        {
                            var datain = File.ReadAllText(f);
                            try
                            {
                                var nbi = new NBrightInfo();
                                nbi.XMLData = datain;
                                AddPlugin(nbi);
                                updated = true;
                                File.Delete(f);
                            }
                            catch (Exception)
                            {
                                // data might not be XML complient (ignore)
                            }
                        }
                    }
                    if (updated)
                    {
                        Save();
                        result = true;
                    }
                }

            }

            if (result) NBrightBuyUtils.RemoveModCache(0);

            return result;
        }


        /// <summary>
        /// Save cart
        /// </summary>
        public void Save(Boolean portallevelsave = true)
        {
            //save cart
            var strXml = "<plugin>";
            var lp = 0;
            foreach (var info in _pluginList)
            {
                info.SetXmlProperty("genxml/hidden/index", lp.ToString(""), TypeCode.String, false);
                info.SetXmlProperty("genxml/textbox/ctrl", info.GetXmlProperty("genxml/textbox/ctrl").Trim().ToLower());
                strXml += info.XMLData;
                lp += 1;
            }
            strXml += "</plugin>";
            var nbi = new NBrightInfo(true);
            nbi.RemoveXmlNode("genxml/plugin");
            nbi.AddXmlNode(strXml, "plugin", "genxml");
            _templCtrl.SaveTemplate("menuplugin.xml", nbi.XMLData, portallevelsave);

            NBrightBuyUtils.RemoveModCache(0);
        }

        public void RemovePortalLevel()
        {
             _templCtrl.RemovePortalLevelTemplate("menuplugin.xml");
        }

        #region "base methods"

        /// <summary>
        /// Add Adddress
        /// </summary>
        /// <param name="rpData"></param>
        public String AddPlugin(Repeater rpData)
        {
            var strXml = GenXmlFunctions.GetGenXml(rpData);
            // load into NBrigthInfo class, so it's easier to get at xml values.
            var objInfoIn = new NBrightInfo();
            objInfoIn.XMLData = strXml;
            AddPlugin(objInfoIn);
            return ""; // if everything is OK, don't send a message back.
        }

        public void AddNewPlugin()
        {
            var objInfoIn = new NBrightInfo(true);
            AddPlugin(objInfoIn,0);
        }

        public String AddPlugin(NBrightInfo pluginInfo, int index = -1)
        {
            // look to see if we already have the plugin
            var ctrl = pluginInfo.GetXmlProperty("genxml/textbox/ctrl");
            if (ctrl != "")
            {
                var ctrllist = from i in _pluginList where i.GetXmlProperty("genxml/textbox/ctrl") == ctrl select i;
                var nBrightInfos = ctrllist as IList<NBrightInfo> ?? ctrllist.ToList();
                if (nBrightInfos.Any())
                {
                    index = nBrightInfos.First().GetXmlPropertyInt("genxml/hidden/index");
                }
            }

            if (index == -1)
                _pluginList.Add(pluginInfo);
            else
                UpdatePlugin(pluginInfo.XMLData, index);

            return ""; // if everything is OK, don't send a message back.
        }

        public void DeletePlugin(int index)
        {
            if (UserController.GetCurrentUserInfo().IsSuperUser) // only super user allow to remove plugins at system level.
            {
                _pluginList.RemoveAt(index);
                Save(false); // system level save
                Save(); // portal level save
            }            
        }

        public void RemovePlugin(int index)
        {
            _pluginList.RemoveAt(index);
        }

        public void UpdatePlugin(String xmlData, int index)
        {
            if (_pluginList.Count > index)
            {
                _pluginList[index].XMLData = xmlData;
            }
        }

        public void UpdatePlugin(Repeater rpData, int index)
        {
            if (_pluginList.Count > index)
            {
                var strXml = GenXmlFunctions.GetGenXml(rpData);
                UpdatePlugin(strXml, index);
            }
        }

        private List<NBrightInfo> CalcPluginList(NBrightInfo info)
        {
            var rtnList = new List<NBrightInfo>();
            var xmlNodeList = info.XMLDoc.SelectNodes("genxml/plugin/*");
            if (xmlNodeList != null)
            {
                foreach (XmlNode carNod in xmlNodeList)
                {
                    var newInfo = new NBrightInfo { XMLData = carNod.OuterXml };
                    newInfo.ItemID = rtnList.Count;
                    newInfo.SetXmlProperty("genxml/hidden/index", rtnList.Count.ToString(""));
                    newInfo.GUIDKey = newInfo.GetXmlProperty("genxml/textbox/ctrl");
                    rtnList.Add(newInfo);
                }
            }

            // get the systemlevel, incase this is an update and we have new system level provider that needs to be added
            var menupluginsys = _templCtrl.GetTemplateData("menuplugin.xml", Utils.GetCurrentCulture(), true, true, false, storeSettings.Settings());
            var infosys = new NBrightInfo();
            infosys.XMLData = menupluginsys;
            if (infosys.XMLDoc != null)
            {
                var xmlNodeList2 = infosys.XMLDoc.SelectNodes("genxml/plugin/*");
                if (xmlNodeList2 != null)
                {
                    foreach (XmlNode carNod in xmlNodeList2)
                    {
                        var newInfo = new NBrightInfo {XMLData = carNod.OuterXml};
                        newInfo.GUIDKey = newInfo.GetXmlProperty("genxml/textbox/ctrl");
                        var resultsys = rtnList.Where(p => p.GUIDKey == newInfo.GUIDKey);
                        if (!resultsys.Any())
                        {
                            // add the missing plugin to the active list
                            newInfo.ItemID = rtnList.Count;
                            newInfo.SetXmlProperty("genxml/hidden/index", rtnList.Count.ToString(""));
                            newInfo.GUIDKey = newInfo.GetXmlProperty("genxml/textbox/ctrl");
                            rtnList.Add(newInfo);
                        }
                    }
                }
            }

            return rtnList;
        }

        public List<NBrightInfo> GetPluginList()
        {
            return _pluginList;
        }

        public NBrightInfo GetShippingProviderDefault()
        {
            var l = GetShippingProviders();
            if (l.Count > 0) return l.First().Value;
            return null;
        }

        public Dictionary<String,NBrightInfo> GetShippingProviders(Boolean activeOnly = true)
        {
            return GetProviders("02", activeOnly);
        }

        public Dictionary<String, NBrightInfo> GetTaxProviders(Boolean activeOnly = true)
        {
            return GetProviders("03", activeOnly);
        }

        public Dictionary<String, NBrightInfo> GetPromoProviders(Boolean activeOnly = true)
        {
            return GetProviders("04", activeOnly);
        }
        public Dictionary<String, NBrightInfo> GetSchedulerProviders(Boolean activeOnly = true)
        {
            return GetProviders("05", activeOnly);
        }
        public Dictionary<String, NBrightInfo> GetEventsProviders(Boolean activeOnly = true)
        {
            return GetProviders("06", activeOnly);
        }

        public Dictionary<String, NBrightInfo> GetPaymentProviders(Boolean activeOnly = true)
        {
            return GetProviders("07", activeOnly);
        }

        public Dictionary<String, NBrightInfo> GetDiscountCodeProviders(Boolean activeOnly = true)
        {
            return GetProviders("08", activeOnly);
        }

        public Dictionary<String, NBrightInfo> GetFilterProviders(Boolean activeOnly = true)
        {
            return GetProviders("09", activeOnly);
        }

        public Dictionary<String, NBrightInfo> GetTemplateExtProviders(Boolean activeOnly = true)
        {
            return GetProviders("10", activeOnly);
        }

        public Dictionary<String, NBrightInfo> GetOtherProviders(Boolean activeOnly = true)
        {
            return GetProviders("99", activeOnly);
        }

        public NBrightInfo GetPaymentProviderDefault()
        {
            var l = GetPaymentProviders();
            if (l.Count > 0) return l.First().Value;
            return null;
        }


        private Dictionary<String, NBrightInfo> GetProviders(String providerType, Boolean activeOnly = true)
        {
            var pList = new Dictionary<String, NBrightInfo>();
            foreach (var p in _pluginList)
            {
                if (p.GetXmlProperty("genxml/dropdownlist/providertype") == providerType && (p.GetXmlProperty("genxml/checkbox/active") == "True" || !activeOnly))
                {
                    var ctrlkey = p.GetXmlProperty("genxml/textbox/ctrl");
                    var lp = 1;
                    while (pList.ContainsKey(ctrlkey))
                    {
                        ctrlkey = p.GetXmlProperty("genxml/textbox/assembly") + lp.ToString("");
                        lp += 1;
                    }
                    pList.Add(ctrlkey, p);
                }
            }
            return pList;
        }


        public List<NBrightInfo> GetSubList(String groupname)
        {
            var rtnList = new List<NBrightInfo>();
            if (groupname != "")
            {
                foreach (var p in _pluginList)
                {
                    if (p.GetXmlProperty("genxml/textbox/group") == groupname)
                    {
                        rtnList.Add(p);
                    }
                }
            }
            return rtnList;
        }

        public NBrightInfo GetPluginByCtrl(String ctrlname)
        {
            var ctrllist = from i in _pluginList where i.GetXmlProperty("genxml/textbox/ctrl") == ctrlname select i;
            if (ctrllist.Any()) return ctrllist.First(); 
            return null;
        }

        public NBrightInfo GetPlugin(int index)
        {
            if (index < 0 || index >= _pluginList.Count) return null;
            return _pluginList[index];
        }

        public void MovePlugin(String selectedctrl, String movetoctrl)
        {
            var selectedplugin = GetPluginByCtrl(selectedctrl);
            var moveplugin = GetPluginByCtrl(movetoctrl);
            if (selectedplugin != null & moveplugin != null)
            {
                Save(); // save so we know we have valid index
                
                //read again for valid idx
                selectedplugin = GetPluginByCtrl(selectedctrl);
                moveplugin = GetPluginByCtrl(movetoctrl);
                
                // remove and insert plugin
                var selectedidx = selectedplugin.GetXmlPropertyInt("genxml/hidden/index");
                _pluginList.RemoveAt(selectedidx);
                var toctrlidx = moveplugin.GetXmlPropertyInt("genxml/hidden/index");
                _pluginList.Insert(toctrlidx,selectedplugin);
                Save();
            }
        }

        #endregion



    }
}
