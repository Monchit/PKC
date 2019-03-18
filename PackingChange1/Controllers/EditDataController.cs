using PackingChange1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.Mvc;
using WebCommonFunction;

namespace PackingChange1.Controllers
{
    public class EditDataController : Controller
    {
        private PackingChangeEntities dbPC = new PackingChangeEntities();
        private TNC_ADMINEntities dbTNC = new TNC_ADMINEntities();
        private TNCConversion convert = new TNCConversion();

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

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult Comment()
        {
            ViewBag.ControlNo = from a in dbPC.V_Report
                                select a;
            return View();
        }

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult Concern()
        {
            ViewBag.ControlNo = from a in dbPC.V_Tran3
                                select a;
            return View();
        }

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult ItemList()
        {
            ViewBag.ControlNo = from a in dbPC.V_Report
                                orderby a.id ascending
                                select a;
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
                         }).OrderBy(o => o.status_id).ThenBy(o => o.lv_id).ToList();

            if (query != null)
                return Json(query, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, VaryByParam = "*", NoStore = true)]
        public ActionResult GetItemList(string gpcode, string year, int runno)
        {
            var query = (from a in dbPC.td_item_list
                         where a.gpcode == gpcode && a.year == year && a.runno == runno
                         select new
                         {
                             a.gpcode,
                             a.year,
                             a.runno,
                             a.item_code,
                             a.cust_no,
                             a.customer_name,
                             a.part_no,
                             a.wc,
                             a.pp_have,
                             a.pp_lot,
                             a.pp_qty,
                             a.cs_lot
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

        public ActionResult _FormItemList(string gpcode, string year, int runno, string item)
        {
            var query = dbPC.td_item_list.Find(gpcode, year, runno, item);
            return PartialView(query);
        }

        public ActionResult _FormItemList1(string gpcode, string year, int runno, string item)
        {
            var query = dbPC.td_item_list.Find(gpcode, year, runno, item);
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

        [HttpPost]
        public ActionResult UpdateItemList()
        {
            try
            {
                var gpcode = Request.Form["hdgc"];
                var year = Request.Form["hdyy"];
                var runno = int.Parse(Request.Form["hdrn"]);
                var item = Request.Form["item_code"];

                var itemlist = dbPC.td_item_list.Find(gpcode, year, runno, item);
                itemlist.cust_no = Request.Form["cust_no"] != "" ? Request.Form["cust_no"] : null;
                itemlist.customer_name = Request.Form["customer_name"] != "" ? Request.Form["customer_name"] : null;
                itemlist.part_no = Request.Form["part_no"] != "" ? Request.Form["part_no"] : null;
                itemlist.wc = Request.Form["wc"] != "" ? Request.Form["wc"] : null;
                itemlist.pp_have = Request.Form["pp_have"] != "" ? Request.Form["pp_have"] : null;
                itemlist.pp_lot = Request.Form["pp_lot"] != "" ? Request.Form["pp_lot"] : null;
                itemlist.pp_qty = Request.Form["pp_qty"] != "" ? int.Parse(Request.Form["pp_qty"].ToString()) : 0;
                itemlist.pp_confirm_lot = Request.Form["pp_confirm_lot"] != "" ? Request.Form["pp_confirm_lot"] : null;
                if (Request.Form["pp_eff_prod"] != "")
                {
                    itemlist.pp_eff_prod = convert.DateDisplayToDB(Request.Form["pp_eff_prod"]);
                }
                if (Request.Form["pp_eff_pln"] != "")
                {
                    itemlist.pp_eff_pln = convert.DateDisplayToDB(Request.Form["pp_eff_pln"]);
                }

                dbPC.SaveChanges();
                return RedirectToAction("ItemList", "EditData");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult UpdateItemList1()
        {
            try
            {
                var gpcode = Request.Form["hdgc"];
                var year = Request.Form["hdyy"];
                var runno = int.Parse(Request.Form["hdrn"]);
                var item = Request.Form["item_code"];

                var itemlist = dbPC.td_item_list.Find(gpcode, year, runno, item);
                itemlist.cs_stock = Request.Form["cs_stock"] != "" ? int.Parse(Request.Form["cs_stock"]) : 0;
                itemlist.cs_repack = Request.Form["cs_repack"] != "" ? Request.Form["cs_repack"] : null;
                itemlist.cs_lot = Request.Form["cs_lot"] != "" ? Request.Form["cs_lot"] : null;
                itemlist.pln_item = Request.Form["pln_item"] != "" ? Request.Form["pln_item"] : null;
                if (Request.Form["cs_eff_dt"] != "")
                {
                    itemlist.cs_eff_dt = convert.DateDisplayToDB(Request.Form["cs_eff_dt"]);
                }
                if (Request.Form["cs_change"] != "")
                {
                    itemlist.cs_change = bool.Parse(Request.Form["cs_change"]);
                }

                dbPC.SaveChanges();
                return RedirectToAction("ItemList", "EditData");
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

        [OutputCache(Duration = 0, VaryByParam = "*", NoStore = true)]
        public ActionResult GetSelectedTemp(string gpcode, string year, int runno, byte gtype)
        {
            var get_selected = (from a in dbPC.td_temp_concern
                                where a.gpcode == gpcode && a.year == year
                                && a.runno == runno && a.concern_group_id == gtype
                                select a.group_id).ToList();
            var get_inf = dbTNC.tnc_group_master.Where(w => get_selected.Contains(w.id))
                .Select(s => new { id = s.id, text = s.group_name });
            if (get_inf != null)
                return Json(get_inf, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateConcern(string gpcode, string year, int runno, byte congid, string congroup)
        {
            try
            {
                if (!string.IsNullOrEmpty(congroup))
                {
                    //Delete
                    ((System.Data.Entity.Infrastructure.IObjectContextAdapter)dbPC)
                    .ObjectContext.ExecuteStoreCommand("DELETE FROM td_temp_concern WHERE gpcode='" + gpcode + "' and year='" + year
                                                    + "' and runno='" + runno + "' and concern_group_id='" + congid + "'");
                    dbPC.SaveChanges();

                    int[] groups = congroup.Split(',').Select(s => Convert.ToInt32(s)).ToArray();
                    using (var dbLocal = new PackingChangeEntities())
                    {
                        //Add
                        foreach (var item in groups)
                        {
                            var check = dbLocal.td_temp_concern.Find(gpcode, year, runno, congid, item);
                            if (check == null)
                            {
                                var temp = new td_temp_concern();
                                temp.gpcode = gpcode;
                                temp.year = year;
                                temp.runno = runno;
                                temp.concern_group_id = congid;
                                temp.group_id = item;
                                dbLocal.td_temp_concern.Add(temp);
                            }
                        }
                        dbLocal.SaveChanges();
                    }
                } 

                return Json("Update Successful.");
            }
            catch (Exception)
            {
                return Json("Error");
            }
        }
    }
}
