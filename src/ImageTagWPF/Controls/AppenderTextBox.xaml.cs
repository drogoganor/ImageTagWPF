using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ImageTagWPF.Code;
using log4net;
using log4net.Appender;
using log4net.Core;

namespace ImageTagWPF.Controls
{
    /// <summary>
    /// Interaction logic for AppenderTextBox.xaml
    /// </summary>
    public partial class AppenderTextBox : UserControl, IAppender
    {
        public Level FilterLevel = Level.Debug;

        public AppenderTextBox()
        {
            InitializeComponent();

            var root = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
            var attachable = root as IAppenderAttachable;
            
            attachable.AddAppender(this);
        }

        public void Close()
        {
            
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            this.TextBlock.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new Action(
                    delegate
                    {
                        if (loggingEvent.Level <= FilterLevel)
                            return;

                        var color = Colors.Black;

                        if (loggingEvent.Level == Level.Warn)
                            color = Colors.Orange;
                        if (loggingEvent.Level == Level.Error)
                            color = Colors.Red;
                        if (loggingEvent.Level == Level.Debug)
                            color = Colors.DarkBlue;
                        
                        this.TextBlock.AppendText(loggingEvent.RenderedMessage + "\r", color);
                    }));
        }

        public void ScrollToEnd()
        {
            TextBlock.ScrollToEnd();
        }

        public void Clear()
        {
            TextBlock.Document.Blocks.Clear();
        }
    }
}
