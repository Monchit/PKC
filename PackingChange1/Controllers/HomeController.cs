using PackingChange1.Models;
using Rotativa;
using Rotativa.Options;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.Mvc;
using WebCommonFunction;

namespace PackingChange1.Controllers
{
    public class HomeController : Controller
    {
        private TNCSecurity secure = new TNCSecurity();
        private PackingChangeEntities dbPC = new PackingChangeEntities();
        private TNC_ADMINEntities dbTNC = new TNC_ADMINEntities();
        private PCREntities dbPCR = new PCREntities();
        private TNCFileDirectory dir = new TNCFileDirectory();
        private TNCUtility util = new TNCUtility();

        //------------------View---------------------//

        public ActionResult Index(string key = null)
        {
            if (key != null)
            {
                return Login(key);
            }
            else
            {
                ViewBag.Message = "";
                return View();
            }
        }

        [AllowAnonymous]
        public ActionResult Login(string key = null)
        {
            string username = key == null ? Request.Form["Username"].ToString() : "";
            string pass = key == null ? Request.Form["Password"].ToString() : "";

            var chklogin = secure.Login(username, pass, true);//set false to true for Real

            if (key != null)
            {
                username = secure.WebCenterDecode(key);
                chklogin = secure.Login(username, "a", false);
            }
            
            if (chklogin != null)
            {
                Session["PCO_Auth"] = chklogin.emp_code;

                var ulv = (from a in dbPC.tm_user_level
                           where a.pos_min <= chklogin.position_level && a.pos_max >= chklogin.position_level
                           select a).FirstOrDefault();

                if (ulv != null)
                {
                    Session["PCO_ULv"] = ulv.lv_id;
                    Session["PCO_Org"] = chklogin.LeafOrgGroupId;

                    if (chklogin.emp_lname.Length > 2)
                    {
                        Session["PCO_Name"] = chklogin.emp_fname + " " + chklogin.emp_lname.Substring(0, 2) + ". (" + ulv.lv_name + ")";
                    }
                    else
                    {
                        Session["PCO_Name"] = chklogin.emp_fname + " " + chklogin.emp_lname + ". (" + ulv.lv_name + ")";
                    }
                }

                var chk_sys_user = (from a in dbPC.tm_sys_user
                                    where a.emp_code == chklogin.emp_code
                                    select a).FirstOrDefault();

                Session["PCO_UTypeLv"] = chk_sys_user != null ? chk_sys_user.tm_sys_utype.utype_lv : 0;

                var chk_sys_plant = from a in dbPC.tm_sys_group_user
                                    where a.emp_code == chklogin.emp_code
                                    select a;

                var plant_respond = "";
                if (chk_sys_plant.Any())
                {
                    foreach (var item in chk_sys_plant)
                    {
                        plant_respond += "," + item.tm_sys_group.plant_code;
                    }
                    Session["PCO_PlantResp"] = plant_respond.Substring(1);
                }

                if (Session["Redirect"] != null)
                {
                    string url = Session["Redirect"].ToString();
                    Session.Remove("Redirect");
                    return Redirect(url);
                }
                else
                {
                    return RedirectToAction("MainPCO", "Home");
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Logout()
        {
            Session.Remove("PCO_Auth");
            Session.Remove("PCO_Name");
            Session.Remove("PCO_UTypeLv");
            Session.Remove("PCO_ULv");
            Session.Remove("PCO_Org");
            Session.Remove("PCO_PlantResp");
            return RedirectToAction("Index", "Home");
        }

        [Chk_Authen]
        public ActionResult MainPCO()
        {
            //ViewBag.Menu = 1;
            ViewBag.SelectGroup = from a in dbTNC.tnc_group_master
                                  orderby a.group_name ascending
                                  select a;
            ViewBag.SelectULv = from a in dbPC.tm_user_level
                                select a;
            ViewBag.SelectStatus = from a in dbPC.tm_status
                                   select a;
            return View();
        }

        [Chk_Authen]
        public ActionResult NewPCO()
        {
            //ViewBag.Menu = 2;
            ViewBag.SelectPlant = from a in dbPC.tm_plant
                                  where a.active == true
                                  select a;

            int org = int.Parse(Session["PCO_Org"].ToString());
            var get_gpcode = (from a in dbPCR.PCR_GCode
                              where a.g_id == org
                              select a.g_code).FirstOrDefault();

            if (get_gpcode != null)
            {
                ViewBag.GCode = get_gpcode;
            }
            else
            {
                TempData["ErrorMessage"] = "You not have Group Code. Please contact QA (6715).";
                return RedirectToAction("MainPCO", "Home");
            }

            var year = DateTime.Now.ToString("yy", new CultureInfo("en-US"));
            ViewBag.Year = year;
            ViewBag.Runno = GetRunNo(get_gpcode, year);

            return View();
        }

        [Chk_Authen]
        public ActionResult EditPCO(string gpcode, string year, int runno)
        {
            var main = (from a in dbPC.td_main_data
                        where a.gpcode == gpcode && a.year == year && a.runno == runno
                        select a).FirstOrDefault();

            ViewBag.SelectPlant = from a in dbPC.tm_plant
                                  where a.active == true
                                  select a;

            ViewBag.ImpactPlant = (from a in dbPC.td_impact_plant
                                   where a.gpcode == gpcode && a.year == year && a.runno == runno
                                   select a.plant_code).ToList();

            return View(main);
        }

        [Chk_Authen]
        public ActionResult SubmitPackSTD(string gpcode, string year, int runno, int org)
        {
            var get_tran = (from a in dbPC.V_ShowTran
                            where a.gpcode == gpcode && a.year == year && a.runno == runno && a.status_id == 7 && a.org_id == org
                            select a).FirstOrDefault();
            return View(get_tran);
        }

        [Chk_Authen]
        public ActionResult PackingSTD(string gpcode, string year, int runno, int org)
        {
            var get_pack = from a in dbPC.V_ShowPack
                           where a.gpcode == gpcode && a.year == year && a.runno == runno && a.issue_org == org
                           select a;

            if (!get_pack.Any(a => a.status == "Pending"))
            {
                ViewBag.CompBtn = 1;
            }

            ViewBag.SelCust = from a in dbPCR.PCR_Customer
                              orderby a.cust_no
                              select a;

            ViewBag.GCode = gpcode;
            ViewBag.Year = year;
            ViewBag.Runno = runno;
            ViewBag.Org = org;

            return View(get_pack);
        }

        [Chk_Authen]
        public ActionResult EditItemList(string gpcode, string year, int runno, int org)
        {
            var itemlist = from a in dbPC.td_item_list
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;
            ViewBag.GCode = gpcode;
            ViewBag.Year = year;
            ViewBag.Runno = runno;
            ViewBag.Org = org;
            ViewBag.CompItem = itemlist.Where(w => w.pp_have == null).Count();
            return View(itemlist);
        }

        [Chk_Authen]
        public ActionResult EditItemList1(string gpcode, string year, int runno, int org)
        {
            var itemlist = from a in dbPC.td_item_list
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;
            ViewBag.GCode = gpcode;
            ViewBag.Year = year;
            ViewBag.Runno = runno;
            ViewBag.Org = org;
            ViewBag.CompItem = itemlist.Where(w => w.cs_stock == null).Count();
            return View(itemlist);
        }

        [Chk_Authen]
        public ActionResult EditItemList2(string gpcode, string year, int runno, int org)
        {
            var itemlist = from a in dbPC.td_item_list
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;
            ViewBag.GCode = gpcode;
            ViewBag.Year = year;
            ViewBag.Runno = runno;
            ViewBag.Org = org;
            ViewBag.CompItem = itemlist.Where(w => w.cs_change == true && w.pln_item == null).Count();
            return View(itemlist);
        }

        [Chk_Authen]
        public ActionResult EditItemList3(string gpcode, string year, int runno, int org)
        {
            var itemlist = from a in dbPC.td_item_list
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;
            ViewBag.GCode = gpcode;
            ViewBag.Year = year;
            ViewBag.Runno = runno;
            ViewBag.Org = org;
            ViewBag.CompItem = itemlist.Where(w => w.pp_eff_prod == null).Count();
            return View(itemlist);
        }

        [Chk_Authen]
        public ActionResult ClosePCO()
        {
            //ViewBag.Menu = 3;
            ViewBag.SelectGroup = from a in dbTNC.tnc_group_master
                                  orderby a.group_name ascending
                                  select a;
            ViewBag.SelectULv = from a in dbPC.tm_user_level
                                select a;
            ViewBag.SelectStatus = from a in dbPC.tm_status
                                   where a.status_id > 99
                                   select a;

            return View();
        }

        //------------------ Tabs ---------------------//

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabMain(string gpcode, string year, int runno)
        {
            var main = (from a in dbPC.V_Tran
                        where a.gpcode == gpcode && a.year == year && a.runno == runno
                        select a).FirstOrDefault();

            var queryImpact = from a in dbPC.td_impact_plant.Where(w => w.gpcode == gpcode && w.year == year && w.runno == runno)
                              join b in dbPC.tm_plant on a.plant_code equals b.plant_code into g
                              from x in g.DefaultIfEmpty()
                              select new
                              {
                                  x.plant_code,
                                  x.plant_name
                              };

            var getImpact = "";

            foreach (var item in queryImpact)
            {
                getImpact += ", " + item.plant_name;
            }
            ViewBag.ShowImpactPlant = getImpact.Substring(2);

            var tran = from a in dbPC.V_ShowTran
                       where a.gpcode == gpcode && a.year == year && a.runno == runno && a.status_id == 1
                       orderby a.status_id, a.lv_id
                       select a;
            ViewBag.IssTran = tran;

            return PartialView(main);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabAtt(string gpcode, string year, int runno)
        {
            var get_att = from a in dbPC.td_files
                          where a.gpcode == gpcode && a.year == year && a.runno == runno && a.delete_dt == null
                          orderby a.filename
                          select a;

            var ulv = byte.Parse(Session["PCO_ULv"].ToString());
            int org = int.Parse(Session["PCO_Org"].ToString());
            var query = dbPC.td_transaction.Any(w => w.gpcode == gpcode && w.year == year && w.runno == runno && w.act_id == "ISS" && w.lv_id == ulv && w.org_id == org);
            if (query)
            {
                ViewBag.IsIssuer = true;
                ViewBag.AttGC = gpcode;
                ViewBag.AttYY = year;
                ViewBag.AttRN = runno;
                if (!dbPC.td_transaction.Any(w => w.gpcode == gpcode && w.year == year && w.runno == runno && w.status_id >= 100))
                {
                    ViewBag.AttClose = true;
                }
                else
                {
                    ViewBag.AttClose = false;
                }
            }
            else
            {
                ViewBag.IsIssuer = false;
            }

            return PartialView(get_att);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabQA(string gpcode, string year, int runno)
        {
            var get_concern = from a in dbPC.V_ShowConcern
                              where a.gpcode == gpcode && a.year == year && a.runno == runno
                              select a;

            var get_tran = from a in dbPC.V_ShowTran
                           where a.gpcode == gpcode && a.year == year && a.runno == runno && a.status_id == 2
                           orderby a.status_id, a.lv_id
                           select a;
            ViewBag.QATran = get_tran;

            return PartialView(get_concern);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabConcern(string gpcode, string year, int runno)
        {
            var get_tran = from a in dbPC.V_ShowTran
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           && (a.status_id == 3 || a.status_id == 4 || a.status_id == 5 || (a.status_id >= 8 && a.status_id <= 13))
                           orderby a.status_id, a.act_dt
                           select a;
            return PartialView(get_tran);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabSLCS(string gpcode, string year, int runno)
        {
            var get_tran = from a in dbPC.V_ShowTran
                           where a.gpcode == gpcode && a.year == year && a.runno == runno && a.status_id == 6
                           orderby a.status_id, a.lv_id
                           select a;
            if (get_tran.Any()) ViewBag.SLCSTran = get_tran;

            var get_slcs = from a in dbPC.td_salecs
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;

            return PartialView(get_slcs);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabPack(string gpcode, string year, int runno)
        {
            var get_tran = from a in dbPC.V_ShowTran
                           where a.gpcode == gpcode && a.year == year && a.runno == runno && a.status_id == 7
                           orderby a.status_id, a.lv_id
                           select a;
            if (get_tran.Any()) ViewBag.QAPackTran = get_tran;

            var get_pack = from a in dbPC.V_ShowPack
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           orderby a.series
                           select a;

            return PartialView(get_pack);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabItemList(string gpcode, string year, int runno)
        {
            var get_item = from a in dbPC.td_item_list
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;

            return PartialView(get_item);
        }

        //[HttpGet]
        //[OutputCache(Duration = 0, NoStore = true)]
        //public ActionResult _TabPP(string gpcode, string year, int runno)
        //{
        //    var get_tran = from a in dbPC.V_ShowTran
        //                   where a.gpcode == gpcode && a.year == year && a.runno == runno
        //                   && a.status_id == 8
        //                   orderby a.act_dt
        //                   select a;
        //    return PartialView(get_tran);
        //}

        //[HttpGet]
        //[OutputCache(Duration = 0, NoStore = true)]
        //public ActionResult _TabCS(string gpcode, string year, int runno)
        //{
        //    var get_tran = from a in dbPC.V_ShowTran
        //                   where a.gpcode == gpcode && a.year == year && a.runno == runno
        //                   && a.status_id == 9
        //                   orderby a.act_dt
        //                   select a;
        //    return PartialView(get_tran);
        //}

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabFB(string gpcode, string year, int runno)
        {
            //Feedback
            var get_fb = from a in dbPC.V_ShowFB
                         where a.gpcode == gpcode && a.year == year && a.runno == runno
                         orderby a.act_dt
                         select a;
            return PartialView(get_fb);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabCls(string gpcode, string year, int runno)
        {
            var get_tran = from a in dbPC.V_ShowTran
                           where a.gpcode == gpcode && a.year == year && a.runno == runno && a.status_id == 14
                           orderby a.act_dt
                           select a;
            return PartialView(get_tran);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult _TabApprove(string gpcode, string year, int runno)
        {
            var get_tran = from a in dbPC.td_transaction
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           && a.act_id == null
                           orderby a.lv_id descending
                           select a;

            var ulv = byte.Parse(Session["PCO_ULv"].ToString());
            int org = int.Parse(Session["PCO_Org"].ToString());

            if (get_tran.Any())//have wait tran
            {
                foreach (var item in get_tran)
                {
                    if (item.lv_id <= ulv)// if other, not check for display form.
                    {
                        //Check Authorize User
                        if (item.lv_id == ulv && item.org_id == org)//Exact
                        {
                            ViewBag.PCOTran = item;
                        }
                        else if (ulv <= 2 && item.org_id == org)//Mgr. for Eng.
                        {
                            ViewBag.PCOTran = item;
                        }
                        else
                        {
                            if (item.lv_id == 2)
                            {
                                var get_org = (from a in dbTNC.View_Organization
                                               where a.group_id == item.org_id
                                               select a).FirstOrDefault();
                                if (get_org != null)
                                {
                                    if (ulv == 3 && org == get_org.dept_id)//Dept. for Mgr.
                                    {
                                        ViewBag.PCOTran = item;
                                    }
                                    else if (ulv == 4 && org == get_org.plant_id)//Plant for Mgr.
                                    {
                                        ViewBag.PCOTran = item;
                                    }
                                }
                            }
                            else if (item.lv_id == 3)
                            {
                                var get_org = (from a in dbTNC.View_Organization
                                               where a.dept_id == item.org_id
                                               select a).FirstOrDefault();
                                if (get_org != null && org == get_org.plant_id)//Plant for Dept.
                                {
                                    ViewBag.PCOTran = item;
                                }
                            }
                        }

                        if (ViewBag.PCOTran != null)
                        {
                            //Check Transaction
                            if (item.status_id == 1 && item.lv_id == ulv && item.comment == "Wait" && item.org_id == org)
                            {
                                ViewBag.PCOForm = "EditPCO";
                            }
                            else if (item.status_id == 1 && item.comment != "Wait")
                            {
                                ViewBag.PCOForm = "_FormMgrIssuer";
                            }
                            else if (item.status_id == 2)
                            {
                                ViewBag.PCOForm = "_FormQAReview";
                            }
                            else if ((item.status_id == 3 || item.status_id == 4 || item.status_id == 5 ||
                                        item.status_id == 10 || item.status_id == 11))
                            {
                                ViewBag.PCOForm = "_FormApprove";
                            }
                            else if (item.status_id == 6)
                            {
                                ViewBag.PCOForm = "_FormSaleCS";
                            }
                            else if (item.status_id == 7)
                            {
                                ViewBag.PCOForm = "PackingSTD";
                            }
                            else if (item.status_id == 8 && item.lv_id == 1)
                            {
                                ViewBag.PCOForm = "EditItemList";
                            }
                            else if (item.status_id == 8 && item.lv_id >= 2)//8 = Prod. Planning
                            {
                                ViewBag.PCOForm = "_FormApprove";
                            }
                            else if (item.status_id == 9 && item.lv_id == 1)
                            {
                                ViewBag.PCOForm = "EditItemList1";
                            }
                            else if (item.status_id == 9 && item.lv_id >= 2)//9 = CS
                            {
                                ViewBag.PCOForm = "_FormCSApprove";
                            }
                            else if (item.status_id == 12 && item.lv_id == 1)//12 = Planning
                            {
                                ViewBag.PCOForm = "EditItemList2";
                            }
                            else if (item.status_id == 12 && item.lv_id >= 2)//12 = Planning
                            {
                                ViewBag.PCOForm = "_FormApprove";
                            }
                            else if (item.status_id == 13)//13 = Prod. Planning Confirm
                            {
                                ViewBag.PCOForm = "EditItemList3";
                            }
                            else if (item.status_id == 14)//14 = QA Close
                            {
                                ViewBag.PCOForm = "_FormApprove";
                            }
                            break;
                        }
                    }
                }
            }
            //else
            //{
            //    var query = dbPC.td_transaction.Where(w => w.gpcode == gpcode && w.year == year && w.runno == runno && w.act_id == "ISS" && w.lv_id == ulv && w.org_id == org).FirstOrDefault();
            //    if (query != null)
            //    {
            //        ViewBag.PCOTran = query;
            //        ViewBag.PCOForm = "_FormCancel";
            //    }
            //}

            return PartialView();
        }

        //------------------Form Post---------------------//

        [HttpPost]
        public ActionResult IssuerAddFiles(IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                var issuer = Session["PCO_Auth"].ToString();
                UploadFiles(gpcode, year, runno, atfiles, issuer);
                dbPC.SaveChanges();

                return RedirectToAction("MainPCO");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult CancelAction()
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["ccgc"];
                string year = Request.Form["ccyy"];
                int runno = int.Parse(Request.Form["ccrn"]);
                string reason = Request.Form["txaRS"];
                var ulv = byte.Parse(Session["PCO_ULv"].ToString());
                int org = int.Parse(Session["PCO_Org"].ToString());

                ManageTransaction(gpcode, year, runno, 1, ulv, org, null, "CXL", actor, reason);

                return RedirectToAction("ClosePCO");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult CreatePCO(HttpPostedFileBase impfile, IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                int org = int.Parse(Session["PCO_Org"].ToString());
                var issuer = Session["PCO_Auth"].ToString();

                var get_gpcode = Request.Form["txtGC"];
                var year = Request.Form["txtYear"];
                var runno = GetRunNo(get_gpcode, year);

                //-----------------Insert Main data---------------------//

                td_main_data main = new td_main_data();
                main.gpcode = get_gpcode;
                main.year = year;
                main.runno = runno;
                main.chage_type = Request.Form["selChangeType"];
                main.change_detail = Request.Form["txaDetail"];
                main.condition_current = Request.Form["txaCurrent"];
                main.condition_new = Request.Form["txaNew"];
                main.reason = Request.Form["txaReason"];
                main.expected_dt = ParseToDate(Request.Form["dtpExpectDate"].ToString());
                main.request_by = Request.Form["selReqBy"];
                main.production_type = Request.Form["selProdType"];
                main.issue_group = org;
                main.request_date = DateTime.Now;
                dbPC.td_main_data.Add(main);

                //------------------Insert Impact--------------------//

                var imp_plant = Request.Form["ImpPlant[]"].ToString();
                var listPlant = imp_plant.Split(',');
                foreach (var item in listPlant)
                {
                    td_impact_plant imp = new td_impact_plant();
                    imp.gpcode = main.gpcode;
                    imp.year = main.year;
                    imp.runno = main.runno;
                    imp.plant_code = item;
                    dbPC.td_impact_plant.Add(imp);
                }

                //------------------Insert Attach files--------------------//
                UploadFiles(main.gpcode, main.year, main.runno, atfiles, issuer);

                //------------------Import item list--------------------//

                var savedDir = dir.SaveFile(impfile, "Temp/" + DateTime.Now.ToString("yyyyMM", new CultureInfo("en-US")));
                var reader = util.ReadExcel(savedDir);

                foreach (var item in reader)
                {
                    td_item_list itemlist = new td_item_list();
                    itemlist.gpcode = main.gpcode;
                    itemlist.year = main.year;
                    itemlist.runno = main.runno;
                    itemlist.item_code = item[0].ToString();
                    itemlist.cust_no = item[1].ToString();
                    itemlist.customer_name = item[2].ToString();//2
                    itemlist.part_no = item[3].ToString();
                    itemlist.wc = item[4].ToString();

                    dbPC.td_item_list.Add(itemlist);
                }

                dbPC.SaveChanges();

                //------------------Insert Transaction--------------------//

                var ulv = byte.Parse(Session["PCO_ULv"].ToString());

                ManageTransaction(get_gpcode, year, runno, 1, ulv, org, null, "ISS", issuer);

                //--------------------------------------//

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult UpdatePCO(IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                int org = int.Parse(Session["PCO_Org"].ToString());
                var issuer = Session["PCO_Auth"].ToString();

                var gpcode = Request.Form["txtGC"];
                var year = Request.Form["txtYear"];
                var runno = int.Parse(Request.Form["txtRun"]);

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
                //------------------Insert Attach files--------------------//

                UploadFiles(main.gpcode, main.year, main.runno, atfiles, issuer);

                //------------------Import item list--------------------//

                var file = Request.Files["impfile"];
                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    
                    using (var db = new PackingChangeEntities())//Add Date 2016-02-17 by Monchit.
                    {
                        ((IObjectContextAdapter)db).ObjectContext.ExecuteStoreCommand("DELETE FROM td_item_list WHERE gpcode = " + gpcode + " and year = " + year + " and runno = " + runno + "");
                        db.SaveChanges();
                    }

                    var savedDir = dir.SaveFile(file, "Temp/" + DateTime.Now.ToString("yyyyMM", new CultureInfo("en-US")));
                    var reader = util.ReadExcel(savedDir);

                    foreach (var item in reader)
                    {
                        td_item_list itemlist = new td_item_list();
                        itemlist.gpcode = gpcode;
                        itemlist.year = year;
                        itemlist.runno = runno;
                        itemlist.item_code = item[0].ToString();
                        itemlist.cust_no = item[1].ToString();
                        itemlist.customer_name = item[2].ToString();//2
                        itemlist.part_no = item[3].ToString();
                        itemlist.wc = item[4].ToString();
                        dbPC.td_item_list.Add(itemlist);
                    }
                }

                dbPC.SaveChanges();

                //------------------Insert Transaction--------------------//

                var ulv = byte.Parse(Session["PCO_ULv"].ToString());

                UpdateTransaction(gpcode, year, runno, 1, ulv, org, action: "ISS", actor: issuer, comment: "Update data.");
                ManageTransaction(gpcode, year, runno, 1, ulv, org, null, "UPD", issuer);

                //--------------------------------------//

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult IssuerMgrAction(IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                //------------------Insert Transaction--------------------//
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);
                byte ulv = byte.Parse(Request.Form["hdulv"]);
                byte status = byte.Parse(Request.Form["hdstt"]);
                string act = Request.Form["slAction"];
                string comment = Request.Form["txaCM"];

                if (act == "APV")
                {
                    //------------------Insert Attach files--------------------//
                    UploadFiles(gpcode, year, runno, atfiles, actor);
                    dbPC.SaveChanges();
                }
                UpdateTransaction(gpcode, year, runno, status, ulv, org, action: act, actor: actor, comment: comment);
                ManageTransaction(gpcode, year, runno, status, ulv, org, null, act, actor, comment);

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult QAReviewAction(IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);
                byte ulv = byte.Parse(Request.Form["hdulv"]);
                byte status = byte.Parse(Request.Form["hdstt"]);
                string act = Request.Form["slAction"];
                string comment = Request.Form["txaCM"];

                if (act != "FBK")
                {
                    if (act == "APV")
                    {
                        var prod = Request.Form["slProd"] != null ? Request.Form["slProd"].ToString() : null;
                        var pur = Request.Form["slPur"] != null ? Request.Form["slPur"].ToString() : null;
                        var sale = Request.Form["slSale"].ToString();
                        var cs = Request.Form["slCS"].ToString();
                        var lg = Request.Form["slLG"].ToString();
                        var pln = Request.Form["slPLN"].ToString();

                        InsertTempConcern(gpcode, year, runno, 1, prod);
                        InsertTempConcern(gpcode, year, runno, 2, pur);
                        InsertTempConcern(gpcode, year, runno, 3, sale);
                        InsertTempConcern(gpcode, year, runno, 4, cs);
                        InsertTempConcern(gpcode, year, runno, 5, lg);
                        InsertTempConcern(gpcode, year, runno, 6, pln);

                        //------------------Insert Attach files--------------------//
                        UploadFiles(gpcode, year, runno, atfiles, actor);
                        dbPC.SaveChanges();
                    }
                    UpdateTransaction(gpcode, year, runno, status, ulv, org, action: act, actor: actor, comment: comment);
                    ManageTransaction(gpcode, year, runno, status, ulv, org, null, act, actor, comment);
                }
                else
                {
                    InsertFeedback(gpcode, year, runno, status, ulv, org, actor, comment);
                }

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult SaleCSAction(IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);
                byte ulv = byte.Parse(Request.Form["hdulv"]);
                byte status = byte.Parse(Request.Form["hdstt"]);
                string act = Request.Form["slAction"];
                string comment = Request.Form["txaCM"];

                if (act == "APV")
                {
                    string chkneed = Request.Form["chkneed[]"].ToString();
                    var listProb = chkneed.Split(',');
                    var need = false;
                    var noneed = false;
                    foreach (var item in listProb)
                    {
                        if (item == "1")
                        {
                            need = true;
                        }
                        else if (item == "0")
                        {
                            noneed = true;
                        }
                    }
                    byte init = byte.Parse(Request.Form["slTag"]);

                    td_salecs scs = new td_salecs();
                    scs.gpcode = gpcode;
                    scs.year = year;
                    scs.runno = runno;
                    scs.actor = actor;
                    scs.pack_std_need = need;
                    scs.cust_need = need ? Request.Form["txtCustNeed"] : null;
                    scs.pack_std_noneed = noneed;
                    scs.cust_noneed = noneed ? Request.Form["txtCustNo"] : null;
                    scs.init_tag_id = init;
                    if (init != 0) scs.frequency = Request.Form["txtFreq"];
                    dbPC.td_salecs.Add(scs);

                    //------------------Insert Attach files--------------------//
                    UploadFiles(gpcode, year, runno, atfiles, actor);
                    dbPC.SaveChanges();
                }
                UpdateTransaction(gpcode, year, runno, status, ulv, org, action: act, actor: actor, comment: comment);
                ManageTransaction(gpcode, year, runno, status, ulv, org, null, act, actor, comment);

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult StdApproveAction(IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);
                byte ulv = byte.Parse(Request.Form["hdulv"]);
                byte status = byte.Parse(Request.Form["hdstt"]);
                string act = Request.Form["slAction"];
                string comment = Request.Form["txaCM"];

                if (act == "APV")
                {
                    //------------------Insert Attach files--------------------//
                    UploadFiles(gpcode, year, runno, atfiles, actor);
                    dbPC.SaveChanges();
                }
                UpdateTransaction(gpcode, year, runno, status, ulv, org, action: act, actor: actor, comment: comment);
                ManageTransaction(gpcode, year, runno, status, ulv, org, null, act, actor, comment);

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult CSApproveAction(IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);
                byte ulv = byte.Parse(Request.Form["hdulv"]);
                byte status = byte.Parse(Request.Form["hdstt"]);
                string act = Request.Form["slAction"];
                string comment = Request.Form["txaCM"];

                if (act == "APV")
                {
                    //------------------Insert Attach files--------------------//
                    UploadFiles(gpcode, year, runno, atfiles, actor);
                    dbPC.SaveChanges();
                    UpdateTransaction(gpcode, year, runno, status, ulv, org, action: act, actor: actor, comment: comment);
                    ManageTransaction(gpcode, year, runno, status, ulv, org, null, act, actor, comment);
                }
                else if (act == "REV")
                {
                    DeleteTransaction(gpcode, year, runno, status, ulv, org);
                    UpdateTransaction(gpcode, year, runno, status, 1, org);
                }
                else
                {
                    UpdateTransaction(gpcode, year, runno, status, ulv, org, action: act, actor: actor, comment: comment);
                    ManageTransaction(gpcode, year, runno, status, ulv, org, null, act, actor, comment);
                }

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult FeedbackAction()
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["fbgc"];
                string year = Request.Form["fbyy"];
                int runno = int.Parse(Request.Form["fbrn"]);
                int org = int.Parse(Request.Form["fborg"]);
                byte ulv = byte.Parse(Request.Form["fbulv"]);
                byte status = byte.Parse(Request.Form["fbstt"]);
                string comment = Request.Form["txaFB"];
                InsertFeedback(gpcode, year, runno, status, ulv, org, actor, comment);
                SendEmailCenter(gpcode, year, runno, GetEmail(gpcode, year, runno, 1, 0), 2, comment);
                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult MgrAssignAction(IEnumerable<HttpPostedFileBase> atfiles)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);
                byte ulv = byte.Parse(Request.Form["hdulv"]);
                byte status = byte.Parse(Request.Form["hdstt"]);
                string act = Request.Form["slAction"];
                string comment = Request.Form["txaCM"];
                string pic = Request.Form["slPic"];

                if (act == "APV")
                {
                    //------------------Insert Attach files--------------------//
                    UploadFiles(gpcode, year, runno, atfiles, actor);
                    dbPC.SaveChanges();
                }
                UpdateTransaction(gpcode, year, runno, status, ulv, org, action: act, actor: actor, comment: comment);
                ManageTransaction(gpcode, year, runno, status, ulv, org, null, act, pic, comment);

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult CompPackSTD(string gpcode, string year, int runno, int org)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                UpdateTransaction(gpcode, year, runno, 7, 1, org, action: "APV", actor: actor);

                var get_pack = from a in dbPC.td_pack_std
                               where a.gpcode == gpcode && a.year == year && a.runno == runno
                               select a;

                var pack_rej = get_pack.Where(w => w.status == "Rejected").Count();
                var pack_can = get_pack.Where(w => w.status == "Canceled").Count();

                if (pack_rej > 0 && (pack_rej + pack_can) == get_pack.Count())
                {
                    ManageTransaction(gpcode, year, runno, 7, 1, org, null, "REJ", actor, "All Packing STD. was Rejected.");
                }
                else
                {
                    ManageTransaction(gpcode, year, runno, 7, 1, org, null, "APV", actor);
                }

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult CompItem(string gpcode, string year, int runno, int org)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                UpdateTransaction(gpcode, year, runno, 8, 1, org, action: "APV", actor: actor);
                ManageTransaction(gpcode, year, runno, 8, 1, org, null, "APV", actor);

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult ToCSMgr(string gpcode, string year, int runno, int org)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                if (HaveOtherWaitCount(gpcode, year, runno, 9, 1) > 1)
                {
                    UpdateTransaction(gpcode, year, runno, 9, 1, org, action: "APV", actor: actor);
                    ManageTransaction(gpcode, year, runno, 9, 1, org, null, "APV", actor);
                    return RedirectToAction("MainPCO", "Home");
                }
                else
                {
                    return RedirectToAction("EditItemList1", "Home", new { gpcode = gpcode, year = year, runno = runno, org = org });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult CompItem1(string gpcode, string year, int runno, int org)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                UpdateTransaction(gpcode, year, runno, 9, 1, org, action: "APV", actor: actor);
                ManageTransaction(gpcode, year, runno, 9, 1, org, null, "APV", actor);

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult CompItem2(string gpcode, string year, int runno, int org)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                UpdateTransaction(gpcode, year, runno, 12, 1, org, action: "APV", actor: actor);
                ManageTransaction(gpcode, year, runno, 12, 1, org, null, "APV", actor);

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult CompItem3(string gpcode, string year, int runno, int org)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                UpdateTransaction(gpcode, year, runno, 13, 1, org, action: "APV", actor: actor);
                ManageTransaction(gpcode, year, runno, 13, 1, org, null, "APV", actor);

                return RedirectToAction("MainPCO", "Home");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult UpdateItemListPP()
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);

                string[] key = Request.Form.GetValues("hdItem");
                string[] pp_have = Request.Form.GetValues("pp_have");
                string[] pp_lot = Request.Form.GetValues("pp_lot");
                string[] pp_qty = Request.Form.GetValues("pp_qty");

                for (int i = 0; i < key.Length; i++)
                {
                    var upditem = dbPC.td_item_list.Find(gpcode, year, runno, key[i]);
                    if (!string.IsNullOrEmpty(pp_have[i]))
                    {
                        upditem.pp_have = pp_have[i];
                        upditem.pp_lot = !string.IsNullOrEmpty(pp_lot[i]) ? pp_lot[i] : null;
                        if (string.IsNullOrEmpty(upditem.pp_check)) upditem.pp_check = actor;
                        if (!string.IsNullOrEmpty(pp_qty[i])) upditem.pp_qty = int.Parse(pp_qty[i]);
                    }
                    else
                    {
                        upditem.pp_have = null;
                        upditem.pp_lot = null;
                        upditem.pp_check = null;
                        upditem.pp_qty = null;
                    }
                }
                dbPC.SaveChanges();

                if (HaveOtherWaitCount(gpcode, year, runno, 8, 1) > 1)
                {
                    UpdateTransaction(gpcode, year, runno, 8, 1, org, action: "APV", actor: actor);
                    ManageTransaction(gpcode, year, runno, 8, 1, org, null, "APV", actor);
                    return RedirectToAction("MainPCO", "Home");
                }
                else
                {
                    return RedirectToAction("EditItemList", "Home", new { gpcode = gpcode, year = year, runno = runno, org = org });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult UpdateItemListCS()
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);

                string[] key = Request.Form.GetValues("hdItem");
                string[] cs_stock = Request.Form.GetValues("cs_stock");
                string[] cs_repack = Request.Form.GetValues("cs_repack");
                string[] cs_eff_dt = Request.Form.GetValues("cs_eff_dt");
                string[] cs_lot = Request.Form.GetValues("cs_lot");
                string[] cs_change = Request.Form.GetValues("cs_change");

                for (int i = 0; i < key.Length; i++)
                {
                    if (!string.IsNullOrEmpty(cs_repack[i]))
                    {
                        var upditem = dbPC.td_item_list.Find(gpcode, year, runno, key[i]);
                        upditem.cs_stock = int.Parse(cs_stock[i]);
                        upditem.cs_repack = cs_repack[i];
                        upditem.cs_eff_dt = ParseToDate(cs_eff_dt[i]);
                        upditem.cs_lot = cs_lot[i];
                        upditem.cs_change = bool.Parse(cs_change[i]);
                        if (string.IsNullOrEmpty(upditem.cs_check)) upditem.cs_check = actor;
                    }
                }
                dbPC.SaveChanges();

                return RedirectToAction("EditItemList1", "Home", new { gpcode = gpcode, year = year, runno = runno, org = org });

                //if (HaveOtherWaitCount(gpcode, year, runno, 9, 1) > 1)
                //{
                //    UpdateTransaction(gpcode, year, runno, 9, 1, org, action: "APV", actor: actor);
                //    ManageTransaction(gpcode, year, runno, 9, 1, org, null, "APV", actor);
                //    return RedirectToAction("MainPCO", "Home");
                //}
                //else
                //{
                //    return RedirectToAction("EditItemList1", "Home", new { gpcode = gpcode, year = year, runno = runno, org = org });
                //}
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult UpdateItemListPLN()
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);

                string[] key = Request.Form.GetValues("hdItem");
                string[] pln_item = Request.Form.GetValues("pln_item");

                for (int i = 0; i < key.Length; i++)
                {
                    if (!string.IsNullOrEmpty(pln_item[i]))
                    {
                        var upditem = dbPC.td_item_list.Find(gpcode, year, runno, key[i]);
                        upditem.pln_item = pln_item[i];
                    }
                }
                dbPC.SaveChanges();

                if (HaveOtherWaitCount(gpcode, year, runno, 12, 1) > 1)
                {
                    UpdateTransaction(gpcode, year, runno, 12, 1, org, action: "APV", actor: actor);
                    ManageTransaction(gpcode, year, runno, 12, 1, org, null, "APV", actor);
                    return RedirectToAction("MainPCO", "Home");
                }
                else
                {
                    return RedirectToAction("EditItemList2", "Home", new { gpcode = gpcode, year = year, runno = runno, org = org });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult UpdateItemListPPC()
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);

                string[] key = Request.Form.GetValues("hdItem");
                string[] pp_confirm_lot = Request.Form.GetValues("pp_confirm_lot");
                string[] pp_eff_prod = Request.Form.GetValues("pp_eff_prod");
                string[] pp_eff_pln = Request.Form.GetValues("pp_eff_pln");

                for (int i = 0; i < key.Length; i++)
                {
                    if (!string.IsNullOrEmpty(pp_confirm_lot[i]))
                    {
                        var upditem = dbPC.td_item_list.Find(gpcode, year, runno, key[i]);
                        upditem.pp_confirm_lot = pp_confirm_lot[i];
                        upditem.pp_eff_prod = ParseToDate(pp_eff_prod[i]);
                        upditem.pp_eff_pln = ParseToDate(pp_eff_pln[i]);
                    }
                }
                dbPC.SaveChanges();

                if (HaveOtherWaitCount(gpcode, year, runno, 13) > 1)
                {
                    UpdateTransaction(gpcode, year, runno, 13, 1, org, action: "APV", actor: actor);
                    return RedirectToAction("MainPCO", "Home");
                }
                else
                {
                    return RedirectToAction("EditItemList3", "Home", new { gpcode = gpcode, year = year, runno = runno, org = org });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //------------------jTable---------------------//

        [HttpPost]
        public JsonResult MainList(string id, string detail, int group = 0, byte status = 0, byte lv = 0, int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = from a in dbPC.V_Tran3
                            select a;

                if (!string.IsNullOrEmpty(id))
                {
                    query = query.Where(w => w.id.Contains(id));
                }
                //else if (mode != 0)//First Load
                //{
                //    var org = int.Parse(Session["PCO_Org"].ToString());
                //    var ulv = byte.Parse(Session["PCO_ULv"].ToString());
                //    query = query.Where(w => w.lv_id == ulv && w.org_id == org);
                //}

                if (!string.IsNullOrEmpty(detail))
                {
                    query = query.Where(w => w.change_detail.Contains(detail));
                }
                if (group != 0)
                {
                    query = query.Where(w => w.issue_group == group);
                }
                if (status != 0)
                {
                    query = query.Where(w => w.status_id == status);
                }
                if (lv != 0)
                {
                    query = query.Where(w => w.lv_id == lv);
                }

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.gpcode,
                        s.year,
                        s.runno,
                        s.id,
                        s.change_detail,
                        s.issue_group,
                        s.status_id,
                        s.lv_id
                    }).OrderBy(jtSorting).Skip(jtStartIndex).Take(jtPageSize);

                //Return result to jTable
                return Json(new { Result = "OK", Records = output, TotalRecordCount = TotalRecord });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult PCOCloseList(string id, string detail, string item, int group = 0, byte status = 0, byte lv = 0, int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = from a in dbPC.V_Tran
                            where a.maxstatus >= 100
                            select a;

                if (!string.IsNullOrEmpty(id))
                {
                    query = query.Where(w => w.id.Contains(id));
                }
                if (!string.IsNullOrEmpty(detail))
                {
                    query = query.Where(w => w.change_detail.Contains(detail));
                }
                if (!string.IsNullOrEmpty(item))
                {
                }
                if (group != 0)
                {
                    query = query.Where(w => w.issue_group == group);
                }
                if (status != 0)
                {
                    query = query.Where(w => w.maxstatus == status);
                }
                if (lv != 0)
                {
                    query = query.Where(w => w.lv_id == lv);
                }

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.gpcode,
                        s.year,
                        s.runno,
                        s.id,
                        s.change_detail,
                        s.issue_group,
                        s.maxstatus,
                        s.lv_id
                    }).OrderBy(jtSorting).Skip(jtStartIndex).Take(jtPageSize);

                //Return result to jTable
                return Json(new { Result = "OK", Records = output, TotalRecordCount = TotalRecord });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------Transaction---------------------//

        /// <summary>
        /// Input Data => Current Transaction
        /// </summary>
        /// <param name="gpcode"></param>
        /// <param name="year"></param>
        /// <param name="runno"></param>
        /// <param name="status">Current Status</param>
        /// <param name="lv">Current User Level</param>
        /// <param name="org"></param>
        /// <param name="plant"></param>
        /// <param name="action"></param>
        /// <param name="actor"></param>
        public void ManageTransaction(string gpcode, string year, int runno, byte status, byte lv, int org, string plant, string action, string actor, string content = "")
        {
            TNCOrganization get_tnc_org = new TNCOrganization();

            plant = plant ?? "-";

            if (action == "APV")//Approve
            {
                var chk_current_status = (from a in dbPC.tm_status
                                          where a.status_id == status
                                          select a).FirstOrDefault();

                if (lv < chk_current_status.lv_max)//Current Status
                {
                    get_tnc_org.GetApprover(org, 1);
                    InsertTransaction(gpcode, year, runno, status, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                    SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state:chk_current_status.status_name);
                }
                else//Next Status
                {
                    if (status == 1)//Issuer
                    {
                        var get_impact = (from a in dbPC.td_impact_plant
                                          where a.gpcode == gpcode && a.year == year && a.runno == runno
                                          select a.plant_code).ToList();

                        var get_respond = (from a in dbPC.tm_sys_group
                                           where a.group_type == "QA" && get_impact.Contains(a.plant_code)
                                           select a.group_respond).Distinct();

                        var get_status_name = dbPC.tm_status;

                        foreach (var item in get_respond)//goto QA
                        {
                            get_tnc_org.GetApprover(item, 1);
                            InsertTransaction(gpcode, year, runno, 2, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);//2 is Lv. Mgr.
                            SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state:dbPC.tm_status.Find(2).status_name);
                        }
                    }
                    else if (status == 2)//QA Review
                    {
                        //Check that have wait for approve in this status?
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var get_main = dbPC.td_main_data.Find(gpcode, year, runno);
                            if (get_main != null)
                            {
                                if (get_main.production_type.Contains("Manufacturing"))//goto QC
                                {
                                    var get_impact = (from a in dbPC.td_impact_plant
                                                      where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                      select a.plant_code).ToList();
                                    //var issgroup = dbPC.td_main_data.Find(gpcode, year, runno).issue_group;
                                    var get_respond = (from a in dbPC.tm_sys_group
                                                       where a.group_type == "QC" && get_impact.Contains(a.plant_code)
                                                       select a.group_respond).Distinct();
                                    foreach (var item in get_respond)
                                    {
                                        get_tnc_org.GetApprover(item, 1);
                                        InsertTransaction(gpcode, year, runno, 3, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                        SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(3).status_name);//Send Email to
                                    }
                                }
                                else//goto Purchase
                                {
                                    var pur_list = GetTempConcern(gpcode, year, runno, 2);//2 = Purchase
                                    if (pur_list.Count != 0)//have Purchase -> Purchase
                                    {
                                        foreach (var item in pur_list)
                                        {
                                            get_tnc_org.GetApprover(item, 1);
                                            InsertTransaction(gpcode, year, runno, 5, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                            SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(5).status_name);
                                        }
                                    }
                                    else//no Purchase -> Sales/CS
                                    {
                                        var sale_list = GetTempConcern(gpcode, year, runno, 3);//3 = Sales/CS
                                        foreach (var item in sale_list)
                                        {
                                            get_tnc_org.GetApprover(item, 1);

                                            if (get_tnc_org.OrgId != 0)
                                            {
                                                InsertTransaction(gpcode, year, runno, 6, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                                SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(6).status_name);
                                            }
                                            else
                                            {
                                                InsertTransaction(gpcode, year, runno, 6, 2, item);
                                                SendEmailCenter(gpcode, year, runno, GetEmail(item,2), state: dbPC.tm_status.Find(6).status_name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (status == 3)//QC
                    {
                        //Check that have wait for approve in this status?
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var prod_list = GetTempConcern(gpcode, year, runno, 1);//1 = Production
                            foreach (var item in prod_list)//goto Production
                            {
                                get_tnc_org.GetApprover(item, 1);
                                InsertTransaction(gpcode, year, runno, 4, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(4).status_name);
                            }
                        }
                    }
                    else if (status == 4)//Production
                    {
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var pur_list = GetTempConcern(gpcode, year, runno, 2);//2 = Purchase
                            if (pur_list.Count != 0)//have Purchase -> Purchase
                            {
                                foreach (var item in pur_list)
                                {
                                    get_tnc_org.GetApprover(item, 1);
                                    InsertTransaction(gpcode, year, runno, 5, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                    SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(5).status_name);
                                }
                            }
                            else//no Purchase -> Sales/CS Mgr.
                            {
                                var sale_list = GetTempConcern(gpcode, year, runno, 3);//3 = Sales/CS
                                foreach (var item in sale_list)
                                {
                                    get_tnc_org.GetApprover(item, 1);

                                    if (get_tnc_org.OrgId != 0)
                                    {
                                        InsertTransaction(gpcode, year, runno, 6, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                        SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(6).status_name);
                                    }
                                    else
                                    {
                                        InsertTransaction(gpcode, year, runno, 6, 2, item);
                                        SendEmailCenter(gpcode, year, runno, GetEmail(item, 2), state: dbPC.tm_status.Find(6).status_name);
                                    }
                                }
                            }
                        }
                    }
                    else if (status == 5)//Purchase
                    {
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var sale_list = GetTempConcern(gpcode, year, runno, 3);//3 = Sales/CS
                            foreach (var item in sale_list)//goto Sales/CS
                            {
                                get_tnc_org.GetApprover(item, 1);

                                if (get_tnc_org.OrgId != 0)
                                {
                                    InsertTransaction(gpcode, year, runno, 6, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                    SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(6).status_name);
                                }
                                else
                                {
                                    InsertTransaction(gpcode, year, runno, 6, 2, item);
                                    SendEmailCenter(gpcode, year, runno, GetEmail(item, 2), state: dbPC.tm_status.Find(6).status_name);
                                }
                            }
                        }
                    }
                    else if (status == 6)//Sales/CS
                    {
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var get_impact = (from a in dbPC.td_impact_plant
                                              where a.gpcode == gpcode && a.year == year && a.runno == runno
                                              select a.plant_code).ToList();

                            if (HaveNeedPack(gpcode, year, runno))//Have need Packing STD.
                            {
                                var get_respond = (from a in dbPC.tm_sys_group
                                                   where a.group_type == "QA" && get_impact.Contains(a.plant_code)
                                                   select a.group_respond).Distinct();
                                foreach (var item in get_respond)//goto QA Packing Officer
                                {
                                    InsertTransaction(gpcode, year, runno, 7, 1, item);//1 is Lv. Eng.
                                    SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(7).status_name);
                                }
                            }
                            else// No need Packing STD.
                            {
                                var get_main = dbPC.td_main_data.Find(gpcode, year, runno);
                                if (get_main != null)
                                {
                                    if (get_main.chage_type == "Inspection Data" 
                                        || (get_main.production_type == "Pass Through" || get_main.production_type == "Service Part"))
                                    {
                                        var cs_list = GetTempConcern(gpcode, year, runno, 4);//4 = CS
                                        foreach (var item in cs_list)//goto CS
                                        {
                                            InsertTransaction(gpcode, year, runno, 9, 1, item);
                                            SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(9).status_name);
                                        }
                                    }
                                    else
                                    {
                                        var get_respond = (from a in dbPC.tm_sys_group
                                                           where a.group_type == "PP" && get_impact.Contains(a.plant_code)
                                                           select a.group_respond).Distinct();
                                        if (get_respond != null)
                                        {
                                            foreach (var item in get_respond)//goto PP Mgr.
                                            {
                                                InsertTransaction(gpcode, year, runno, 8, 1, item);
                                                SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(8).status_name);
                                            }
                                        }
                                        else//Add 2016-03-03 by Monchit W.
                                        {
                                            var cs_list = GetTempConcern(gpcode, year, runno, 4);//4 = CS
                                            foreach (var item in cs_list)//goto CS
                                            {
                                                InsertTransaction(gpcode, year, runno, 9, 1, item);
                                                SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(9).status_name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (status == 7)//QA Packing STD.
                    {
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var get_impact = (from a in dbPC.td_impact_plant
                                              where a.gpcode == gpcode && a.year == year && a.runno == runno
                                              select a.plant_code).ToList();

                            var get_main = dbPC.td_main_data.Find(gpcode, year, runno);
                            if (get_main != null)
                            {
                                if (get_main.chage_type == "Inspection Data"
                                    || get_main.production_type == "Pass Through"
                                    || get_main.production_type == "Service Part")
                                {
                                    var cs_list = GetTempConcern(gpcode, year, runno, 4);//4 = CS
                                    foreach (var item in cs_list)//goto CS
                                    {
                                        InsertTransaction(gpcode, year, runno, 9, 1, item);
                                        SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(9).status_name);
                                    }
                                }
                                else
                                {
                                    var get_respond = (from a in dbPC.tm_sys_group
                                                       where a.group_type == "PP" && get_impact.Contains(a.plant_code)
                                                       select a.group_respond).Distinct();
                                    if (get_respond != null)
                                    {
                                        foreach (var item in get_respond)//goto PP Mgr.
                                        {
                                            InsertTransaction(gpcode, year, runno, 8, 1, item);
                                            SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(8).status_name);
                                        }
                                    }
                                    else//Add 2016-03-03 by Monchit W.
                                    {
                                        var cs_list = GetTempConcern(gpcode, year, runno, 4);//4 = CS
                                        foreach (var item in cs_list)//goto CS
                                        {
                                            InsertTransaction(gpcode, year, runno, 9, 1, item);
                                            SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(9).status_name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (status == 8)//Prod. Planning
                    {
                        //if (lv == 1)
                        //{
                        //    get_tnc_org.GetApprover(org, 1);
                        //    InsertTransaction(gpcode, year, runno, status, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                        //    SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail);//Send Email to
                        //}
                        //else if (lv == 2)
                        //{
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var cs_list = GetTempConcern(gpcode, year, runno, 4);//4 = CS
                            foreach (var item in cs_list)//goto CS
                            {
                                InsertTransaction(gpcode, year, runno, 9, 1, item);
                                SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(9).status_name);
                            }
                        }
                        //}
                    }
                    else if (status == 9)//CS
                    {
                        //if (lv == 1)
                        //{
                        //    get_tnc_org.GetApprover(org, 1);
                        //    InsertTransaction(gpcode, year, runno, status, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                        //    SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail);//Send Email to
                        //}
                        //else if (lv == 2)
                        //{
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var lg_list = GetTempConcern(gpcode, year, runno, 5);//5 = LG
                            foreach (var item in lg_list)//goto LG
                            {
                                get_tnc_org.GetApprover(item, 1);
                                InsertTransaction(gpcode, year, runno, 10, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(10).status_name);//Send Email to
                            }
                        }
                        //}
                    }
                    else if (status == 10)//LG
                    {
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var get_main = dbPC.td_main_data.Find(gpcode, year, runno);
                            var get_item_change = dbPC.td_item_list.Where(w => w.gpcode == gpcode && w.year == year
                                                                && w.runno == runno && w.cs_change == true).Count();
                            if (get_main.chage_type.Contains("Inspection") || get_main.production_type == "Pass Through")
                            {
                                var get_respond = (from a in dbPC.tm_sys_group
                                                   where a.group_type == "QC" && a.plant_code == "PPP"
                                                   select a.group_respond).Distinct();
                                foreach (var item in get_respond)//goto QC Receiving
                                {
                                    get_tnc_org.GetApprover(item, 1);
                                    InsertTransaction(gpcode, year, runno, 11, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                    SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(11).status_name);
                                }
                            }
                            else if (get_item_change > 0)
                            {
                                var pln_list = GetTempConcern(gpcode, year, runno, 6);//5 = Planning
                                foreach (var item in pln_list)//goto Planning
                                {
                                    InsertTransaction(gpcode, year, runno, 12, 1, item);
                                    SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(12).status_name);
                                }
                            }
                            else
                            {
                                //goto Prod. Planning
                                var get_respond = from a in dbPC.td_transaction
                                                  where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                    && a.status_id == 8 && a.lv_id == 1
                                                  select a;
                                if (get_respond.Any())
                                {
                                    foreach (var item in get_respond)
                                    {
                                        InsertTransaction(gpcode, year, runno, 13, 1, item.org_id, item.plant_code, actor: item.actor);
                                        SendEmailCenter(gpcode, year, runno, GetEmail(item.actor), state: dbPC.tm_status.Find(13).status_name);
                                    }
                                }
                                else
                                {
                                    var get_impact = (from a in dbPC.td_impact_plant
                                                      where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                      select a.plant_code).ToList();

                                    var get_qa = (from a in dbPC.tm_sys_group
                                                  where a.group_type == "QA" && get_impact.Contains(a.plant_code)
                                                  select a.group_respond).Distinct();
                                    foreach (var item in get_qa)//goto QA Packing Officer
                                    {
                                        InsertTransaction(gpcode, year, runno, 14, 1, item);//1 is Lv. Eng.
                                        SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(14).status_name);
                                    }
                                }
                            }
                        }
                    }
                    else if (status == 11)//QC Receiving
                    {
                        var get_item = dbPC.td_item_list.Where(w => w.gpcode == gpcode && w.year == year
                                                                && w.runno == runno && w.cs_change == true).Count();
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            if (get_item > 0)
                            {
                                var pln_list = GetTempConcern(gpcode, year, runno, 6);//5 = Planning
                                foreach (var item in pln_list)//goto Planning
                                {
                                    get_tnc_org.GetApprover(item, 1);
                                    InsertTransaction(gpcode, year, runno, 12, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);
                                    SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(12).status_name);
                                }
                            }
                            else
                            {
                                //goto Prod. Planning
                                var get_respond = from a in dbPC.td_transaction
                                                  where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                    && a.status_id == 8 && a.lv_id == 1
                                                  select a;
                                if (get_respond.Any())
                                {
                                    foreach (var item in get_respond)
                                    {
                                        InsertTransaction(gpcode, year, runno, 13, 1, item.org_id, item.plant_code, actor: item.actor);
                                        SendEmailCenter(gpcode, year, runno, GetEmail(item.actor), state: dbPC.tm_status.Find(13).status_name);
                                    }
                                }
                                else
                                {
                                    var get_impact = (from a in dbPC.td_impact_plant
                                                      where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                      select a.plant_code).ToList();

                                    var get_qa = (from a in dbPC.tm_sys_group
                                                  where a.group_type == "QA" && get_impact.Contains(a.plant_code)
                                                  select a.group_respond).Distinct();
                                    foreach (var item in get_qa)//goto QA Packing Officer
                                    {
                                        InsertTransaction(gpcode, year, runno, 14, 1, item);//1 is Lv. Eng.
                                        SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(14).status_name);
                                    }
                                }
                            }
                        }
                    }
                    else if (status == 12)//Planning
                    {
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            //goto Prod. Planning
                            var get_respond = from a in dbPC.td_transaction
                                              where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                && a.status_id == 8 && a.lv_id == 1
                                              select a;
                            if (get_respond.Any())
                            {
                                foreach (var item in get_respond)
                                {
                                    InsertTransaction(gpcode, year, runno, 13, 1, item.org_id, item.plant_code, actor: item.actor);
                                    SendEmailCenter(gpcode, year, runno, GetEmail(item.actor), state: dbPC.tm_status.Find(13).status_name);
                                }
                            }
                            else
                            {
                                var get_impact = (from a in dbPC.td_impact_plant
                                                  where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                  select a.plant_code).ToList();

                                var get_qa = (from a in dbPC.tm_sys_group
                                              where a.group_type == "QA" && get_impact.Contains(a.plant_code)
                                              select a.group_respond).Distinct();
                                foreach (var item in get_qa)//goto QA Packing Officer
                                {
                                    InsertTransaction(gpcode, year, runno, 14, 1, item);//1 is Lv. Eng.
                                    SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(14).status_name);
                                }
                            }
                        }
                    }
                    else if (status == 13)//Prod. Planning
                    {
                        if (!HaveOtherWaitAction(gpcode, year, runno, status))
                        {
                            var get_respond = from a in dbPC.td_transaction
                                              where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                && a.status_id == 7 && a.lv_id == 1
                                              select a;//7 = QA
                            if (get_respond.Any())
                            {
                                foreach (var item in get_respond)
                                {
                                    InsertTransaction(gpcode, year, runno, 14, 1, item.org_id, item.plant_code, actor: item.actor);
                                    SendEmailCenter(gpcode, year, runno, GetEmail(item.actor), state: dbPC.tm_status.Find(14).status_name);
                                }
                            }
                            else
                            {
                                var get_impact = (from a in dbPC.td_impact_plant
                                                  where a.gpcode == gpcode && a.year == year && a.runno == runno
                                                  select a.plant_code).ToList();

                                var get_qa = (from a in dbPC.tm_sys_group
                                              where a.group_type == "QA" && get_impact.Contains(a.plant_code)
                                              select a.group_respond).Distinct();
                                foreach (var item in get_qa)//goto QA Packing Officer
                                {
                                    InsertTransaction(gpcode, year, runno, 14, 1, item);//1 is Lv. Eng.
                                    SendEmailCenter(gpcode, year, runno, GetEmail(item, 1), state: dbPC.tm_status.Find(14).status_name);
                                }
                            }
                        }
                    }
                    else if (status == 14)//QA Close
                    {
                        SkipTransaction(gpcode, year, runno);
                        InsertTransaction(gpcode, year, runno, 100, 0, 0);
                        SendEmailCenter(gpcode, year, runno, GetEmail(gpcode, year, runno, 0, 0), 3);//Send Email to all
                    }
                }
            }
            else if (action == "REJ")//Reject
            {
                SkipTransaction(gpcode, year, runno);
                InsertTransaction(gpcode, year, runno, 101, lv, org);
                SendEmailCenter(gpcode, year, runno, GetEmail(gpcode, year, runno, 0, 0), 1, content);//Send Email to all
            }
            else if (action == "REV")//Revise
            {
                BackwardTransaction(gpcode, year, runno, status, "ISS", "Wait");
                SendEmailCenter(gpcode, year, runno, GetEmail(gpcode, year, runno), 5, content);//Send Email to Issuer
            }
            else if (action == "ISS")//Issue
            {
                InsertTransaction(gpcode, year, runno, status, lv, org, plant, action, actor);//Issue Tran
                if (lv == 1)
                {
                    get_tnc_org.GetApprover(actor);
                    InsertTransaction(gpcode, year, runno, status, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);//Next Tran
                    SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, 0, state: dbPC.tm_status.Find(1).status_name);//Send Email to Mgr.
                }
                else
                {
                    var get_impact = (from a in dbPC.td_impact_plant
                                      where a.gpcode == gpcode && a.year == year && a.runno == runno
                                      select a.plant_code).ToList();

                    var get_respond = (from a in dbPC.tm_sys_group
                                       where a.group_type == "QA" && get_impact.Contains(a.plant_code)
                                       select a.group_respond).Distinct();

                    var get_status_name = dbPC.tm_status;

                    foreach (var item in get_respond)//goto QA
                    {
                        get_tnc_org.GetApprover(item, 1);
                        InsertTransaction(gpcode, year, runno, 2, (byte)(get_tnc_org.OrgLevel + 1), get_tnc_org.OrgId, actor: get_tnc_org.ManagerId);//2 is Lv. Mgr.
                        SendEmailCenter(gpcode, year, runno, get_tnc_org.ManagerEMail, state: dbPC.tm_status.Find(2).status_name);//Send Email to
                    }
                }
            }
            else if (action == "UPD")//Updated
            {
                BackwardTransaction(gpcode, year, runno, status, "REV");
                SendEmailCenter(gpcode, year, runno, GetEmail(gpcode, year, runno), 0, state: dbPC.tm_status.Find(1).status_name);//Send Email to Mgr.
            }
            else if (action == "CXL")
            {
                SkipTransaction(gpcode, year, runno);
                InsertTransaction(gpcode, year, runno, 102, lv, org, actor:actor,content:content);
                SendEmailCenter(gpcode, year, runno, GetEmail(gpcode, year, runno, 0, 0), 4, content);//Send Email to all
            }
        }

        private List<int> GetTempConcern(string gpcode, string year, int runno, byte grouptype)
        {
            if (grouptype != 1)
            {
                var concern = (from a in dbPC.td_temp_concern
                               where a.gpcode == gpcode && a.year == year && a.runno == runno && a.concern_group_id == grouptype
                               select a.group_id).ToList();
                return concern;
            }
            else
            {
                var issue_group = dbPC.td_main_data.Find(gpcode, year, runno).issue_group;
                var concern = (from a in dbPC.td_temp_concern
                               where a.gpcode == gpcode && a.year == year && a.runno == runno && a.concern_group_id == grouptype && a.group_id != issue_group
                               select a.group_id).ToList();
                return concern;
            }
        }

        private bool HaveOtherWaitAction(string gpcode, string year, int runno, byte status, byte? lv = null)
        {
            var check = dbPC.td_transaction.Where(a => a.gpcode == gpcode && a.year == year && a.runno == runno && a.status_id == status && a.act_id == null);
            if (lv != null)
            {
                check = check.Where(w => w.lv_id == lv);
            }
            return check.Count() > 0 ? true : false;
        }

        private int HaveOtherWaitCount(string gpcode, string year, int runno, byte status, byte? lv = null)
        {
            var check = dbPC.td_transaction.Where(a => a.gpcode == gpcode && a.year == year && a.runno == runno && a.status_id == status && a.act_id == null);
            if (lv != null)
            {
                check = check.Where(w => w.lv_id == lv);
            }
            return check.Count();
        }

        private bool HaveNeedPack(string gpcode, string year, int runno)
        {
            var check = dbPC.td_salecs.Where(w => w.gpcode == gpcode && w.year == year && w.runno == runno && w.pack_std_need == true).Count();

            return check > 0 ? true : false;
        }

        public void InsertTransaction(string gpcode, string year, int runno, byte status, byte lv, int org, string plant = "-", string action = null, string actor = null, string content = null)
        {
            using (var dbLocal = new PackingChangeEntities())
            {
                var query = (from a in dbLocal.td_transaction
                             where a.gpcode == gpcode && a.year == year && a.runno == runno
                             && a.status_id == status && a.lv_id == lv && a.org_id == org && a.plant_code == plant
                             select a).FirstOrDefault();

                if (query == null)
                {
                    td_transaction tran = new td_transaction();
                    tran.gpcode = gpcode;
                    tran.year = year;
                    tran.runno = runno;
                    tran.status_id = status;
                    tran.lv_id = lv;
                    tran.org_id = org;
                    tran.plant_code = plant;
                    tran.act_id = action;
                    tran.actor = actor;
                    tran.act_dt = DateTime.Now;
                    tran.comment = content;
                    dbLocal.td_transaction.Add(tran);
                    dbLocal.SaveChanges();
                }
            }
        }

        public void UpdateTransaction(string gpcode, string year, int runno, byte status, byte lv, int org, string plant = "-", string action = null, string actor = null, string comment = null)
        {
            using (var dbLocal = new PackingChangeEntities())
            {
                var upd = dbLocal.td_transaction.Find(gpcode, year, runno, status, lv, org, plant);
                upd.actor = actor;
                upd.act_id = action;
                upd.act_dt = DateTime.Now;
                upd.comment = comment;
                dbLocal.SaveChanges();
            }
        }

        public void DeleteTransaction(string gpcode, string year, int runno, byte status, byte lv, int org, string plant = "-")
        {
            var del = dbPC.td_transaction.Find(gpcode, year, runno, status, lv, org, plant);
            dbPC.td_transaction.Remove(del);
        }

        public void BackwardTransaction(string gpcode, string year, int runno, byte status, string action, string comment = null)
        {
            using (var dbLocal = new PackingChangeEntities())
            {
                var get_tran = (from a in dbLocal.td_transaction
                                where a.gpcode == gpcode && a.year == year && a.runno == runno
                                && a.status_id == status && a.act_id == action
                                select a).FirstOrDefault();
                if (get_tran != null)
                {
                    //var upd = dbPC.td_transaction.Find(gpcode, year, runno, status, get_tran.lv_id, get_tran.org_id, get_tran.plant_code);
                    //get_tran.actor = null;
                    get_tran.act_id = null;
                    get_tran.comment = comment;
                    get_tran.act_dt = DateTime.Now;
                    dbLocal.SaveChanges();
                }
            }
        }

        private void SkipTransaction(string gpcode, string year, int runno)
        {
            using (var dbLocal = new PackingChangeEntities())
            {
                var query = from a in dbLocal.td_transaction
                            where a.gpcode == gpcode && a.year == year && a.runno == runno && a.act_id == null
                            select a;
                if (query != null)
                {
                    foreach (var item in query)
                    {
                        var upd = dbLocal.td_transaction.Find(gpcode, year, runno, item.status_id, item.lv_id, item.org_id, item.plant_code);
                        upd.act_id = "SKIP";
                        upd.act_dt = DateTime.Now;
                    }
                    dbLocal.SaveChanges();
                }
            }
        }

        private void InsertFeedback(string gpcode, string year, int runno, byte status, byte ulv, int org, string actor, string comment)
        {
            using (var dbLocal = new PackingChangeEntities())
            {
                td_feedback fb = new td_feedback();
                fb.gpcode = gpcode;
                fb.year = year;
                fb.runno = runno;
                fb.status_id = status;
                fb.lv_id = ulv;
                fb.org_id = org;
                fb.actor = actor;
                fb.act_dt = DateTime.Now;
                fb.comment = comment;
                dbLocal.td_feedback.Add(fb);

                dbLocal.SaveChanges();
            }
        }

        //-------------------Function---------------------//

        public bool SaveFiletoDB(string gpcode, string year, int runno, string filename, string path, string uploadby)
        {
            try
            {
                var check = dbPC.td_files.Any(w => w.gpcode == gpcode && w.year == year && w.runno == runno && w.filename == filename);
                if (!check)
                {
                    td_files file = new td_files();
                    file.gpcode = gpcode;
                    file.year = year;
                    file.runno = runno;
                    file.filename = filename;
                    file.path = path;
                    file.upload_by = uploadby;
                    file.upload_dt = DateTime.Now;
                    dbPC.td_files.Add(file);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public DateTime ParseToDate(string inputDT)
        {
            char[] delimiters = new char[] { '-', '/', ' ' };
            var temp = inputDT.Split(delimiters);
            DateTime outputDT = DateTime.Parse(temp[2] + "-" + temp[1] + "-" + temp[0]);
            return outputDT;
        }

        public int GetRunNo(string gpcode, string year)
        {
            var get_run = (from a in dbPC.td_main_data
                           where a.gpcode == gpcode && a.year == year
                           orderby a.runno descending
                           select a).FirstOrDefault();
            if (get_run != null) return (get_run.runno + 1);
            else return 1;
        }

        [HttpPost]
        public JsonResult GetStatusList()
        {
            try
            {
                var result = dbPC.tm_status
                    .Select(r => new { DisplayText = r.status_name, Value = r.status_id });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetULvList()
        {
            try
            {
                var result = dbPC.tm_user_level
                    .Select(r => new { DisplayText = r.lv_name, Value = r.lv_id });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetTNCGroupList()
        {
            try
            {
                var result = dbTNC.tnc_group_master
                    .Select(r => new { DisplayText = r.group_name, Value = r.id });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetShowImpact(string gpcode, string year, int runno)
        {
            try
            {
                var result = from a in dbPC.td_impact_plant
                             where a.gpcode == gpcode && a.year == year && a.runno == runno
                             select a;
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetProdList(string searchTerm)
        {
            var result = dbPC.V_ConcernGroup
                        .Where(w => w.concern_group_id == 1 && w.group_name.Contains(searchTerm))
                        .OrderBy(o => o.group_name)
                        .Select(s => new { id = s.group_id, text = s.group_name })
                        .ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPurList(string searchTerm)
        {
            var result = dbPC.V_ConcernGroup
                        .Where(w => w.concern_group_id == 2 && w.group_name.Contains(searchTerm))
                        .OrderBy(o => o.group_name)
                        .Select(s => new { id = s.group_id, text = s.group_name })
                        .ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSaleList(string searchTerm)
        {
            var result = dbPC.V_ConcernGroup
                        .Where(w => w.concern_group_id == 3 && w.group_name.Contains(searchTerm))
                        .OrderBy(o => o.group_name)
                        .Select(s => new { id = s.group_id, text = s.group_name })
                        .ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetCSList(string searchTerm)
        {
            var result = dbPC.V_ConcernGroup
                        .Where(w => w.concern_group_id == 4 && w.group_name.Contains(searchTerm))
                        .OrderBy(o => o.group_name)
                        .Select(s => new { id = s.group_id, text = s.group_name })
                        .ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetLGList(string searchTerm)
        {
            var result = dbPC.V_ConcernGroup
                        .Where(w => w.concern_group_id == 5 && w.group_name.Contains(searchTerm))
                        .OrderBy(o => o.group_name)
                        .Select(s => new { id = s.group_id, text = s.group_name })
                        .ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPLNList(string searchTerm)
        {
            var result = dbPC.V_ConcernGroup
                        .Where(w => w.concern_group_id == 6 && w.group_name.Contains(searchTerm))
                        .OrderBy(o => o.group_name)
                        .Select(s => new { id = s.group_id, text = s.group_name })
                        .ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetS2CustList(string searchTerm)
        {
            var result = dbPCR.PCR_Customer
                        .Where(w => w.cust_name.Contains(searchTerm) || w.cust_no.Contains(searchTerm))
                        .OrderBy(o => o.cust_no)
                        .Select(s => new { id = s.cust_no, text = s.cust_no + " - " + s.cust_name })
                        .ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, VaryByParam = "*", NoStore = true)]
        public ActionResult GetSelectedTemp(string gpcode, string year, int runno, byte gtype)
        {
            var get_selected = (from a in dbPC.td_temp_concern
                                where a.gpcode == gpcode && a.year == year
                                && a.runno == runno && a.concern_group_id == gtype
                                select a.group_id).ToList();
            var get_inf = dbTNC.tnc_group_master.Where(w => get_selected.Contains(w.id))
                .Select(s => new { id = s.id, text = s.group_name, locked = true });
            if (get_inf != null)
                return Json(get_inf, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
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

        private void InsertTempConcern(string gpcode, string year, int runno, byte grouptype, string glist)
        {
            if (!string.IsNullOrEmpty(glist))
            {
                int[] groups = glist.Split(',').Select(s => Convert.ToInt32(s)).ToArray();
                using (var dbLocal = new PackingChangeEntities())
                {
                    foreach (var item in groups)
                    {
                        var check = dbPC.td_temp_concern.Find(gpcode, year, runno, grouptype, item);
                        if (check == null)
                        {
                            var temp = new td_temp_concern();
                            temp.gpcode = gpcode;
                            temp.year = year;
                            temp.runno = runno;
                            temp.concern_group_id = grouptype;
                            temp.group_id = item;
                            dbLocal.td_temp_concern.Add(temp);
                        }
                    }
                    dbLocal.SaveChanges();
                }
            }
        }

        public string IsManufacturing(string gpcode, string year, int runno)
        {
            return dbPC.td_main_data.Find(gpcode, year, runno).production_type;
        }

        public void UploadFiles(string gpcode, string year, int runno, IEnumerable<HttpPostedFileBase> atfiles, string issuer)
        {
            string subPath = "~/UploadFiles/" + year + "/" + gpcode + "/" + runno + "/";
            if (!Directory.Exists(Server.MapPath(subPath)))
                Directory.CreateDirectory(Server.MapPath(subPath));

            if (atfiles != null)
            {
                foreach (var atfile in atfiles)
                {
                    if (atfile != null && atfile.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(atfile.FileName);
                        var path = Path.Combine(Server.MapPath(subPath), fileName);
                        if (SaveFiletoDB(gpcode, year, runno, fileName, subPath, issuer))
                        {
                            atfile.SaveAs(path);
                        }
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult PackSTDList(string gpcode, string year, int runno, int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = from a in dbPC.td_pack_std
                            where a.gpcode == gpcode && a.year == year && a.runno == runno
                            select a;

                int TotalRecord = query.Count();

                var output = query.ToList()
                    .Select(s => new
                    {
                        s.gpcode,
                        s.year,
                        s.runno,
                        s.series,
                        s.cust_no,
                        s.status,
                        s.submit_dt,
                        pack_file = !string.IsNullOrEmpty(s.pack_file) ?
                        "<a href='" + Url.Content(s.pack_file) + "' target='_blank'><i class='icon-file'></i></a>" : "",
                        reason = !string.IsNullOrEmpty(s.reason) ? s.reason : ""
                    }).AsQueryable().OrderBy(jtSorting).Skip(jtStartIndex).Take(jtPageSize);

                return Json(new { Result = "OK", Records = output, TotalRecordCount = TotalRecord });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetCustList()
        {
            try
            {
                var result = dbPCR.PCR_Customer
                    .Select(r => new { DisplayText = r.cust_no + " - " + r.cust_name, Value = r.cust_no });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult CreatePackSTD(HttpPostedFileBase files)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);

                var get_series = (from a in dbPC.td_pack_std
                                  where a.gpcode == gpcode && a.year == year && a.runno == runno
                                  select a).Count();

                var pack = new td_pack_std();
                pack.gpcode = gpcode;
                pack.year = year;
                pack.runno = runno;
                pack.series = (byte)(get_series + 1);
                pack.cust_no = Request.Form["selCust"];
                pack.status = Request.Form["selStatus"];
                pack.submit_dt = ParseToDate(Request.Form["dpSubmit"]);
                pack.issue_org = org;
                pack.reason = Request.Form["txaReason"];

                string subPath = "~/UploadFiles/" + year + "/" + gpcode + "/" + runno + "/";
                if (!Directory.Exists(Server.MapPath(subPath)))
                    Directory.CreateDirectory(Server.MapPath(subPath));

                if (files != null && files.ContentLength > 0)
                {
                    if (files.ContentType == "application/pdf")//Check accept file type.
                    {
                        var fileName = Path.GetFileName(files.FileName);//DateTime.Now.Millisecond + "_" +
                        var path = Path.Combine(Server.MapPath(subPath), fileName);
                        files.SaveAs(path);
                        pack.pack_file = subPath + fileName;
                    }
                }

                dbPC.td_pack_std.Add(pack);
                dbPC.SaveChanges();
                //return Json(new { Result = "OK", Record = dbPC.td_pack_std.OrderByDescending(o => o.series).FirstOrDefault() });
                return RedirectToAction("PackingSTD", "Home", new { gpcode = gpcode, year = year, runno = runno, org = org });
            }
            catch (Exception)
            {
                throw;
                //return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UpdatePackSTD(HttpPostedFileBase files)
        {
            try
            {
                string actor = Session["PCO_Auth"].ToString();
                string gpcode = Request.Form["hdgc"];
                string year = Request.Form["hdyy"];
                int runno = int.Parse(Request.Form["hdrn"]);
                int org = int.Parse(Request.Form["hdorg"]);
                byte series = byte.Parse(Request.Form["hdseries"]);

                var pack = dbPC.td_pack_std.Find(gpcode, year, runno, series);
                pack.cust_no = Request.Form["selCust"];
                pack.status = Request.Form["selStatus"];
                pack.submit_dt = ParseToDate(Request.Form["dpSubmit"]);
                pack.reason = Request.Form["txaReason"];

                string subPath = "~/UploadFiles/" + year + "/" + gpcode + "/" + runno + "/";
                if (!Directory.Exists(Server.MapPath(subPath)))
                    Directory.CreateDirectory(Server.MapPath(subPath));

                if (files != null && files.ContentLength > 0)
                {
                    if (files.ContentType == "application/pdf")//Check accept file type.
                    {
                        var fileName = Path.GetFileName(files.FileName);//DateTime.Now.Millisecond + "_" +
                        var path = Path.Combine(Server.MapPath(subPath), fileName);
                        files.SaveAs(path);
                        pack.pack_file = subPath + fileName;
                    }
                }

                dbPC.SaveChanges();

                return RedirectToAction("PackingSTD", "Home", new { gpcode = gpcode, year = year, runno = runno, org = org });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [OutputCache(Duration = 0, VaryByParam = "*", NoStore = true)]
        public JsonResult GetPackSTD(string gpcode, string year, int runno, byte series)
        {
            var pack = (from a in dbPC.td_pack_std
                        where a.gpcode == gpcode && a.year == year && a.runno == runno && a.series == series
                        select new
                        {
                            a.cust_no,
                            a.status,
                            a.reason,
                            a.series,
                            dd = a.submit_dt.Day,
                            mm = a.submit_dt.Month,
                            yy = a.submit_dt.Year
                        }).FirstOrDefault();
            if (pack != null)
                return Json(pack, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPIC(int org)
        {
            var pic = dbTNC.tnc_user.Where(w => w.emp_group == org && w.emp_position != 2).Select(c => new
            {
                ID = c.emp_code,
                Text = c.emp_fname + " " + c.emp_lname
            });
            return Json(pic, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Fuction Send Email (Insert to MailCenter)
        /// </summary>
        /// <param name="gpcode"></param>
        /// <param name="year"></param>
        /// <param name="runno"></param>
        /// <param name="receiver">Send Email to</param>
        /// <param name="type">0:Default,1:Reject,2:Feedback,3:Close,4:Cancel,5:Revise</param>
        /// <param name="content">Reason for reject or cancel, Feedback</param>
        /// <param name="state">Status name waiting</param>
        public void SendEmailCenter(string gpcode, string year, int runno, string receiver, int type = 0, string content = "", string state = "")
        {
            string actor = Session["PCO_Auth"].ToString();
            string act_name = Session["PCO_Name"].ToString();
            if (!string.IsNullOrEmpty(receiver))
            {
                string mailto = "";
                char[] delimiter = new char[] { ',' };
                string[] email = receiver.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                int max_email = 47;
                if (email.Length > max_email)//Max send email = 50
                {
                    for (int i = 0; i < max_email; i++)
                    {
                        mailto += "," + email[i];
                    }
                    mailto = mailto.Substring(1);
                }
                else
                {
                    mailto = receiver;
                }

                var get_doc = dbPC.td_main_data.Find(gpcode, year, runno);

                if (get_doc != null)
                {
                    TNCUtility tnc_util = new TNCUtility();
                    string subject = "";
                    string body = "";//For Real
                    //string body = "Mail :" + mailto + "<br />";//For Test
                    string int_link = "http://webExternal/PKC/Home";
                    //string ext_link = "http://webexternal.nok.co.th/PKC/Home";
                    string pkc = "PKC-" + get_doc.gpcode + "-" + get_doc.year + "-" + get_doc.runno.ToString("000");

                    if (type == 0)//waiting for process
                    {
                        subject = "Packing Change online waiting for process (" + pkc + ")";
                        body += "Dear. All Concern,<br /><br />" +
                            "You have Packing Change No. <b>" + pkc + "</b> waiting for Process at State : " + state + "<br />" +
                            "<br /><a href='" + int_link + "/MainPCO?id=" + pkc + "'>Packing Change Online</a><br />" +
                            "<br /><b>Best Regard.<br />" + "From Packing Change-Admin</b>";
                    }
                    else if (type == 1)//Email Reject
                    {
                        subject = "Packing Change online was Reject (" + pkc + ")";
                        body += "Dear. All Concern,<br /><br />" +
                            "You have Packing Change No. <b>" + pkc + "</b> was Reject as detail below<br />" +
                            "Change content : " + get_doc.change_detail + "<br />" +
                            "Reject by : " + act_name + "<br />" +
                            "Reason : " + content + "<br />" +
                            "<br /><a href='" + int_link + "/ClosePCO?id=" + pkc + "'>Packing Change Online</a><br />" +
                            "<br /><b>Best Regard.<br />" + "From Packing Change-Admin</b>";
                    }
                    else if (type == 2)//Email Feedback
                    {
                        subject = "Packing Change online have Feedback from concern section (" + pkc + ")";
                        body += "Dear. All Concern,<br /><br />" +
                            "You have feed back of Packing Change No. <b>" + pkc + "</b> as detail below<br />" +
                            "Change content : " + get_doc.change_detail + "<br />" +
                            "Feedback by : " + act_name + "<br />" +
                            "Feedback detail : " + content + "<br />" +
                            "<br /><a href='" + int_link + "/MainPCO?id=" + pkc + "'>Packing Change Online</a><br />" +
                            "<br /><b>Best Regard.<br />" + "From Packing Change-Admin</b>";
                    }
                    else if (type == 3)//Email Close
                    {
                        subject = "Packing Change online was Closed (" + pkc + ")";
                        body += "Dear. All Concern,<br /><br />" +
                            "Packing Change No. <b>" + pkc + "</b> was Closed as detail below<br />" +
                            "Change content : " + get_doc.change_detail + "<br />" +
                            "<a href='" + int_link + "/ClosePCO?id=" + pkc + "'>Packing Change Online</a><br />" +
                            "<br /><b>Best Regard.<br />" + "From Packing Change-Admin</b>";
                    }
                    else if (type == 4)//Email Cancel
                    {
                        subject = "Packing Change online was cancel (" + pkc + ")";
                        body += "Dear. All Concern,<br /><br />" +
                            "You have Packing Change No. <b>" + pkc + "</b> was cancel as detail below<br />" +
                            "Change content : " + get_doc.change_detail + "<br />" +
                            "Cancel by : " + act_name + "<br />" +
                            "Reason : " + content + "<br />" +
                            "<br /><a href='" + int_link + "/ClosePCO?id=" + pkc + "'>Packing Change Online</a><br />" +
                            "<br /><b>Best Regard.<br />" + "From Packing Change-Admin</b>";
                    }
                    else if (type == 5)//Email Revise
                    {
                        subject = "Packing Change online no. " + pkc + " was sent back for revision.";
                        body += "Dear. All Concern,<br /><br />" +
                            "You have Packing Change No. <b>" + pkc + "</b> was revised as detail below<br />" +
                            "Change content : " + get_doc.change_detail + "<br />" +
                            "by : " + act_name + "<br />" +
                            "Reason : " + content + "<br />" +
                            "<br /><a href='" + int_link + "/MainPCO?id=" + pkc + "'>Packing Change Online</a><br />" +
                            "<br /><b>Best Regard.<br />" + "From Packing Change-Admin</b>";
                    }

                    tnc_util.SendMail(31, "TNCAutoMail-PKC@nok.co.th", mailto, subject, body);//For Real
                    //tnc_util.SendMail(31, "TNCAutoMail-PKC@nok.co.th", "noppamas@nok.co.th", subject, body);//For Test
                }
            }
        }

        private string GetEmail(string emp_code)
        {
            //var query = dbTNC.tnc_user.Find(emp_code);
            var query = (from a in dbTNC.tnc_user
                         where a.emp_code == emp_code && !(a.email == null || a.email.Equals("")) && a.emp_status == "A"
                         select a).FirstOrDefault();
            return query != null ? query.email : "";
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="org">group id</param>
        /// <param name="mode">1:lower Mgr. 2:Mgr. 3:Mgr. and lower</param>
        /// <returns></returns>
        private string GetEmail(int org, byte mode)
        {
            //mode -> 1:lower Mgr. 2:Mgr. 3:Mgr. and lower
            string email = "";
            var get_pos = dbPC.tm_user_level.Find(mode);
            if (mode != 3)
            {
                var query = from a in dbTNC.V_Employee_Info
                            where a.group_id == org && a.emp_status == "A" && !(a.email == null || a.email.Equals(""))
                            && a.position_level >= get_pos.pos_min && a.position_level <= get_pos.pos_max 
                            select a.email;
                foreach (var item in query)
                {
                    email += ", " + item;
                }
                email = email.Substring(2);
            }
            else
            {
                var query = from a in dbTNC.V_Employee_Info
                            where a.group_id == org && a.position_level < get_pos.pos_min && a.emp_status == "A"
                            && !(a.email == null || a.email.Equals(""))
                            select a.email;
                foreach (var item in query)
                {
                    email += ", " + item;
                }
                email = email.Substring(2);
            }
            return email;
        }

        private string GetEmail(string gpcode, string year, int runno, string act = null)
        {
            var email = "";
            var tran = from a in dbPC.td_transaction
                       where a.gpcode == gpcode && a.year == year && a.runno == runno && a.actor != null && a.act_id == act
                       select a.actor;
            if (tran.Any())
            {
                foreach (var item in tran)
                {
                    var query = (from a in dbTNC.tnc_user
                                 where a.emp_code == item && a.emp_status == "A" && !(a.email == null || a.email.Equals(""))
                                 select a).FirstOrDefault();
                    if (query != null)
                    {
                        email += ", " + query.email;
                    }
                }
                email = email.Substring(2);
            }

            return email;
        }
        
        private string GetEmail(string gpcode, string year, int runno, byte status, byte lv)
        {
            var email = "";
            var tran = from a in dbPC.td_transaction
                       where a.gpcode == gpcode && a.year == year && a.runno == runno && a.actor != null
                       select a;
            if (status != 0)
            {
                tran = tran.Where(a => a.status_id == status);
            }
            if (lv != 0)
            {
                tran = tran.Where(a => a.lv_id == lv);
            }

            if (tran.Any())
            {
                foreach (var item in tran)
                {
                    var query = (from a in dbTNC.tnc_user
                                 where a.emp_code == item.actor && a.emp_status == "A" && !(a.email == null || a.email.Equals(""))
                                 select a).FirstOrDefault();
                    if (query != null)
                    {
                        email += ", " + query.email;
                    }
                }
                email = email.Substring(2);
            }

            return email;
        }

        public ActionResult PrintPDF(string gpcode, string year, int runno)
        {
            return new ActionAsPdf("ReportPDF", new { gpcode = gpcode, year = year, runno = runno });
        }

        public ActionResult ReportPDF(string gpcode, string year, int runno)
        {
            //ViewBag.Title = "SA Report";

            var main = (from a in dbPC.V_Tran
                        where a.gpcode == gpcode && a.year == year && a.runno == runno
                        select a).FirstOrDefault();

            var queryImpact = from a in dbPC.td_impact_plant.Where(w => w.gpcode == gpcode && w.year == year && w.runno == runno)
                              join b in dbPC.tm_plant on a.plant_code equals b.plant_code into g
                              from x in g.DefaultIfEmpty()
                              select new
                              {
                                  x.plant_code,
                                  x.plant_name
                              };

            var getImpact = "";

            foreach (var item in queryImpact)
            {
                getImpact += ", " + item.plant_name;
            }
            ViewBag.ShowImpactPlant = getImpact.Substring(2);

            var tran = from a in dbPC.V_ShowTran
                       where a.gpcode == gpcode && a.year == year && a.runno == runno //&& a.status_id == 1
                       orderby a.status_id, a.lv_id
                       select a;
            ViewBag.IssTran = tran.Where(w => w.status_id == 1);
            ViewBag.QATran = tran.Where(w => w.status_id == 2);
            ViewBag.CCTran = tran.Where(w => (w.status_id >= 3 && w.status_id <= 5) || (w.status_id >= 8 && w.status_id <= 13));
            ViewBag.SLCSTran = tran.Where(w => w.status_id == 6);
            ViewBag.SLCS = from a in dbPC.td_salecs
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;
            ViewBag.PKTran = tran.Where(w => w.status_id == 7);
            ViewBag.Pack = from a in dbPC.V_ShowPack
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           orderby a.series
                           select a;
            ViewBag.ClsTran = tran.Where(w => w.status_id == 14);

            return View(main);
        }

        public ActionResult PrintItem(string gpcode, string year, int runno)
        {
            return new ActionAsPdf("ReportItem", new { gpcode = gpcode, year = year, runno = runno }) 
            {
                PageOrientation = Orientation.Landscape
            };
        }

        public ActionResult ReportItem(string gpcode, string year, int runno)
        {
            var get_item = from a in dbPC.td_item_list
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;
            ViewBag.PNO = "PKC-" + gpcode + "-" + year + "-" + runno.ToString("000");
            return View(get_item);
        }

        public ActionResult PrintPLN(string gpcode, string year, int runno)
        {
            //string footer = "--footer-right \"Date: [date] [time]\" " + "--footer-center \"Page: [page] of [toPage]\" --footer-line --footer-font-size \"9\" --footer-spacing 5 --footer-font-name \"calibri light\"";
            return new ActionAsPdf("ReportPLN", new { gpcode = gpcode, year = year, runno = runno })
            {
                PageOrientation = Orientation.Landscape
                //CustomSwitches = footer
            };
        }

        public ActionResult ReportPLN(string gpcode, string year, int runno)
        {
            var main = (from a in dbPC.V_Tran
                        where a.gpcode == gpcode && a.year == year && a.runno == runno
                        select a).FirstOrDefault();

            var queryImpact = from a in dbPC.td_impact_plant.Where(w => w.gpcode == gpcode && w.year == year && w.runno == runno)
                              join b in dbPC.tm_plant on a.plant_code equals b.plant_code into g
                              from x in g.DefaultIfEmpty()
                              select new
                              {
                                  x.plant_code,
                                  x.plant_name
                              };

            var getImpact = "";

            foreach (var item in queryImpact)
            {
                getImpact += ", " + item.plant_name;
            }
            ViewBag.ShowImpactPlant = getImpact.Substring(2);

            var get_item = from a in dbPC.td_item_list
                           where a.gpcode == gpcode && a.year == year && a.runno == runno
                           select a;
            ViewBag.ItemList = get_item;

            return View(main);
        }
    }
}