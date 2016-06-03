using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebCommonFunction;

namespace PKCRemindEmail
{
    public class Program
    {
        static void Main(string[] args)
        {
            PackingChangeEntities db = new PackingChangeEntities();

            try
            {
                var query = from a in db.td_transaction
                            where a.act_id == null && a.status_id < 100
                            //orderby a.lv_id, a.org_id ascending
                            select a;

                //int i = 1;
                foreach (var q in query)
                {
                    GetEmail gm = new GetEmail();

                    string mailto = gm.ByOrg(q.org_id, q.lv_id);

                    if (!string.IsNullOrEmpty(mailto))
                    {
                        TNCUtility tnc_util = new TNCUtility();

                        string pkc = "PKC-" + q.gpcode + "-" + q.year + "-" + q.runno.ToString("000");

                        string int_link = "http://webExternal/PKC/Home";
                        string subject = "[Remind] Packing Change waiting for process (" + pkc + ")";
                        string body = //"Mail To : " + mailto + "<br />Dear. All Concern,<br /><br />" +
                            "You have Packing Change No. <b>" + pkc +
                            "</b> waiting for Process at State : " + q.tm_status.status_name + "<br />" +
                            "<br /><a href='" + int_link + "/MainPCO?id=" + pkc + "'>Packing Change Online</a><br />" +
                            "<br /><b>Best Regard.<br />" + "From Packing Change-Admin</b>";

                        tnc_util.SendMail(31, "TNCAutoMail-PKC@nok.co.th", mailto, subject, body);//For Real
                        //tnc_util.SendMail(31, "TNCAutoMail-PKC@nok.co.th", "noppamas@nok.co.th", subject, body);//For Test

                        //Console.WriteLine(i + ". " + pkc);
                        //i++;
                    }
                    //break;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error PKC E-mail Remind");
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
