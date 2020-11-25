using System.Web;
using System.Web.Optimization;

namespace Portal
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle(Links.Bundles.Styles.styles).Include(
                Links.Bundles.Content.Bootstrap.css.Assets.bootstrap_css, new CssRewriteUrlTransformFixed()).Include(
                Links.Bundles.Content.Animate.Assets.animate_css).Include(
                Links.Bundles.Content.Dynatree.skin_vista.Assets.ui_dynatree_css, new CssRewriteUrlTransformFixed()).Include(
                Links.Bundles.Content.JQueryUI.Assets.jquery_ui_css, new CssRewriteUrlTransformFixed()).Include(
                Links.Bundles.Content.Custom.css.Assets.site_css, new CssRewriteUrlTransformFixed()
            ));

            bundles.Add(new ScriptBundle(Links.Bundles.Scripts.preinit).Include(
                Links.Bundles.Content.JQuery.Assets.jquery_js,
                Links.Bundles.Content.Custom.js.Assets.preinit_js
            ));

            bundles.Add(new ScriptBundle(Links.Bundles.Scripts.scripts).Include(
                Links.Bundles.Content.JQueryUnobtrusive.Assets.jquery_unobtrusive_ajax_js,
                Links.Bundles.Content.Dynatree.Assets.jquery_dynatree_js,
                Links.Bundles.Content.Popper.Assets.popper_js,
                Links.Bundles.Content.Bootstrap.js.Assets.bootstrap_js,
                Links.Bundles.Content.BootstrapNotify.Assets.bootstrap_notify_js,
                Links.Bundles.Content.JQueryUI.Assets.jquery_ui_js,
                Links.Bundles.Content.Custom.js.Assets.utils_js
            ));
        }
    }
}
