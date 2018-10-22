namespace Sitecore.Support.Shell.Applications.Layouts.DeviceEditor
{
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Shell.Framework;
    using Sitecore.Text;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Web.UI.WebControls;
    using System;
    using System.IO;
    using System.Web;
    using System.Web.UI;

    public class CopyDeviceToForm : DialogForm
    {
        protected Sitecore.Web.UI.HtmlControls.DataContext DataContext;
        protected Border Devices;
        protected TreeviewEx Treeview;

        private Item GetCurrentItem(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            string str = message["id"];
            Item folder = this.DataContext.GetFolder();
            Language language = Context.Language;
            if (folder != null)
            {
                language = folder.Language;
            }
            if (!string.IsNullOrEmpty(str))
            {
                return Client.ContentDatabase.GetItem(str, language);
            }
            return folder;
        }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            Dispatcher.Dispatch(message, this.GetCurrentItem(message));
            base.HandleMessage(message);
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                this.DataContext.GetFromQueryString();
                this.RenderDevices();
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            Item selectionItem = this.Treeview.GetSelectionItem();
            if (selectionItem == null)
            {
                SheerResponse.Alert("Select an item.", new string[0]);
            }
            if (selectionItem == null)
            {
                SheerResponse.Alert("The target item could not be found.", new string[0]);
            }
            else
            {
                ListString str = new ListString();
                foreach (string str2 in HttpContext.Current.Request.Form.Keys)
                {
                    if (!string.IsNullOrEmpty(str2) && str2.StartsWith("de_", StringComparison.InvariantCulture))
                    {
                        str.Add(ShortID.Decode(StringUtil.Mid(str2, 3)));
                    }
                }
                if (str.Count == 0)
                {
                    SheerResponse.Alert("Please select one or more devices.", new string[0]);
                }
                else
                {
                    Registry.SetValue("/Current_User/DeviceEditor/CopyDevices/TargetDevices", str.ToString());
                    SheerResponse.SetDialogValue(str + "^" + selectionItem.ID);
                    base.OnOK(sender, args);
                }
            }
        }

        private void RenderDevices()
        {
            ListString str = new ListString(Registry.GetValue("/Current_User/DeviceEditor/CopyDevices/TargetDevices"));
            Item itemNotNull = Client.GetItemNotNull(ItemIDs.DevicesRoot);
            HtmlTextWriter writer = new HtmlTextWriter(new StringWriter());
            foreach (Item item2 in itemNotNull.Children)
            {
                string str2 = "de_" + item2.ID.ToShortID();
                writer.Write("<div style=\"padding:2px\">");
                writer.Write("<input type=\"checkbox\" id=\"" + str2 + "\" name=\"" + str2 + "\"");
                if (str.Contains(item2.ID.ToString()))
                {
                    writer.Write(" checked=\"checked\"");
                }
                writer.Write(" />");
                writer.Write("<label for=\"" + str2 + "\">");
                writer.Write(item2.DisplayName);
                writer.Write("</label>");
                writer.Write("</div>");
            }
            this.Devices.InnerHtml = writer.InnerWriter.ToString();
        }
    }
}