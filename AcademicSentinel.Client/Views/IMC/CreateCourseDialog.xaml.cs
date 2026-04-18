using System.Windows;

namespace AcademicSentinel.Client.Views.IMC
{
    /// <summary>
    /// Interaction logic for CreateCourseDialog.xaml
    /// </summary>
    public partial class CreateCourseDialog : Window
    {
        public string CourseDescription { get; private set; }

        public CreateCourseDialog()
        {
            InitializeComponent();
            TxtCourseDescription.Focus();
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCourseDescription.Text))
            {
                MessageBox.Show(
                    "Please enter a course description.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            CourseDescription = TxtCourseDescription.Text.Trim();
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
