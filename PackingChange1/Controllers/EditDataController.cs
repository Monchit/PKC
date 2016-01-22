using PackingChange1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.Mvc;

namespace PackingChange1.Controllers
{
    public class EditDataController : Controller
    {
        private PackingChangeEntities dbPC = new PackingChangeEntities();

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult Main()
        {
            ViewBag.ControlNo = from a in dbPC.V_Report
                                select a;

            ViewBag.SelectPlant = from a in dbPC.tm_plant
                                  where a.active == true
                                  select a;
            return View();
        }

        public ActionResult Comment()
        {
            ViewBag.ControlNo = from a in dbPC.V_Report
                                select a;
            return View();
        }

        public ActionResult Concern()
        {
            return View();
        }

        [OutputCache(Duration = 0, VaryByParam = "*", NoStore = true)]
        public ActionResult GetMain(string gpcode, string year, int runno)
        {
            var query = (from a in dbPC.td_main_data.ToList()
                         where a.gpcode == gpcode && a.year == year && a.runno == runno
                         select new
                         {
                             a.gpcode,
                             a.year,
                             a.runno,
                             a.production_type,
                             a.request_by,
                             a.reason,
                             a.chage_type,
                             a.change_detail,
                             a.condition_current,
                             a.condition_new,
                             //expected_dt = a.expected_dt.Day + "-" + a.expected_dt.Month + "-" + a.expected_dt.Year
                             expected_dt = a.expected_dt.ToString("dd-MM-yyyy")
                         }).FirstOrDefault();

            if (query != null)
                return Json(query, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, VaryByParam = "*", NoStore = true)]
        public ActionResult GetImpactPlant(string gpcode, string year, int runno)
        {
            var query = (from a in dbPC.td_impact_plant
                         where a.gpcode == gpcode && a.year == year && a.runno == runno
                         select a.plant_code).ToList();

            if (query != null)
                return Json(query, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, VaryByParam = "*", NoStore = true)]
        public ActionResult GetTran(string gpcode, string year, int runno)
        {
            var query = (from a in dbPC.V_ShowTran
                         where a.gpcode == gpcode && a.year == year && a.runno == runno
                         select new
                         {
                             a.fname,
                             a.status_name,
                             a.lv_name,
                             a.action,
                             a.comment,
                             a.gpcode,
                             a.year,
                             a.runno,
                             a.status_id,
                             a.lv_id,
                             a.org_id,
                             a.plant_code
                         }).ToList();

            if (query != null)
                return Json(query, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _FormComment(string gpcode, string year, int runno, byte status, byte lv, int org, string plant = "-")
        {
            var query = dbPC.td_transaction.Find(gpcode, year, runno, status, lv, org, plant);
            return PartialView(query);
        }

        [HttpPost]
        public ActionResult UpdateMain()
        {
            try
            {
                var gpcode = Request.Form["hdgd"];
                var year = Request.Form["hdyy"];
                var runno = int.Parse(Request.Form["hdrn"]);

                //-----------------Update Main data---------------------//

                var main = dbPC.td_main_data.Find(gpcode, year, runno);
                main.chage_type = Request.Form["selChangeType"];
                main.change_detail = Request.Form["txaDetail"];
                main.condition_current = Request.Form["txaCurrent"];
                main.condition_new = Request.Form["txaNew"];
                main.reason = Request.Form["txaReason"];
                main.expected_dt = ParseToDate(Request.Form["dtpExpectDate"].ToString());
                main.request_by = Request.Form["selReqBy"];
                main.production_type = Request.Form["selProdType"];

                //------------------Insert Impact--------------------//

                var imp_plant = Request.Form["ImpPlant[]"].ToString();
                var listPlant = imp_plant.Split(',');
                DeleteImpactPlant(gpcode, year, runno);
                foreach (var item in listPlant)
                {
                    td_impact_plant imp = new td_impact_plant();
                    imp.gpcode = main.gpcode;
                    imp.year = main.year;
                    imp.runno = main.runno;
                    imp.plant_code = item;
                    dbPC.td_impact_plant.Add(imp);
                }

                dbPC.SaveChanges();

                return RedirectToAction("Main", "EditData");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult UpdateComment()
        {
            try
            {
                var gpcode = Request.Form["hdgc"];
                var year = Request.Form["hdyy"];
                var runno = int.Parse(Request.Form["hdrn"]);
                var status = byte.Parse(Request.Form["hdst"]);
                var lv = byte.Parse(Request.Form["hdlv"]);
                var org = int.Parse(Request.Form["hdorg"]);
                var plant = Request.Form["hdpc"];

                var tran = dbPC.td_transaction.Find(gpcode, year, runno, status, lv, org, plant);
                tran.comment = Request.Form["txaEditComment"];
                dbPC.SaveChanges();

                return RedirectToAction("Comment", "EditData");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DateTime ParseToDate(string inputDT)
        {
            char[] delimiters = new char[] { '-', '/', ' ' };
            var temp = inputDT.Split(delimiters);
            DateTime outputDT = DateTime.Parse(temp[2] + "-" + temp[1] + "-" + temp[0]);
            return outputDT;
        }

        private void DeleteImpactPlant(string gpcode, string year, int runno)
        {
            using (var dbLocal = new PackingChangeEntities())
            {
                var sel_imp_plant = from a in dbLocal.td_impact_plant
                                    where a.gpcode == gpcode && a.year == year && a.runno == runno
                                    select a.plant_code;

                foreach (var item in sel_imp_plant)
                {
                    var del_imp_plant = dbLocal.td_impact_plant.Find(gpcode, year, runno, item);
                    dbLocal.td_impact_plant.Remove(del_imp_plant);
                }
                dbLocal.SaveChanges();
            }
        }

    }
}
