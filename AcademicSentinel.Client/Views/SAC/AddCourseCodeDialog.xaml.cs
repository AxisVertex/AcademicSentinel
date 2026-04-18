using System.Windows;

namespace AcademicSentinel.Client.Views.SAC
{
    /// <summary>
    /// Dialog for entering a course code to enroll.
    /// </summary>
    public partial class AddCourseCodeDialog : Window
    {
        public string CourseCode { get; private set; }

        public AddCourseCodeDialog()
        {
            InitializeComponent();
            TxtCourseCode.Focus();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCourseCode.Text))
            {
                MessageBox.Show("Please enter a course code.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCourseCode.Focus();
                return;
            }

            CourseCode = TxtCourseCode.Text.Trim();
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
