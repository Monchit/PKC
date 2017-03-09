using PackingChange1.Models;
using System;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.Mvc;
using WebCommonFunction;

namespace PackingChange1.Controllers
{
    public class MasterController : Controller
    {
        private PackingChangeEntities dbPC = new PackingChangeEntities();
        private TNC_ADMINEntities dbTNC = new TNC_ADMINEntities();
        private PCREntities dbPCR = new PCREntities();

        [Chk_Authen]
        [Chk_Authorize_Admin]
        public ActionResult Users()
        {
            return View();
        }

        [HttpPost]
        public JsonResult UserList(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = dbPC.tm_sys_user;

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.emp_code,
                        s.utype_code
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
        public JsonResult CreateUser()
        {
            try
            {
                var formData = Request.Form["emp_code"];
                var dbData = dbPC.tm_sys_user.Where(w => w.emp_code == formData).FirstOrDefault();
                if (dbData == null)
                {
                    tm_sys_user data = new tm_sys_user();
                    data.emp_code = formData;
                    data.utype_code = Request.Form["utype_code"];
                    data.update_dt = DateTime.Now;

                    dbPC.tm_sys_user.Add(data);
                }

                dbPC.SaveChanges();

                return Json(new { Result = "OK", Record = dbPC.tm_sys_user.OrderByDescending(o => o.update_dt).FirstOrDefault() });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateUser()
        {
            try
            {
                var emp_code = Request.Form["emp_code"];
                var data = dbPC.tm_sys_user.Find(emp_code);
                data.utype_code = Request.Form["utype_code"];
                data.update_dt = DateTime.Now;
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteUser()
        {
            try
            {
                var emp_code = Request.Form["emp_code"];
                var data = dbPC.tm_sys_user.Find(emp_code);
                dbPC.tm_sys_user.Remove(data);
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------------------------------------//

        [Chk_Authen]
        [Chk_Authorize_Admin]
        public ActionResult UserType()
        {
            return View();
        }

        [HttpPost]
        public JsonResult UserTypeList(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = dbPC.tm_sys_utype;

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.utype_code,
                        s.utype_name,
                        s.utype_lv
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
        public JsonResult CreateUserType()
        {
            try
            {
                var formData = Request.Form["utype_code"];
                var dbData = dbPC.tm_sys_utype.Where(w => w.utype_code == formData).FirstOrDefault();
                if (dbData == null)
                {
                    tm_sys_utype data = new tm_sys_utype();
                    data.utype_code = formData;
                    data.utype_name = Request.Form["utype_name"];
                    data.utype_lv = byte.Parse(Request.Form["utype_lv"]);
                    data.update_dt = DateTime.Now;

                    dbPC.tm_sys_utype.Add(data);
                }

                dbPC.SaveChanges();

                return Json(new { Result = "OK", Record = dbPC.tm_sys_utype.OrderByDescending(o => o.update_dt).FirstOrDefault() });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateUserType()
        {
            try
            {
                var utype_code = Request.Form["utype_code"];
                var data = dbPC.tm_sys_utype.Find(utype_code);
                data.utype_name = Request.Form["utype_name"];
                data.utype_lv = byte.Parse(Request.Form["utype_lv"]);
                data.update_dt = DateTime.Now;
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteUserType()
        {
            try
            {
                var utype_code = Request.Form["utype_code"];
                var data = dbPC.tm_sys_utype.Find(utype_code);
                dbPC.tm_sys_utype.Remove(data);
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------------------------------------//

        [Chk_Authen]
        [Chk_Authorize_Admin]
        public ActionResult Plant()
        {
            //ViewBag.Menu = 13;
            return View();
        }

        [HttpPost]
        public JsonResult PlantList(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = dbPC.tm_plant;

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.plant_code,
                        s.plant_name,
                        s.active
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
        public JsonResult CreatePlant()
        {
            try
            {
                var formData = Request.Form["plant_code"];
                var dbData = dbPC.tm_plant.Where(w => w.plant_code == formData).FirstOrDefault();
                if (dbData == null)
                {
                    tm_plant data = new tm_plant();
                    data.plant_code = formData;
                    data.plant_name = Request.Form["plant_name"];
                    data.update_dt = DateTime.Now;

                    dbPC.tm_plant.Add(data);
                }

                dbPC.SaveChanges();

                return Json(new { Result = "OK", Record = dbPC.tm_plant.OrderByDescending(o => o.update_dt).FirstOrDefault() });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdatePlant()
        {
            try
            {
                var plant_code = Request.Form["plant_code"];
                var data = dbPC.tm_plant.Find(plant_code);
                data.plant_name = Request.Form["plant_name"];
                data.active = Request.Form["active"] == "1" ? true : false;
                data.update_dt = DateTime.Now;
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeletePlant()
        {
            try
            {
                var plant_code = Request.Form["plant_code"];
                var data = dbPC.tm_plant.Find(plant_code);
                dbPC.tm_plant.Remove(data);
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------------------------------------//

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult Group()
        {
            //ViewBag.Menu = 14;
            return View();
        }

        [HttpPost]
        public JsonResult GroupList(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = dbPC.tm_sys_group;

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.sys_group_code,
                        s.sys_group_name,
                        s.group_type,
                        s.plant_code
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
        public JsonResult CreateGroup()
        {
            try
            {
                var formData = Request.Form["sys_group_code"];
                var dbData = dbPC.tm_sys_group.Where(w => w.sys_group_code == formData).FirstOrDefault();
                if (dbData == null)
                {
                    tm_sys_group data = new tm_sys_group();
                    data.sys_group_code = formData;
                    data.sys_group_name = Request.Form["sys_group_name"];
                    data.plant_code = Request.Form["plant_code"];
                    data.group_type = Request.Form["group_type"];
                    data.update_dt = DateTime.Now;

                    dbPC.tm_sys_group.Add(data);
                }

                dbPC.SaveChanges();

                return Json(new { Result = "OK", Record = dbPC.tm_sys_group.OrderByDescending(o => o.update_dt).FirstOrDefault() });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateGroup()
        {
            try
            {
                var formData = Request.Form["sys_group_code"];
                var data = dbPC.tm_sys_group.Find(formData);
                data.sys_group_name = Request.Form["sys_group_name"];
                data.plant_code = Request.Form["plant_code"];
                data.group_type = Request.Form["group_type"];
                data.update_dt = DateTime.Now;
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteGroup()
        {
            try
            {
                var formData = Request.Form["sys_group_code"];
                var data = dbPC.tm_sys_group.Find(formData);
                dbPC.tm_sys_group.Remove(data);
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------------------------------------//

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult GroupUser()
        {
            //ViewBag.Menu = 15;
            return View();
        }

        [HttpPost]
        public JsonResult GroupUserList(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = dbPC.tm_sys_group_user;

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.sys_group_code,
                        s.emp_code,
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
        public JsonResult CreateGroupUser()
        {
            try
            {
                var sys_group_code = Request.Form["sys_group_code"];
                var emp_code = Request.Form["emp_code"];
                var dbData = dbPC.tm_sys_group_user
                            .Where(w => w.sys_group_code == sys_group_code && w.emp_code == emp_code).FirstOrDefault();
                if (dbData == null)
                {
                    tm_sys_group_user data = new tm_sys_group_user();
                    data.sys_group_code = sys_group_code;
                    data.emp_code = emp_code;
                    data.lv_id = byte.Parse(Request.Form["lv_id"]);
                    data.update_dt = DateTime.Now;

                    dbPC.tm_sys_group_user.Add(data);
                }

                dbPC.SaveChanges();

                return Json(new { Result = "OK", Record = dbPC.tm_sys_group_user.OrderByDescending(o => o.update_dt).FirstOrDefault() });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //[HttpPost]
        //public JsonResult UpdateGroupUser()
        //{
        //    try
        //    {
        //        var formData = Request.Form["sys_group_code"];
        //        var data = dbPC.tm_sys_group.Find(formData);
        //        data.sys_group_name = Request.Form["sys_group_name"];
        //        data.plant_code = Request.Form["plant_code"];
        //        data.update_dt = DateTime.Now;
        //        dbPC.SaveChanges();

        //        return Json(new { Result = "OK" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { Result = "ERROR", Message = ex.Message });
        //    }
        //}

        [HttpPost]
        public JsonResult DeleteGroupUser(string emp_code, string group)
        {
            try
            {
                var data = dbPC.tm_sys_group_user.Find(group, emp_code);
                dbPC.tm_sys_group_user.Remove(data);
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------------------------------------//

        [Chk_Authen]
        [Chk_Authorize_Admin]
        public ActionResult ConcernType()
        {
            //ViewBag.Menu = 16;
            return View();
        }

        [HttpPost]
        public JsonResult ConcernTypeList(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = dbPC.tm_sys_concern_group;

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.concern_group_id,
                        s.concern_group_name
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
        public JsonResult CreateConcernType()
        {
            try
            {
                //var id = byte.Parse(Request.Form["concern_group_id"]);
                //var dbData = dbPC.tm_sys_concern_group.Where(w => w.concern_group_id == id).FirstOrDefault();
                //if (dbData == null)
                //{
                tm_sys_concern_group data = new tm_sys_concern_group();
                //data.concern_group_id = id;
                data.concern_group_name = Request.Form["concern_group_name"];

                dbPC.tm_sys_concern_group.Add(data);
                //}

                dbPC.SaveChanges();

                return Json(new
                {
                    Result = "OK",
                    Record = dbPC.tm_sys_concern_group
                           .OrderByDescending(o => o.concern_group_id).FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //[HttpPost]
        //public JsonResult UpdateConcernType()
        //{
        //    try
        //    {
        //        var id = Request.Form["concern_group_id"];
        //        var data = dbPC.tm_sys_concern_group.Find(id);
        //        data.concern_group_name = Request.Form["concern_group_name"];
        //        dbPC.SaveChanges();

        //        return Json(new { Result = "OK" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { Result = "ERROR", Message = ex.Message });
        //    }
        //}

        [HttpPost]
        public JsonResult DeleteConcernType()
        {
            try
            {
                var id = byte.Parse(Request.Form["concern_group_id"]);
                var data = dbPC.tm_sys_concern_group.Find(id);
                dbPC.tm_sys_concern_group.Remove(data);
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------------------------------------//

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult ConcernGroup()
        {
            //ViewBag.Menu = 17;
            return View();
        }

        [HttpPost]
        public JsonResult ConcernGroupList(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = dbPC.tm_concern_group;

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.concern_group_id,
                        s.group_id,
                        s.create_dt
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
        public JsonResult CreateConcernGroup()
        {
            try
            {
                var id = byte.Parse(Request.Form["concern_group_id"]);
                var group = int.Parse(Request.Form["group_id"]);
                var dbData = dbPC.tm_concern_group.Where(w => w.concern_group_id == id && w.group_id == group).FirstOrDefault();
                if (dbData == null)
                {
                    tm_concern_group data = new tm_concern_group();
                    data.concern_group_id = id;
                    data.group_id = group;
                    data.create_dt = DateTime.Now;

                    dbPC.tm_concern_group.Add(data);
                }

                dbPC.SaveChanges();

                return Json(new
                {
                    Result = "OK",
                    Record = dbPC.tm_concern_group
                           .OrderByDescending(o => o.create_dt).FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //[HttpPost]
        //public JsonResult UpdateConcernType()
        //{
        //    try
        //    {
        //        var id = Request.Form["concern_group_id"];
        //        var data = dbPC.tm_sys_concern_group.Find(id);
        //        data.concern_group_name = Request.Form["concern_group_name"];
        //        dbPC.SaveChanges();

        //        return Json(new { Result = "OK" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { Result = "ERROR", Message = ex.Message });
        //    }
        //}

        [HttpPost]
        public JsonResult DeleteConcernGroup()
        {
            try
            {
                var id = byte.Parse(Request.Form["concern_group_id"]);
                var group = int.Parse(Request.Form["group_id"]);
                var data = dbPC.tm_concern_group.Find(id, group);
                dbPC.tm_concern_group.Remove(data);
                dbPC.SaveChanges();

                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------------------------------------//

        [HttpPost]
        public JsonResult GetTNCUserList()
        {
            try
            {
                var result = dbTNC.tnc_user.Where(w=> w.emp_status == "A").OrderBy(o => o.emp_fname).ThenBy(o => o.emp_lname)
                    .Select(r => new { DisplayText = r.emp_fname + " " + r.emp_lname, Value = r.emp_code });
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
                var result = dbTNC.tnc_group_master.OrderBy(o => o.group_name)
                    .Select(r => new { DisplayText = r.group_name, Value = r.id });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetUTypeList()
        {
            try
            {
                var result = dbPC.tm_sys_utype
                    .Select(r => new { DisplayText = r.utype_name, Value = r.utype_code });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetUserLvList()
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
        public JsonResult GetConcernTypeList()
        {
            try
            {
                var result = dbPC.tm_sys_concern_group
                    .Select(r => new { DisplayText = r.concern_group_name, Value = r.concern_group_id });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetPlantList()
        {
            try
            {
                var result = dbPC.tm_plant
                    .Select(r => new { DisplayText = r.plant_name, Value = r.plant_code });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetGroupList()
        {
            try
            {
                var result = dbPC.tm_sys_group
                    .Select(r => new { DisplayText = r.sys_group_name, Value = r.sys_group_code });
                return Json(new { Result = "OK", Options = result });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        //------------------------------------------------//

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult ReportPCO()
        {
            ViewBag.SelectGroup = from a in dbTNC.tnc_group_master
                                  orderby a.group_name ascending
                                  select a;
            return View();
        }

        [HttpPost]
        public JsonResult ExportPCOList(DateTime date_from, DateTime date_to, int group = 0, int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var dateto = date_to.AddDays(1);
                var query = from a in dbPC.V_Report
                            where a.request_date >= date_from && a.request_date <= dateto
                            select a;

                if (group != 0)
                {
                    query = query.Where(w => w.issue_group == group);
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
                        s.request_date,
                        s.status_name,
                        s.DiffDate
                    }).OrderBy(jtSorting).Skip(jtStartIndex).Take(jtPageSize);

                //Return result to jTable
                return Json(new { Result = "OK", Records = output, TotalRecordCount = TotalRecord });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        public void ExportPCO(DateTime date_from, DateTime date_to, int selGroup = 0)
        {
            TNCUtility util = new TNCUtility();
            var dateto = date_to.AddDays(1);
            var query = from a in dbPC.V_Report
                        where a.request_date >= date_from && a.request_date <= dateto
                        select a;

            if (selGroup != 0)
            {
                query = query.Where(w => w.issue_group == selGroup);
            }

            var output = query.ToList()
                    .Select(s => new
                    {
                        DocNo = s.id,
                        TypeOfChange = s.chage_type,
                        ChangeDetail = s.change_detail,
                        IssueGroup = s.group_name,
                        IssueDate = s.request_date.ToString("dd/MM/yyyy"),
                        Status = s.status_name,
                        Days = s.DiffDate
                    });

            util.CreateExcel(output.ToList(), "PackingChange");
        }

        //------------------------------------------------//

        [Chk_Authen]
        [Chk_Authorize_PowerUser]
        public ActionResult ReportPCR()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ExportPCRList(DateTime? date_from, DateTime? date_to, int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                var query = from a in dbPCR.v_pcr_main
                            select a;

                if (date_from != null && date_to != null)
                {
                    var dateto = date_to.Value.AddDays(1);
                    query = query.Where(a => a.issued_date >= date_from.Value && a.issued_date <= dateto);
                }

                //Get data from database
                int TotalRecord = query.Count();

                // Paging
                var output = query
                    .Select(s => new
                    {
                        s.pcr_id,
                        s.emp_fname,
                        s.emp_lname,
                        s.group_name,
                        s.state_name,
                        s.status_name,
                        s.email
                    }).OrderBy(jtSorting).Skip(jtStartIndex).Take(jtPageSize);

                //Return result to jTable
                return Json(new { Result = "OK", Records = output, TotalRecordCount = TotalRecord });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        public void ExportPCR(DateTime? date_from, DateTime? date_to)
        {
            TNCUtility util = new TNCUtility();
            var query = from a in dbPCR.v_pcr_main
                        select a;

            if (date_from != null && date_to != null)
            {
                var dateto = date_to.Value.AddDays(1);
                query = query.Where(a => a.issued_date >= date_from.Value && a.issued_date <= dateto);
            }

            var output = query.ToList()
                    .Select(s => new
                    {
                        s.pcr_id,
                        s.emp_fname,
                        s.emp_lname,
                        s.group_name,
                        s.state_name,
                        s.status_name,
                        s.email,
                        s.type_name,
                        s.item_no,
                        s.cust_no,
                        s.issue_tel,
                        IssueDate = s.issued_date != null ? s.issued_date.Value.ToString("dd-MM-yyyy") : "",
                        ExpectDate = s.expected_date != null ? s.expected_date.Value.ToString("dd-MM-yyyy") : "",
                        CompDate = s.completed_date != null ? s.completed_date.Value.ToString("dd-MM-yyyy") : "",
                        s.change_txt
                    });

            util.CreateExcel(output.ToList(), "PCRReport");
        }
    }
}