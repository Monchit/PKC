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
    
    public partial class tm_concern_group
    {
        public byte concern_group_id { get; set; }
        public int group_id { get; set; }
        public System.DateTime create_dt { get; set; }
    
        public virtual tm_sys_concern_group tm_sys_concern_group { get; set; }
    }
}
