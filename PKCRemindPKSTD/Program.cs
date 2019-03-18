using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebCommonFunction;

namespace PKCRemindPKSTD
{
    public class Program
    {
        static void Main(string[] args)
        {
            PackingChangeEntities db = new PackingChangeEntities();
            
            try
            {
                string email_test = "monchit@nok.co.th,chonticha@nok.co.th";//,sarin@nok.co.th";
                string int_link = "http://webExternal/PKC/Home/MainPCO?id=";

                var sale = from a in db.v_pack_std_qa_sale
                           where a.status == "Pending"
                           group a by a.sale into g
                           select new { sales = g.Key, list = g };

                foreach (var g in sale)
                {
                    GetEmail gm = new GetEmail();
                    string mailto = gm.ByOrg(g.sales.Value);

                    TNCUtility tnc_util = new TNCUtility();
                    
                    string subject = "[Remind] Please Follow up Packing Standard";
                    string body = "<style> table { border-collapse: collapse; } table, td, th { border: 1px solid black; } td, th { padding: 4px; }</style>";
                    //body += "Mail To : " + mailto + "<br />"; //For Test
                    body += "Dear. All Concern,<br /><br />";
                    body += "<table><tr><td>Control No.</td><td>Change Detail</td></tr>";

                    foreach (var l in g.list)
                    {
                        var pkc = "PKC-" + l.gpcode + "-" + l.year + "-" + l.runno.ToString("000");
                        body += "<tr><td><a href='" + int_link + pkc + "'>" + pkc + "</a></td><td>" + l.change_detail + "</td></tr>";
                    }

                    body += "</table><br />Packing Change Online";

                    //Console.WriteLine(body);
                    tnc_util.SendMail(31, "TNCAutoMail-PKC@nok.co.th", mailto, subject, body, bcc:email_test);//For Real
                    //tnc_util.SendMail(31, "TNCAutoMail-PKC@nok.co.th", "monchit@nok.co.th", subject, body);//For Test
                }

                //--------------------------------------------//

                var qa = from a in db.v_pack_std_qa_sale
                           where a.status == null
                           group a by a.qa into g
                           select new { qa = g.Key, list = g };

                foreach (var g in qa)
                {
                    GetEmail gm = new GetEmail();
                    string mailto = gm.ByOrg(g.qa);

                    TNCUtility tnc_util = new TNCUtility();

                    string subject = "[Remind] Please Add Packing Standard";
                    string body = "<style> table { border-collapse: collapse; } table, td, th { border: 1px solid black; } td, th { padding: 4px; }</style>";//"Dear. All Concern,<br /><br />";
                    //body += "Mail To : " + mailto;
                    body += "Dear. All Concern,<br /><br />";
                    body += "<table><tr><td>Control No.</td><td>Change Detail</td></tr>";//For Test

                    foreach (var l in g.list)
                    {
                        var pkc = "PKC-" + l.gpcode + "-" + l.year + "-" + l.runno.ToString("000");
                        body += "<tr><td><a href='" + int_link + pkc + "'>" + pkc + "</a></td><td>" + l.change_detail + "</td></tr>";
                    }

                    body += "</table><br />Packing Change Online";

                    //Console.WriteLine(body);
                    tnc_util.SendMail(31, "TNCAutoMail-PKC@nok.co.th", mailto, subject, body, bcc: "monchit@nok.co.th");//For Real
                    //tnc_util.SendMail(31, "TNCAutoMail-PKC@nok.co.th", "monchit@nok.co.th", subject, body, flag: 0);//For Test
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error PKC E-mail Remind QA, Sales");
                Console.ReadLine();
            }
        }
    }

    public class GetEmail
    {
        public string ByOrg(int org, byte lv = 0)
        {
            TNC_ADMINEntities dbTNC = new TNC_ADMINEntities();

            string email = "";

            if (lv == 0) // All email in group
            {
                var get_mail = from a in dbTNC.tnc_user
                               where a.emp_group == org && !string.IsNullOrEmpty(a.email) && a.emp_status == "A"
                               select a.email;

                foreach (var item in get_mail)
                {
                    email += "," + item;
                }
            }
            else if (lv == 1) // Eng.
            {
                var get_mail = from a in dbTNC.V_Employee_Info
                               where (a.position_level > 0 && a.position_level <= 4) && a.group_id == org
                                    && !string.IsNullOrEmpty(a.email) && a.emp_status == "A"
                               select a.email;

                foreach (var item in get_mail)
                {
                    email += "," + item;
                }
            }
            else if (lv == 2) // Group
            {
                var get_mail = from a in dbTNC.V_Employee_Info
                               where a.position_level == 5 && a.group_id == org
                                    && !string.IsNullOrEmpty(a.email) && a.emp_status == "A"
                               orderby a.emp_position ascending
                               select a.email;

                foreach (var item in get_mail)
                {
                    email += "," + item;
                    break;
                }
            }
            else if (lv == 3) // Dept.
            {
                var get_mail = (from a in dbTNC.View_Organization
                                where a.dept_id == org
                                select a).FirstOrDefault();

                if (get_mail != null)
                {
                    email = !string.IsNullOrEmpty(get_mail.DeptMgr_email) ? "," + get_mail.DeptMgr_email : "," + get_mail.PlantMgr_email;
                }
            }
            else if (lv == 4) // Plant/Div
            {
                var get_mail = (from a in dbTNC.View_Organization
                                where a.plant_id == org
                                select a).FirstOrDefault();

                if (get_mail != null)
                {
                    email = !string.IsNullOrEmpty(get_mail.PlantMgr_email) ? "," + get_mail.PlantMgr_email : "";
                }
            }
            else if (lv == 5) // MD
            {
                var get_mail = from a in dbTNC.tnc_user
                               where a.emp_position == 9 && a.emp_plant == org && !string.IsNullOrEmpty(a.email)
                               select a.email;

                foreach (var item in get_mail)
                {
                    email += "," + item;
                }
            }

            return !string.IsNullOrEmpty(email) ? email.Substring(1) : email;
        }
    }
}
