using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ImageTagWPF.Code
{
    public class ImageTagDispatchItem
    {
        public delegate void ImageTagDispatchItemHandler(ImageTagDispatchItem self);

        public event ImageTagDispatchItemHandler OnFinish;

        public Task Task { get; set; }
        public Action Action { get; set; }
        public string Description { get; set; }
        public CancellationToken Token { get; set; }
        public bool IsCancelable { get; set; }
        public bool IsRunning { get; set; }

        public Dispatcher Dispatcher { get; set; }

        public ImageTagDispatchItem()
        {
            IsCancelable = true;
        }

        public void Finish()
        {
            OnFinish?.Invoke(this);
        }
    }
}
