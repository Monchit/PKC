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
    
    public partial class td_item_list
    {
        public string gpcode { get; set; }
        public string year { get; set; }
        public int runno { get; set; }
        public string item_code { get; set; }
        public string customer_name { get; set; }
        public string pp_have { get; set; }
        public string pp_lot { get; set; }
        public Nullable<int> pp_qty { get; set; }
        public string pp_check { get; set; }
        public string pp_confirm_lot { get; set; }
        public Nullable<System.DateTime> pp_eff_prod { get; set; }
        public Nullable<System.DateTime> pp_eff_pln { get; set; }
        public Nullable<int> cs_stock { get; set; }
        public string cs_repack { get; set; }
        public Nullable<System.DateTime> cs_eff_dt { get; set; }
        public string cs_lot { get; set; }
        public Nullable<bool> cs_change { get; set; }
        public string cs_check { get; set; }
        public string pln_item { get; set; }
        public string cust_no { get; set; }
        public string part_no { get; set; }
        public string wc { get; set; }
    
        public virtual td_main_data td_main_data { get; set; }
    }
}
