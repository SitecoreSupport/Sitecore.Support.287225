namespace Sitecore.Support.Shell.Applications.ContentManager.Dialogs.LayoutDetails
{

    // --------------------------------------------------------------------------------------------------------------------
    // <copyright file="LayoutDetails.form.cs" company="Sitecore A/S">
    //   Copyright (c) Sitecore A/S. All rights reserved.
    // </copyright>
    // <summary>
    //   Represents a gallery layout form.
    // </summary>
    // --------------------------------------------------------------------------------------------------------------------


    #region Usings

    using System;
    using System.Collections.Specialized;
    using System.Xml;
    using Diagnostics;
    using Layouts.DeviceEditor;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Globalization;
    using Sitecore.Layouts;
    using Sitecore.Shell.Applications.Dialogs;
    using Sitecore.Shell.Applications.Dialogs.LayoutDetails;
    using Sitecore.Shell.Applications.Layouts.DeviceEditor;
    using Sitecore.Shell.Framework;
    using Sitecore.Shell.Web.UI;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Xml.Patch;
    using Text;
    using Web.UI;
    using Xml;
    using Version = Sitecore.Data.Version;

    #endregion Usings

    /// <summary>
    /// Represents a gallery layout form.
    /// </summary>
    public class LayoutDetailsForm : DialogForm
    {
        #region Constants and Fields

        /// <summary>
        /// The layout panel.
        /// </summary>
        protected Border LayoutPanel;

        /// <summary>
        /// The final layout panel.
        /// </summary>
        protected Border FinalLayoutPanel;

        /// <summary>
        /// The final layout warning panel.
        /// </summary>
        protected Border FinalLayoutNoVersionWarningPanel;

        /// <summary>
        /// The shared layout tab.
        /// </summary>
        protected Tab SharedLayoutTab;

        /// <summary>
        /// The final layout tab.
        /// </summary>
        protected Tab FinalLayoutTab;

        /// <summary>
        /// The tabs.
        /// </summary>
        protected Tabstrip Tabs;

        /// <summary>
        /// The title of the warning.
        /// </summary>
        protected Literal WarningTitle;

        #endregion

        /// <summary>
        /// The tab enumeration.
        /// </summary>
        private enum TabType
        {
            /// <summary>
            /// The shared layout tab.
            /// </summary>
            Shared,

            /// <summary>
            /// The final layout tab.
            /// </summary>
            Final,

            /// <summary>
            /// The unknown tab.
            /// </summary>
            Unknown
        }

        #region Properties

        /// <summary>
        /// Gets or sets the layout.
        /// </summary>
        /// <value>The layout.</value>
        [NotNull]
        public virtual string Layout
        {
            get
            {
                return StringUtil.GetString(this.ServerProperties["Layout"]);
            }

            set
            {
                Assert.ArgumentNotNull(value, "value");

                this.ServerProperties["Layout"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the final layout.
        /// </summary>
        /// <value>The final layout.</value>
        [NotNull]
        public virtual string FinalLayout
        {
            get
            {
                string layoutDelta = this.LayoutDelta;
                if (!string.IsNullOrWhiteSpace(layoutDelta))
                {
                    if (string.IsNullOrWhiteSpace(this.Layout))
                    {
                        return layoutDelta;
                    }

                    return XmlDeltas.ApplyDelta(this.Layout, layoutDelta);
                }

                return this.Layout;
            }

            set
            {
                Assert.ArgumentNotNull(value, "value");

                if (!string.IsNullOrWhiteSpace(this.Layout))
                {
                    this.LayoutDelta = XmlUtil.XmlStringsAreEqual(this.Layout, value) ? null : XmlDeltas.GetDelta(value, this.Layout);
                    return;
                }

                this.LayoutDelta = value;
            }
        }

        /// <summary>
        /// Gets or sets the layout delta.
        /// </summary>
        /// <value>
        /// The layout delta.
        /// </value>
        [CanBeNull]
        protected virtual string LayoutDelta
        {
            get
            {
                return StringUtil.GetString(this.ServerProperties["LayoutDelta"]);
            }

            set
            {
                this.ServerProperties["LayoutDelta"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value indicating whether version has been created.
        /// </summary>
        /// <value>
        /// The value indicating whether version has been created.
        /// </value>
        protected bool VersionCreated
        {
            get
            {
                return MainUtil.GetBool(this.ServerProperties["VersionCreated"], false);
            }

            set
            {
                this.ServerProperties["VersionCreated"] = value;
            }
        }

        /// <summary>
        /// Gets the current active tab.
        /// </summary>
        /// <value>
        /// The active tab.
        /// </value>
        private TabType ActiveTab
        {
            get
            {
                int active = this.Tabs.Active;
                if (active == 0)
                {
                    return TabType.Shared;
                }

                if (active == 1)
                {
                    return TabType.Final;
                }

                return TabType.Unknown;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage([NotNull] Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Name == "item:addversion")
            {
                var currentItem = GetCurrentItem();
                Dispatcher.Dispatch(message, currentItem);
            }
            else
            {
                base.HandleMessage(message);
            }
        }

        /// <summary>
        /// Copy the device.
        /// </summary>
        /// <param name="deviceID">
        /// The device ID.
        /// </param>
        protected void CopyDevice([NotNull] string deviceID)
        {
            Assert.ArgumentNotNullOrEmpty(deviceID, "deviceID");

            var parameters = new NameValueCollection();
            parameters.Add("deviceid", deviceID);
            Context.ClientPage.Start(this, "CopyDevicePipeline", parameters);
        }

        /// <summary>
        /// Copy the device pipeline.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        protected void CopyDevicePipeline([NotNull] ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (args.IsPostBack)
            {
                if (!string.IsNullOrEmpty(args.Result) && args.Result != "undefined")
                {
                    //string[] parts = args.Result.Split('^');
                    //string sourceDevice = StringUtil.GetString(args.Parameters["deviceid"]);

                    #region Added Code

                    char[] separator = new char[] { '^' };
                    string[] parts = args.Result.Split(separator);
                    string[] values = new string[] { args.Parameters["deviceid"] };
                    string sourceDevice = StringUtil.GetString(values);

                    #endregion

                    var targetDevices = new ListString(parts[0]);
                    string targetItemId = parts[1];
                    XmlDocument doc = this.GetDoc();
                    XmlNode deviceNode = doc.SelectSingleNode("/r/d[@id='" + sourceDevice + "']");

                    if (targetItemId == WebUtil.GetQueryString("id"))
                    {
                        if (deviceNode != null)
                        {
                            this.CopyDevice(deviceNode, targetDevices);
                        }
                    }
                    else
                    {
                        if (deviceNode != null)
                        {
                            //Item targetItem = Client.GetItemNotNull(targetItemId);
                            //CopyDevice(deviceNode, targetDevices, targetItem);

                            #region Added Code

                            Sitecore.Data.Items.Item targetItem = Client.GetItemNotNull(targetItemId, Language.Parse(WebUtil.GetQueryString("la")), Sitecore.Data.Version.Parse(WebUtil.GetQueryString("vs")));
                            this.CopyDevice(deviceNode, targetDevices, targetItem);

                            #endregion
                        }
                    }

                    this.Refresh();
                }
            }
            else
            {
                XmlDocument doc = this.GetDoc();
                //WebUtil.SetSessionValue("SC_DEVICEEDITOR", doc.OuterXml);
                //var url = new UrlString(UIUtil.GetUri("control:CopyDeviceTo"));
                //url["de"] = StringUtil.GetString(args.Parameters["deviceid"]);
                //url["fo"] = WebUtil.GetQueryString("id");
                //SheerResponse.ShowModalDialog(url.ToString(), "1200px", "700px", string.Empty, true);

                #region Added Code

                UrlString str4 = new UrlString(UIUtil.GetUri("control:CopyDeviceTo"));
                string[] textArray2 = new string[] { args.Parameters["deviceid"] };
                str4["de"] = StringUtil.GetString(textArray2);
                SheerResponse.ShowModalDialog(str4.ToString(), "1200px", "700px", string.Empty, true);
               
                #endregion

                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Gets the layout value from the active tab.
        /// </summary>
        /// <returns>The layout value.</returns>
        [NotNull]
        protected string GetActiveLayout()
        {
            if (this.ActiveTab == TabType.Final)
            {
                return this.FinalLayout;
            }

            return this.Layout;
        }

        /// <summary>
        /// Gets the dialog result.
        /// </summary>
        /// <returns>An aggregated XML with the both layouts shared and final.</returns>
        [NotNull]
        protected string GetDialogResult()
        {
            var result = new LayoutDetailsDialogResult();
            result.Layout = this.Layout;
            result.FinalLayout = this.FinalLayout;
            result.VersionCreated = this.VersionCreated;

            return result.ToString();
        }

        /// <summary>
        /// Sets the layout value on the active tab.
        /// </summary>
        /// <param name="value">The value.</param>
        protected void SetActiveLayout([NotNull] string value)
        {
            Debug.ArgumentNotNull(value, "value");

            if (this.ActiveTab == TabType.Final)
            {
                this.FinalLayout = value;
            }
            else
            {
                this.Layout = value;
            }
        }

        /// <summary>
        /// Edits the placeholder.
        /// </summary>
        /// <param name="deviceID">
        /// The device ID.
        /// </param>
        /// <param name="uniqueID">
        /// The unique ID.
        /// </param>
        protected void EditPlaceholder([NotNull] string deviceID, [NotNull] string uniqueID)
        {
            Assert.ArgumentNotNull(deviceID, "deviceID");
            Assert.ArgumentNotNullOrEmpty(uniqueID, "uniqueID");

            var parameters = new NameValueCollection();
            parameters.Add("deviceid", deviceID);
            parameters.Add("uniqueid", uniqueID);
            Context.ClientPage.Start(this, "EditPlaceholderPipeline", parameters);
        }

        /// <summary>
        /// Edits the placeholder pipeline.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        protected void EditPlaceholderPipeline([NotNull] ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var doc = this.GetDoc();
            var layoutDefinition = LayoutDefinition.Parse(doc.OuterXml);
            var device = layoutDefinition.GetDevice(args.Parameters["deviceid"]);
            var placeholderDefinition = device.GetPlaceholder(args.Parameters["uniqueid"]);
            if (placeholderDefinition == null)
            {
                return;
            }

            if (args.IsPostBack)
            {
                if (!string.IsNullOrEmpty(args.Result) && args.Result != "undefined")
                {
                    string placeholderKey;
                    var item = SelectPlaceholderSettingsOptions.ParseDialogResult(args.Result, Client.ContentDatabase, out placeholderKey);
                    if (item != null)
                    {
                        placeholderDefinition.MetaDataItemId = item.Paths.FullPath;
                    }

                    if (!string.IsNullOrEmpty(placeholderKey))
                    {
                        placeholderDefinition.Key = placeholderKey;
                    }

                    this.SetActiveLayout(layoutDefinition.ToXml());
                    this.Refresh();
                }
            }
            else
            {
                var settingsItem = string.IsNullOrEmpty(placeholderDefinition.MetaDataItemId) ? null : Client.ContentDatabase.GetItem(placeholderDefinition.MetaDataItemId);
                var options = new SelectPlaceholderSettingsOptions
                {
                    TemplateForCreating = null,
                    PlaceholderKey = placeholderDefinition.Key,
                    CurrentSettingsItem = settingsItem,
                    SelectedItem = settingsItem,
                    IsPlaceholderKeyEditable = true
                };

                SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), "460px", "460px", string.Empty, true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Edits the rendering.
        /// </summary>
        /// <param name="deviceID">
        /// The device ID.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        protected void EditRendering([NotNull] string deviceID, [NotNull] string index)
        {
            Assert.ArgumentNotNull(deviceID, "deviceID");
            Assert.ArgumentNotNull(index, "index");

            var parameters = new NameValueCollection();
            parameters.Add("deviceid", deviceID);
            parameters.Add("index", index);
            Context.ClientPage.Start(this, "EditRenderingPipeline", parameters);
        }

        /// <summary>
        /// Edits the rendering pipeline.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        protected void EditRenderingPipeline([NotNull] ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var options = new RenderingParameters();
            options.Args = args;
            options.DeviceId = StringUtil.GetString(args.Parameters["deviceid"]);
            options.SelectedIndex = MainUtil.GetInt(StringUtil.GetString(args.Parameters["index"]), 0);
            options.Item = UIUtil.GetItemFromQueryString(Client.ContentDatabase);

            if (!args.IsPostBack)
            {
                XmlDocument doc = this.GetDoc();
                WebUtil.SetSessionValue("SC_DEVICEEDITOR", doc.OuterXml);
            }

            if (options.Show())
            {
                XmlDocument doc = XmlUtil.LoadXml(WebUtil.GetSessionString("SC_DEVICEEDITOR"));
                WebUtil.SetSessionValue("SC_DEVICEEDITOR", null);
                this.SetActiveLayout(GetLayoutValue(doc));
                this.Refresh();
            }
        }

        /// <summary>
        /// Raises the load event.
        /// </summary>
        /// <param name="e">
        /// The <see cref="System.EventArgs"/> instance containing the event data.
        /// </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        /// request for the page it is associated with, such as setting up a database query. At this
        /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
        /// property to determine whether the page is being loaded in response to a client postback,
        /// or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad([NotNull] EventArgs e)
        {
            Assert.CanRunApplication("Content Editor/Ribbons/Chunks/Layout");
            Assert.ArgumentNotNull(e, "e");

            base.OnLoad(e);

            this.Tabs.OnChange += (sender, args) => this.Refresh();

            if (!Context.ClientPage.IsEvent)
            {
                Item item = GetCurrentItem();
                Assert.IsNotNull(item, "Item not found");

                this.Layout = LayoutField.GetFieldValue(item.Fields[FieldIDs.LayoutField]);

                Field finalLayout = item.Fields[FieldIDs.FinalLayoutField];

                if (item.Name != Constants.StandardValuesItemName)
                {
                    this.LayoutDelta = finalLayout.GetValue(false, false) ?? finalLayout.GetInheritedValue(false);
                }
                else
                {
                    this.LayoutDelta = finalLayout.GetStandardValue();
                }

                this.ToggleVisibilityOfControlsOnFinalLayoutTab(item);

                this.Refresh();
            }

            var site = Context.Site;
            if (site == null)
            {
                return;
            }

            site.Notifications.ItemSaved += this.ItemSavedNotification;
        }

        /// <summary>
        /// Toggles the visibility of controls on final layout tab.
        /// </summary>
        /// <param name="item">The item.</param>
        protected void ToggleVisibilityOfControlsOnFinalLayoutTab([NotNull] Item item)
        {
            Debug.ArgumentNotNull(item, "item");
            var itemHasVersion = item.Versions.Count > 0;
            this.FinalLayoutPanel.Visible = itemHasVersion;
            this.FinalLayoutNoVersionWarningPanel.Visible = !itemHasVersion;
            if (!itemHasVersion)
            {
                this.WarningTitle.Text = string.Format(Translate.Text(Texts.THE_CURRENT_ITEM_DOES_NOT_HAVE_A_VERSION_IN_0), item.Language.GetDisplayName());
            }
        }

        /// <summary>
        /// Handles a click on the OK button.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        /// <remarks>
        /// When the user clicks OK, the dialog is closed by calling
        /// the <see cref="Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        protected override void OnOK([NotNull] object sender, [NotNull] EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            SheerResponse.SetDialogValue(this.GetDialogResult());
            base.OnOK(sender, args);
        }

        /// <summary>
        /// Opens the device.
        /// </summary>
        /// <param name="deviceID">
        /// The device ID.
        /// </param>
        protected void OpenDevice([NotNull] string deviceID)
        {
            Assert.ArgumentNotNullOrEmpty(deviceID, "deviceID");

            var parameters = new NameValueCollection();
            parameters.Add("deviceid", deviceID);
            Context.ClientPage.Start(this, "OpenDevicePipeline", parameters);
        }

        /// <summary>
        /// Opens the device pipeline.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        protected void OpenDevicePipeline([NotNull] ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (args.IsPostBack)
            {
                if (!string.IsNullOrEmpty(args.Result) && args.Result != "undefined")
                {
                    XmlDocument doc = XmlUtil.LoadXml(WebUtil.GetSessionString("SC_DEVICEEDITOR"));
                    WebUtil.SetSessionValue("SC_DEVICEEDITOR", null);
                    if (doc != null)
                    {
                        this.SetActiveLayout(GetLayoutValue(doc));
                    }
                    else
                    {
                        this.SetActiveLayout(string.Empty);
                    }

                    this.Refresh();
                }
            }
            else
            {
                XmlDocument doc = this.GetDoc();
                WebUtil.SetSessionValue("SC_DEVICEEDITOR", doc.OuterXml);
                var url = new UrlString(UIUtil.GetUri("control:DeviceEditor"));
                url.Append("de", StringUtil.GetString(args.Parameters["deviceid"]));
                url.Append("id", WebUtil.GetQueryString("id"));
                url.Append("vs", WebUtil.GetQueryString("vs"));
                url.Append("la", WebUtil.GetQueryString("la"));
                Context.ClientPage.ClientResponse.ShowModalDialog(new ModalDialogOptions(url.ToString())
                {
                    Response = true,
                    Width = "700"
                });

                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Copies the device2.
        /// </summary>
        /// <param name="sourceDevice">
        /// The device node.
        /// </param>
        /// <param name="devices">
        /// The devices.
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>
        private void CopyDevice([NotNull] XmlNode sourceDevice, [NotNull] ListString devices, [NotNull] Item item)
        {
            Assert.ArgumentNotNull(sourceDevice, "sourceDevice");
            Assert.ArgumentNotNull(devices, "devices");
            Assert.ArgumentNotNull(item, "item");

            Field field = this.GetLayoutField(item);
            LayoutField layoutField = field;
            XmlDocument doc = layoutField.Data;
            CopyDevices(doc, devices, sourceDevice);
            item.Editing.BeginEdit();
            field.Value = doc.OuterXml;
            item.Editing.EndEdit();
        }

        /// <summary>
        /// Called when the item is saved.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void ItemSavedNotification([NotNull] object sender, [NotNull] Sitecore.Data.Events.ItemSavedEventArgs args)
        {
            Debug.ArgumentNotNull(sender, "sender");
            Debug.ArgumentNotNull(args, "args");
            this.VersionCreated = true;
            this.ToggleVisibilityOfControlsOnFinalLayoutTab(args.Item);
            SheerResponse.SetDialogValue(this.GetDialogResult());
        }

        /// <summary>
        /// Copies the devices.
        /// </summary>
        /// <param name="doc">
        /// The doc.
        /// </param>
        /// <param name="devices">
        /// The devices.
        /// </param>
        /// <param name="sourceDevice">
        /// The source device.
        /// </param>
        private static void CopyDevices(
            [NotNull] XmlDocument doc, [NotNull] ListString devices, [NotNull] XmlNode sourceDevice)
        {
            Assert.ArgumentNotNull(doc, "doc");
            Assert.ArgumentNotNull(devices, "devices");
            Assert.ArgumentNotNull(sourceDevice, "sourceDevice");

            XmlNode node = doc.ImportNode(sourceDevice, true);

            foreach (string deviceId in devices)
            {
                if (doc.DocumentElement == null)
                {
                    continue;
                }

                XmlNode device = doc.DocumentElement.SelectSingleNode("d[@id='" + deviceId + "']");

                if (device != null)
                {
                    XmlUtil.RemoveNode(device);
                }

                device = node.CloneNode(true);
                XmlUtil.SetAttribute("id", deviceId, device);
                doc.DocumentElement.AppendChild(device);
            }
        }

        /// <summary>
        /// Gets the current item.
        /// </summary>
        /// <returns>
        /// The current item.
        /// </returns>
        /// <contract>
        ///   <ensures condition="nullable"/>
        /// </contract>
        [CanBeNull]
        private static Item GetCurrentItem()
        {
            string id = WebUtil.GetQueryString("id");
            Language language = Language.Parse(WebUtil.GetQueryString("la"));
            Version version = Version.Parse(WebUtil.GetQueryString("vs"));
            return Client.ContentDatabase.GetItem(id, language, version);
        }

        /// <summary>
        /// Gets the layout value.
        /// </summary>
        /// <param name="doc">
        /// The doc.
        /// </param>
        /// <returns>
        /// The layout value.
        /// </returns>
        /// <contract>
        ///   <requires name="doc" condition="not null"/>
        ///   <ensures condition="not null"/>
        /// </contract>
        [NotNull]
        private static string GetLayoutValue([NotNull] XmlDocument doc)
        {
            Assert.ArgumentNotNull(doc, "doc");

            XmlNodeList nodes = doc.SelectNodes("/r/d");

            if ((nodes == null) || (nodes.Count == 0))
            {
                return string.Empty;
            }

            foreach (XmlNode node in nodes)
            {
                if ((node.ChildNodes.Count > 0) || (XmlUtil.GetAttribute("l", node).Length > 0))
                {
                    return doc.OuterXml;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Copies the device.
        /// </summary>
        /// <param name="sourceDevice">
        /// The source device.
        /// </param>
        /// <param name="devices">
        /// The devices.
        /// </param>
        private void CopyDevice([NotNull] XmlNode sourceDevice, [NotNull] ListString devices)
        {
            Assert.ArgumentNotNull(sourceDevice, "sourceDevice");
            Assert.ArgumentNotNull(devices, "devices");

            XmlDocument doc = XmlUtil.LoadXml(this.GetActiveLayout());
            CopyDevices(doc, devices, sourceDevice);
            this.SetActiveLayout(doc.OuterXml);
        }

        /// <summary>
        /// Gets the doc.
        /// </summary>
        /// <returns>
        /// The doc.
        /// </returns>
        /// <contract>
        ///   <ensures condition="not null"/>
        /// </contract>
        [NotNull]
        private XmlDocument GetDoc()
        {
            var result = new XmlDocument();
            string value = this.GetActiveLayout();

            if (value.Length > 0)
            {
                result.LoadXml(value);
            }
            else
            {
                result.LoadXml("<r/>");
            }

            return result;
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        private void Refresh()
        {
            string layoutValue = this.GetActiveLayout();
            Control container = this.ActiveTab == TabType.Final ? this.FinalLayoutPanel : this.LayoutPanel;

            this.RenderLayoutGridBuilder(layoutValue, container);
        }

        /// <summary>
        /// Renders the LayoutGridBuilder.
        /// </summary>
        /// <param name="layoutValue">The layout value.</param>
        /// <param name="renderingContainer">The rendering container.</param>
        private void RenderLayoutGridBuilder([NotNull] string layoutValue, [NotNull] Control renderingContainer)
        {
            Debug.ArgumentNotNull(layoutValue, "layoutValue");
            Debug.ArgumentNotNull(renderingContainer, "renderingContainer");

            string id = renderingContainer.ID + "LayoutGrid";
            var builder = new LayoutGridBuilder
            {
                ID = id,
                Value = layoutValue,
                EditRenderingClick = "EditRendering(\"$Device\", \"$Index\")",
                EditPlaceholderClick = "EditPlaceholder(\"$Device\", \"$UniqueID\")",
                OpenDeviceClick = "OpenDevice(\"$Device\")",
                CopyToClick = "CopyDevice(\"$Device\")"
            };

            renderingContainer.Controls.Clear();

            builder.BuildGrid(renderingContainer);

            if (Context.ClientPage.IsEvent)
            {
                SheerResponse.SetOuterHtml(renderingContainer.ID, renderingContainer);
                SheerResponse.Eval("if (!scForm.browser.isIE) { scForm.browser.initializeFixsizeElements(); }");
            }
        }

        /// <summary>
        /// Gets the layout field.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        [NotNull]
        private Field GetLayoutField([NotNull] Item item)
        {
            Debug.ArgumentNotNull(item, "item");
            if (this.ActiveTab == TabType.Final)
            {
                return item.Fields[FieldIDs.FinalLayoutField];
            }

            return item.Fields[FieldIDs.LayoutField];
        }

        #endregion
    }
}