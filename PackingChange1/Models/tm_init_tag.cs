//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PackingChange1.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class tm_init_tag
    {
        public tm_init_tag()
        {
            this.td_salecs = new HashSet<td_salecs>();
        }
    
        public byte init_tag_id { get; set; }
        public string init_tag_name { get; set; }
        public bool active { get; set; }
    
        public virtual ICollection<td_salecs> td_salecs { get; set; }
    }
}
