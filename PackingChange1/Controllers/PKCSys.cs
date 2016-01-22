using PackingChange1.Models;
using System.Linq;

namespace PackingChange1.Controllers
{
    public class PKCSys
    {
        private PackingChangeEntities dbPC = new PackingChangeEntities();
        private TNC_ADMINEntities dbTNC = new TNC_ADMINEntities();

        private string mgrId;

        public string MgrId
        {
            get { return mgrId; }
            set { mgrId = value; }
        }

        private string mgrEMail;

        public string MgrEMail
        {
            get { return mgrEMail; }
            set { mgrEMail = value; }
        }

        private void GetSysUser(string group_type, string plant, byte lv = 1)
        {
            var get_group = dbPC.tm_sys_group.Where(w => w.group_type == group_type && w.plant_code == plant);
        }

        private void GetSysUser(string group_code, byte lv = 1)
        {
            FindSysUser(group_code, lv);
        }

        private void FindSysUser(string group_code, byte lv)
        {
        }
    }
}