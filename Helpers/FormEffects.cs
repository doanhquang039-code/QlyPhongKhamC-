using System.Windows.Forms;

namespace QlyPhongKham.Helpers
{
    public static class FormEffects
    {
        public static void FadeIn(Form form)
        {
            form.Opacity = 0;
            System.Windows.Forms.Timer? timer = null;
            form.Shown += (_, _) =>
            {
                timer = new System.Windows.Forms.Timer { Interval = 15 };
                timer.Tick += (_, _) =>
                {
                    form.Opacity = Math.Min(1, form.Opacity + 0.12);
                    if (form.Opacity < 1)
                        return;

                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                };
                timer.Start();
            };
            form.FormClosed += (_, _) =>
            {
                if (timer is null)
                    return;

                timer.Stop();
                timer.Dispose();
                timer = null;
            };
        }
    }
}
