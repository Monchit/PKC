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
    
    public partial class tm_user_level
    {
        public tm_user_level()
        {
            this.td_transaction = new HashSet<td_transaction>();
        }
    
        public byte lv_id { get; set; }
        public string lv_name { get; set; }
        public Nullable<byte> pos_min { get; set; }
        public Nullable<byte> pos_max { get; set; }
    
        public virtual ICollection<td_transaction> td_transaction { get; set; }
    }
}
