using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ozonesonde_Viewer_2019
{
    static class HelperMethods
    {
        public delegate void MessageDelegate(string str);

        //run a "fire and forget" task that doesn't need to be awaited
        public static void RunAsync(Task task, MessageDelegate showError)
        {
            //this will make sure any exceptions are caught and dealt with properly, even though we're not awaiting the task
            task.ContinueWith(t =>
            {
                //todo: log exception here?
                showError?.Invoke("Task Error: " + t.Exception.ToString());
                //System.Windows.Forms.MessageBox.Show(t.Exception.ToString(), "Task Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);

            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
