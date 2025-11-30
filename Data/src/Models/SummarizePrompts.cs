using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodPT.Data.Models
{
    internal class SummarizePrompts: XPObject
    {
        public SummarizePrompts(Session session) : base(session) { }
        bool active;
        private string role;
        public string Role
        {
            get => role;
            set => SetPropertyValue(nameof(Role), ref role, value);
        }
        private string prompt;
        public string Prompt
        {
            get => prompt;
            set => SetPropertyValue(nameof(Prompt), ref prompt, value);
        }
        public bool Active
        {
            get => active;
            set => SetPropertyValue(nameof(Active), ref active, value);
        }
    }
}
