using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ozonesonde_Viewer_2019
{
    static class HelperMethods
    {
        //run a "fire and forget" task that doesn't need to be awaited
        public static void RunAsync(Task task)
        {
            task.ContinueWith(t =>
            {
                //todo: log exception here?
                System.Windows.Forms.MessageBox.Show(t.Exception.ToString(), "Task Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);

            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
