using System;
using System.Windows.Forms;

namespace BasicFacebookFeatures
{
    public delegate void ExitFormNotifyDelegate();

    public delegate void RememberMeNotifyDelegate();
    
    public partial class FormExit : Form
    {
        public bool ToExit { get; set; }

        public event ExitFormNotifyDelegate m_ExitFormNotifier;

        public event RememberMeNotifyDelegate m_RememberChangedNotifier;

        public FormExit()
        {
            InitializeComponent();
            ToExit = false;
        }

        public bool RememberMe
        {
            get
            {
                return checkBoxRememberMe.Checked;
            }

            set
            {
                checkBoxRememberMe.Checked = value;
            }
        }

        private void ButtonYes_Click(object sender, EventArgs e)
        {
            ToExit = true;
            OnbuttonYesClick();
            this.Close();
        }

        protected virtual void OnbuttonYesClick()
        {
            if (m_ExitFormNotifier != null)
            {
                m_ExitFormNotifier.Invoke();
            }
        }

        private void buttonNo_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkBoxRememberMe_CheckedChanged(object sender, EventArgs e)
        {
            OncheckBoxRememberMeClick();
        }

        protected virtual void OncheckBoxRememberMeClick()
        {
            if (m_RememberChangedNotifier != null)
            {
                m_RememberChangedNotifier.Invoke();
            }
        }
    }
}
