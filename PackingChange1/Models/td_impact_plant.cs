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
    
    public partial class td_impact_plant
    {
        public string gpcode { get; set; }
        public string year { get; set; }
        public int runno { get; set; }
        public string plant_code { get; set; }
    
        public virtual td_main_data td_main_data { get; set; }
    }
}
