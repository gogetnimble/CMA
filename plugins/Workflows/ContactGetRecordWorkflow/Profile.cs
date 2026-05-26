using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactGetRecordWorkflow
{
    public class Profile
    {
        [System.ComponentModel.DefaultValue("")]
        public string Shareholder_ID { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string CMA_ID { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Given_Name { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Last_Name { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Birth_Date { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Other_Name1 { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Other_Name2 { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Grad_Ctry { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Grad_Coll { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Grad_Yr { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Prac_Status { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Activity { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Lic_Prov { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Lic_Ref { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Lic_Year { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Spec_Org { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Spec_Code { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Mem_Status { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Mem_Dis_Dt { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Mem_Exp_Dt { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string E_Mail { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Death_Date { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string addressLine1 { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string addressLine2 { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string addressLine3 { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string city { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string province { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string postalCode { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string country { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string homePhoneNumber { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string businessPhoneNumber { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string preferredContactMethod { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string preferredLanguage { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Standard_Audience_Type { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string GC_Audience_Type { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Staff_Audience_Type { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Contact_Type { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string Contact_Sub_Type { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string MIP_status { get; set; }

        [System.ComponentModel.DefaultValue("")]
        public string LastPTMA { get; set; }
    }
}
