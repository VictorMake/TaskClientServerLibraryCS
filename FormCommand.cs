using System;
using System.Drawing;
using System.Windows.Forms;

namespace TaskClientServerLibrary
{
    public partial class FormCommand : Form
    {
        private ReaderWriterCommand parentReaderWriterCommand;

        public FormCommand(ReaderWriterCommand inReaderWriterCommandClass) : base()
        {
            // Этот вызов является обязательным для конструктора.
            InitializeComponent();

            // Добавить код инициализации после вызова InitializeComponent().
            parentReaderWriterCommand = inReaderWriterCommandClass;
        }

        private void FormCommand_Load(object sender, EventArgs e)
        {
            //RegistrationEventLog.EventLog_AUDIT_SUCCESS("Згрузка окна " & Me.Text)
            this.Text += " - " + parentReaderWriterCommand.Caption;
            PopulateTabPage();
        }

        private void PopulateTabPage()
        {
            int I = 0;

            foreach (Target itemTarget in parentReaderWriterCommand.ManagerAllTargets.Targets.Values)
            {
                TabPage mTabPageTargets = new TabPage();
                UserControlCommand mUserControlCommandTarget = itemTarget.GetUserControlCommandTarget();

                mTabPageTargets.SuspendLayout();
                TabControlCommandShassis.Controls.Add(mTabPageTargets);
                //
                //TabPageTargets
                //
                mTabPageTargets.Controls.Add(mUserControlCommandTarget);

                mTabPageTargets.BackColor = SystemColors.Control;
                mTabPageTargets.BorderStyle = BorderStyle.Fixed3D;
                mTabPageTargets.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, (byte)204);
                mTabPageTargets.ImageIndex = 1;
                mTabPageTargets.Location = new Point(4, 23);
                mTabPageTargets.Name = "TabPageTarget" + I;
                mTabPageTargets.Tag = I;
                mTabPageTargets.Padding = new Padding(3);
                mTabPageTargets.Size = new Size(770, 611);
                //mTabPage.TabIndex = 0
                mTabPageTargets.Text = itemTarget.HostName;
                mTabPageTargets.UseVisualStyleBackColor = true;
                //
                //UserControlCommandTarget
                //
                mUserControlCommandTarget.Dock = DockStyle.Fill;
                mUserControlCommandTarget.Location = new Point(3, 3);
                mUserControlCommandTarget.Name = "UserControlCommandTarget" + I;
                mUserControlCommandTarget.Size = new Size(764, 605);

                mTabPageTargets.ResumeLayout(false);
                I += 1;
            }
        }

        private void FormCommand_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (Target itemTarget in parentReaderWriterCommand.ManagerAllTargets.Targets.Values)
            {
                itemTarget.IsControlCommadVisible = false;
            }

            parentReaderWriterCommand.UcheckMenuCommandClientServer(false);
            parentReaderWriterCommand = null;
            //RegistrationEventLog.EventLog_AUDIT_SUCCESS("Закрытие окна " & Me.Text)
        }
    }
}
