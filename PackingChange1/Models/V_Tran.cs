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
    
    public partial class V_Tran
    {
        public string id { get; set; }
        public string gpcode { get; set; }
        public string year { get; set; }
        public int runno { get; set; }
        public string change_detail { get; set; }
        public string chage_type { get; set; }
        public int issue_group { get; set; }
        public Nullable<byte> maxstatus { get; set; }
        public byte lv_id { get; set; }
        public string request_by { get; set; }
        public string production_type { get; set; }
        public Nullable<bool> system_update { get; set; }
        public string condition_current { get; set; }
        public string condition_new { get; set; }
        public string reason { get; set; }
        public System.DateTime expected_dt { get; set; }
        public Nullable<System.DateTime> request_date { get; set; }
    }
}
