using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Links;
using Sitecore.Text;
using Sitecore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Sitecore.Support.Data.Fields
{
  public class LayoutField : Sitecore.Data.Fields.LayoutField
  {
    private readonly XmlDocument data;
    public LayoutField(Item item) : this(item.Fields[FieldIDs.FinalLayoutField])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Sitecore.Data.Fields.LayoutField" /> class. Creates LayoutField from specific item.
    /// </summary>
    /// <param name="item">Item to get layout for.</param>
    /// <param name="runtimeValue">The runtime value.</param>
    public LayoutField(Item item, string runtimeValue) : this(item.Fields[FieldIDs.FinalLayoutField], runtimeValue)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:Sitecore.Data.Fields.LayoutField" /> class. Creates a new <see cref="T:Sitecore.Data.Fields.LayoutField" /> instance.</summary>
    /// <param name="innerField">Inner field.</param>
    public LayoutField(Field innerField) : base(innerField)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
      this.data = this.LoadData();
    }

    /// <summary>Initializes a new instance of the <see cref="T:Sitecore.Data.Fields.LayoutField" /> class.</summary>
    /// <param name="innerField">The inner field.</param>
    /// <param name="runtimeValue">The runtime value.</param>
    public LayoutField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
      Assert.ArgumentNotNullOrEmpty(runtimeValue, "runtimeValue");
      this.data = this.LoadData();
    }
    private XmlDocument LoadData()
    {
      string value = base.Value;
      if (!string.IsNullOrEmpty(value))
      {
        return XmlUtil.LoadXml(value);
      }
      return XmlUtil.LoadXml("<r/>");
    }
    public override void RemoveLink(ItemLink itemLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      string value = base.Value;
      if (string.IsNullOrEmpty(value))
      {
        return;
      }
      LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);
      ArrayList devices = layoutDefinition.Devices;
      if (devices == null)
      {
        return;
      }
      string b = itemLink.TargetItemID.ToString();
      for (int i = devices.Count - 1; i >= 0; i--)
      {
        DeviceDefinition deviceDefinition = devices[i] as DeviceDefinition;
        if (deviceDefinition != null)
        {
          if (deviceDefinition.ID == b)
          {
            devices.Remove(deviceDefinition);
          }
          else if (deviceDefinition.Layout == b)
          {
            deviceDefinition.Layout = null;
          }
          else
          {
            if (deviceDefinition.Placeholders != null)
            {
              string targetPath = itemLink.TargetPath;
              bool flag = false;
              for (int j = deviceDefinition.Placeholders.Count - 1; j >= 0; j--)
              {
                PlaceholderDefinition placeholderDefinition = deviceDefinition.Placeholders[j] as PlaceholderDefinition;
                if (placeholderDefinition != null && (string.Equals(placeholderDefinition.MetaDataItemId, targetPath, StringComparison.InvariantCultureIgnoreCase) || string.Equals(placeholderDefinition.MetaDataItemId, b, StringComparison.InvariantCultureIgnoreCase)))
                {
                  deviceDefinition.Placeholders.Remove(placeholderDefinition);
                  flag = true;
                }
              }
              if (flag)
              {
                goto IL_294;
              }
            }
            if (deviceDefinition.Renderings != null)
            {
              for (int k = deviceDefinition.Renderings.Count - 1; k >= 0; k--)
              {
                RenderingDefinition renderingDefinition = deviceDefinition.Renderings[k] as RenderingDefinition;
                if (renderingDefinition != null)
                {
                  if (renderingDefinition.Datasource == itemLink.TargetPath)
                  {
                    renderingDefinition.Datasource = string.Empty;
                  }
                  if (renderingDefinition.ItemID == b)
                  {
                    deviceDefinition.Renderings.Remove(renderingDefinition);
                  }
                  if (renderingDefinition.Datasource == b)
                  {
                    renderingDefinition.Datasource = string.Empty;
                  }
                  #region Sitecore.Support.257538
                  if (!string.IsNullOrEmpty(renderingDefinition.Parameters) && !string.IsNullOrEmpty(renderingDefinition.ItemID))
                  #endregion
                  {
                    Item item = base.InnerField.Database.GetItem(renderingDefinition.ItemID);
                    if (item == null)
                    {
                      goto IL_286;
                    }
                    RenderingParametersFieldCollection parametersFields = this.GetParametersFields(item, renderingDefinition.Parameters);
                    foreach (CustomField current in parametersFields.Values)
                    {
                      if (!string.IsNullOrEmpty(current.Value))
                      {
                        current.RemoveLink(itemLink);
                      }
                    }
                    renderingDefinition.Parameters = parametersFields.GetParameters().ToString();
                  }
                  if (renderingDefinition.Rules != null)
                  {
                    RulesField rulesField = new RulesField(base.InnerField, renderingDefinition.Rules.ToString());
                    rulesField.RemoveLink(itemLink);
                    renderingDefinition.Rules = XElement.Parse(rulesField.Value);
                  }
                }
                IL_286:;
              }
            }
          }
        }
        IL_294:;
      }
      base.Value = layoutDefinition.ToXml();
    }
    private RenderingParametersFieldCollection GetParametersFields(Item layoutItem, string renderingParameters)
    {
      UrlString parameters = new UrlString(renderingParameters);
      RenderingParametersFieldCollection result;
      RenderingParametersFieldCollection.TryParse(layoutItem, parameters, out result);
      return result;
    }

  }
}
