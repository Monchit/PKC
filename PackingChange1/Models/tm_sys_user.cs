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
    
    public partial class tm_sys_user
    {
        public string emp_code { get; set; }
        public string utype_code { get; set; }
        public System.DateTime update_dt { get; set; }
    
        public virtual tm_sys_utype tm_sys_utype { get; set; }
    }
}