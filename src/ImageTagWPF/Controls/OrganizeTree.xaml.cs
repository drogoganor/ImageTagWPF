using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ImageTagWPF.Code;

namespace ImageTagWPF.Controls
{
    public delegate void OrganizeTreeNodePickHandler(object item);
    public delegate void OrganizeTreeNodeMoveHandler();

    /// <summary>
    /// Interaction logic for OrganizeTree.xaml
    /// </summary>
    public partial class OrganizeTree : UserControl
    {
        public event OrganizeTreeNodePickHandler OnPickNode;
        public event OrganizeTreeNodeMoveHandler OnNodeMoveUp;
        public event OrganizeTreeNodeMoveHandler OnNodeMoveDown;
        public event OrganizeTreeNodeMoveHandler OnNodeMoveTop;
        public event OrganizeTreeNodeMoveHandler OnNodeMoveBottom;

        public OrganizeTree()
        {
            InitializeComponent();
		}


        public void Initialize()
        {
            var rootDirectory = App.ImageTag.Entities.OrganizeDirectories.Where(x => x.ParentDirectories.Count == 0).ToList();

            FileTree.ItemsSource = rootDirectory;
            
            FileTree.ExpandFirstItem();
        }

        private void FileTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            OnPickNode?.Invoke(FileTree.SelectedItem);
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            OnNodeMoveUp?.Invoke();
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            OnNodeMoveDown?.Invoke();
        }

        private void MoveToTopButton_Click(object sender, RoutedEventArgs e)
        {
            OnNodeMoveTop?.Invoke();
        }

        private void MoveToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            OnNodeMoveBottom?.Invoke();
        }
    }
}
