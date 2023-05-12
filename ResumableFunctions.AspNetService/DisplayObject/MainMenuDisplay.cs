using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.AspNetService.DisplayObject
{
    public record MainMenuItem(string DisplayName,string PartialViewUrl);
    public class MainMenuDisplay
    {
        public MainMenuItem[] Items { get; set; }
        public string BackLinkText { get; set; }
        public string BackLink { get; set; }
    }
}
