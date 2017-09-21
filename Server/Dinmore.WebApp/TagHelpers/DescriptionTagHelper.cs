using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace Dinmore.WebApp.TagHelpers
{
    [HtmlTargetElement("label", Attributes = "asp-for")]
    public class DescriptionTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            if (context.AllAttributes["title"] == null)
            {
                output.Attributes.Add("title", GetDescription(For.Metadata));
            }
        }

        private static string GetDescription(ModelMetadata metadata)
        {
            string description = metadata.Description;
            if (String.IsNullOrEmpty(description))
            {
                description = "Please add a description to the model";
            }
            return description;
        }

    }
}
