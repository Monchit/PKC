using System.Web.Optimization;

namespace PackingChange1
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                        "~/Scripts/jquery-ui-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.unobtrusive*",
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/site.css"));

            bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
                        "~/Content/themes/base/jquery.ui.core.css",
                        "~/Content/themes/base/jquery.ui.resizable.css",
                        "~/Content/themes/base/jquery.ui.selectable.css",
                        "~/Content/themes/base/jquery.ui.accordion.css",
                        "~/Content/themes/base/jquery.ui.autocomplete.css",
                        "~/Content/themes/base/jquery.ui.button.css",
                        "~/Content/themes/base/jquery.ui.dialog.css",
                        "~/Content/themes/base/jquery.ui.slider.css",
                        "~/Content/themes/base/jquery.ui.tabs.css",
                        "~/Content/themes/base/jquery.ui.datepicker.css",
                        "~/Content/themes/base/jquery.ui.progressbar.css",
                        "~/Content/themes/base/jquery.ui.theme.css"));

            bundles.Add(new StyleBundle("~/Content/jqueryui-flick").Include(
                        "~/Content/themes/flick/jquery.ui.core.css",
                        "~/Content/themes/flick/jquery.ui.resizable.css",
                        "~/Content/themes/flick/jquery.ui.selectable.css",
                        "~/Content/themes/flick/jquery.ui.accordion.css",
                        "~/Content/themes/flick/jquery.ui.autocomplete.css",
                        "~/Content/themes/flick/jquery.ui.button.css",
                        "~/Content/themes/flick/jquery.ui.dialog.css",
                        "~/Content/themes/flick/jquery.ui.slider.css",
                        "~/Content/themes/flick/jquery.ui.tabs.css",
                        "~/Content/themes/flick/jquery.ui.datepicker.css",
                        "~/Content/themes/flick/jquery.ui.progressbar.css",
                        "~/Content/themes/flick/jquery.ui.theme.css"));

            // Bootstrap's
            bundles.Add(new StyleBundle("~/Content/bootstrap").Include(
                        "~/Content/bootstrap.css",
                        "~/Content/bootstrap-responsive.css"));
            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                        "~/Scripts/bootstrap.js"));

            // jTable
            bundles.Add(new StyleBundle("~/Content/jTable").Include(
                        "~/Scripts/jtable/themes/metro/lightgray/jtable.css"));
            bundles.Add(new ScriptBundle("~/bundles/jTable").Include(
                        "~/Scripts/jtable/jquery.jtable.js"));

            // form-validator
            bundles.Add(new StyleBundle("~/Content/formvalidator").Include(
                        "~/Content/form-validator.css"));
            bundles.Add(new ScriptBundle("~/bundles/formvalidator").Include(
                        "~/Scripts/form-validator/jquery.form-validator.js"));

            // Select2
            bundles.Add(new StyleBundle("~/Content/select2").Include(
                        "~/Content/css/select2.css"));
            bundles.Add(new ScriptBundle("~/bundles/select2").Include(
                        "~/Scripts/select2.js"));

            // multiselect
            //bundles.Add(new StyleBundle("~/Content/multiselect").Include(
            //            "~/Scripts/multiselect/css/multi-select.css"));
            //bundles.Add(new ScriptBundle("~/bundles/multiselect").Include(
            //            "~/Scripts/multiselect/js/jquery.multi-select.js"));

            // Highcharts
            bundles.Add(new ScriptBundle("~/bundles/highcharts").Include(
                        "~/Scripts/Highcharts-4.0.1/js/highcharts.js",
                        "~/Scripts/Highcharts-4.0.1/js/modules/exporting.js"));

            //Datatables
            //bundles.Add(new StyleBundle("~/Content/datatables").Include(
            //            "~/Content/DataTables/css/jquery.dataTables.css"));

            //bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
            //            "~/Scripts/DataTables/jquery.dataTables.js"));
        }
    }
}