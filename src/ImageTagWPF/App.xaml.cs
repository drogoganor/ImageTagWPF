using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ImageTagWPF.Code;
using log4net;
using log4net.Config;
using TinyIoC;

namespace ImageTagWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ImageTag ImageTag;

        public static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static TinyIoC.TinyIoCContainer Container;
        public static CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public App()
        {
            XmlConfigurator.Configure();

            Container = new TinyIoCContainer();
            Container.Register<ILog>(Log);

            ImageTag = new ImageTag();
            Container.Register<ImageTag>(ImageTag);
            ImageTag.Initialize();




        }


        public static bool CheckForProcessingItems()
        {
            if (ImageTag.DispatchQueue.Count > 0)
            {
                MessageBox.Show("Cannot run processing over items while a tagging operation is in progress.", "Cannot run", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            return false;
        }
    }
}
