using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using static TaskClientServerLibrary.Clobal;

namespace TaskClientServerLibrary
{
    public partial class UserControlCommand : UserControl
    {
        private Target mTarget; // связанный поставщик для отображения его данных
        private readonly bool mIsServer;
        private bool isLoaded;

        public UserControlCommand()
        {
            InitializeComponent();
        }
        public UserControlCommand(Target inTarget) : base()
        {

            InitializeComponent();
            mTarget = inTarget;
            mIsServer = inTarget.ParrentManagerTargets.ParrentReaderWriterCommand.IsServer;

            if (mIsServer)
            {
                SetToolTip(mTarget.HostName);
            }
            else
            {
                SetToolTip(SERVER);
            }
        }
        private void UserControlCommand_Load(object sender, EventArgs e)
        {
            InitializeDataSource();

            TextBoxPipeServerStatus.Text = mTarget.PipeServerStatus;
            TextBoxURLSend.Text = mTarget.URLSendLocation;
            TextBoxURLReceive.Text = mTarget.URLReceiveLocation;
            TextBoxError.Text = mTarget.ErrorCommand;
            TimeStampTextBox.Text = mTarget.TimeStampTarget;
            SendTextBox.Text = mTarget.SendText;
            ReceiveTextBox.Text = ConvertStringToXML(mTarget.CommandValueReader);

            InitializeListView(ref ListTaskSend);
            InitializeListView(ref ListTaskReceive);
            RefreshListCommandsReceive();
            RefreshListCommandsSend();

            //наполнить ComboBoxTasks и назначить обработчик
            ComboBoxTasks.Items.AddRange(mTarget.ConfigSend.ManagerTasks.Tasks.Values.ToArray());
            RadioButtonIsServer.Checked = mIsServer;
            ButtonListen.Enabled = false;
            isLoaded = true;
        }

        public void Close()
        {
            mTarget = null;
        }

        private void SetToolTip(string toReceive)
        {
            ToolTip1.SetToolTip(ComboBoxTasks, $"Выбор команды для редактирования и отправки на {toReceive}");
            ToolTip1.SetToolTip(TextBoxURLSend, $"Сетевой адрес контейнера команды посылаемой на {toReceive}");
            ToolTip1.SetToolTip(TextBoxURLReceive, $"Сетевой адрес контейнера команды пришедшей от {toReceive}");

            ToolTip1.SetToolTip(TextBoxPipeServerStatus, $"Статус контейнера для команды посылаемой на {toReceive}");
            ToolTip1.SetToolTip(SendTextBox, $"Текст команды посылаемой на {toReceive} в формате XML");
            ToolTip1.SetToolTip(ListTaskSend, $"Список всех команд посылаемых на {toReceive} в текущем сеансе работы");
            ToolTip1.SetToolTip(DataGridSend, $"Значения параметров команды посылаемой {toReceive}");
            ToolTip1.SetToolTip(ButtonSend, $"Послать {toReceive} выбранную команду для исполнения");
            ToolTip1.SetToolTip(TimeStampTextBox, $"Время получения команды от {toReceive}");

            ToolTip1.SetToolTip(ReceiveTextBox, $"Текст команды пришедшей от {toReceive} в формате XML");
            ToolTip1.SetToolTip(DataGridReceive, $"Значения параметров команды принятой от {toReceive}");
            ToolTip1.SetToolTip(ListTaskReceive, $"Список всех пришедших команд от {toReceive} в текущем сеансе работы");
            ToolTip1.SetToolTip(TextBoxError, $"Текст ошибки соединения с {toReceive} по сети");
        }

        /// <summary>
        /// Преобразование линейной строки команды XML в форматированную строку
        /// </summary>
        /// <param name="inString"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string ConvertStringToXML(string inString)
        {
            if (inString == null || inString == COMMAND_NOTHING)
            {
                return COMMAND_NOTHING;
            }

            TextReader tr = new StringReader(inString);
            XDocument doc = XDocument.Load(tr);
            StringBuilder sb = new StringBuilder();

            using (var sr1 = new StringWriter(sb))
            {
                doc.Save(sr1, SaveOptions.None);
                return sb.ToString();
            }
        }

        #region DataGrid
        /// <summary>
        /// Связать таблицы контрола с поставщиками данных
        /// </summary>
        /// <remarks></remarks>
        private void InitializeDataSource()
        {
            DataGridSend.DataSource = mTarget.ConfigSend.GetDataTable();
            DataGridReceive.DataSource = mTarget.ConfigReceive.GetDataTable();
            SetDataGridSendColumnsSizeMode();
            SetDataGridReceiveColumnsSizeMode();
        }

        /// <summary>
        /// Настройка внешнего вида таблицы
        /// </summary>
        /// <remarks></remarks>
        private void SetDataGridSendColumnsSizeMode()
        {
            for (int I = 0; I < DataGridSend.Columns.Count; I++)
            {
                DataGridSend.Columns[I].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                DataGridSend.Columns[I].SortMode = DataGridViewColumnSortMode.NotSortable;
                DataGridSend.Columns[I].ReadOnly = true;
            }
        }

        /// <summary>
        /// Настройка внешнего вида таблицы
        /// </summary>
        /// <remarks></remarks>
        private void SetDataGridReceiveColumnsSizeMode()
        {
            for (int I = 0; I < DataGridReceive.Columns.Count; I++)
            {
                DataGridReceive.Columns[I].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                DataGridReceive.Columns[I].SortMode = DataGridViewColumnSortMode.NotSortable;
                DataGridReceive.Columns[I].ReadOnly = true;
            }
        }

        /// <summary>
        /// Настройка внешнего вида листа пришедших и отправляемых команд
        /// </summary>
        /// <param name="lView"></param>
        /// <remarks></remarks>
        private void InitializeListView(ref ListView lView)
        {
            int width = lView.Width;

            lView.Items.Clear();
            lView.Columns.Clear();
            lView.Columns.Add(ID_COMMAND_LV, ID_COMMAND_LV, Convert.ToInt32(width * 0.5 / 4) - 2, HorizontalAlignment.Left, 0);
            lView.Columns.Add(COMMAND_DESCRIPTION_LV, COMMAND_DESCRIPTION_LV, Convert.ToInt32(width * 2 / 4.0) - 2, HorizontalAlignment.Left, 0);
            lView.Columns.Add(COMMANDER_ID_LV, COMMANDER_ID_LV, Convert.ToInt32(width * 0.5 / 4) - 2, HorizontalAlignment.Left, 0);
            lView.Columns.Add(INDEX_COMMAND_LV, INDEX_COMMAND_LV, Convert.ToInt32(width * 1 / 4.0) - 2, HorizontalAlignment.Left, 0);
        }

        private void UserControlCommandShassis_Resize(object sender, EventArgs e)
        {
            if (isLoaded)
            {
                ResizeListView(ref ListTaskSend);
                ResizeListView(ref ListTaskReceive);
            }
        }

        private void ResizeListView(ref ListView List)
        {
            int listViewWidth = List.Width;

            List.Columns[ID_COMMAND_LV].Width = Convert.ToInt32(listViewWidth * 0.5 / 4) - 2;
            List.Columns[COMMAND_DESCRIPTION_LV].Width = Convert.ToInt32(listViewWidth * 2 / 4.0) - 2;
            List.Columns[COMMANDER_ID_LV].Width = Convert.ToInt32(listViewWidth * 0.5 / 4) - 2;
            List.Columns[INDEX_COMMAND_LV].Width = Convert.ToInt32(listViewWidth * 1 / 4.0) - 2;
        }
        #endregion

        /// <summary>
        /// Нужно в какой-то таблице установить свойства выделенного из списка и клонорованной задачи, 
        /// а затем ее послать на выполнениею
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSend_Click(object sender, EventArgs e)
        {
            SendQueryByHand();
        }

        /// <summary>
        /// После установки желаемых значений параметров команды
        /// запись их в класс XMLConfigSend, а затем посылка на выполнение
        /// </summary>
        /// <remarks></remarks>
        private void SendQueryByHand()
        {
            if (ComboBoxTasks.SelectedIndex != -1)
            {
                //Dim runingTask As ManagerTaskApplication.TaskApplication = CType(ComboBoxTasks.SelectedItem, ManagerTaskApplication.TaskApplication).Clone
                ManagerTaskApplication.TaskApplication runingTask = (ManagerTaskApplication.TaskApplication)(((ManagerTaskApplication.TaskApplication)ComboBoxTasks.SelectedItem).Clone());

                mTarget.ConfigSend.Clear();
                mTarget.ConfigSend.AddRow(COMMAND_NAME, runingTask.Name, TypeParam.String);
                mTarget.ConfigSend.AddRow(COMMAND_DESCRIPTION, runingTask.Description, TypeParam.String);

                if (runingTask.Parameters.Values.Count > 0)
                {
                    dgvParameters.Rows[0].Cells[0].Selected = true;
                    foreach (ManagerTaskApplication.TaskApplication.Parameter itemParameter in runingTask.Parameters.Values)
                    {
                        //.AddRow($"{itemParameter.Number.ToString} {COMMAND_PARAMETER}",
                        //         dgvParameters.Rows(itemParameter.Number - 1).Cells(1).Value, itemParameter.Type)
                        mTarget.ConfigSend.AddRow($"{itemParameter.Number} {COMMAND_PARAMETER}", Convert.ToString(dgvParameters.Rows[itemParameter.Number - 1].Cells[1].Value), itemParameter.Type);
                    }
                }

                //Dim _random As Random = New Random(Convert.ToInt32((DateTime.Now.Millisecond >> 32)))
                mTarget.ConfigSend.AddRow(INDEX, Convert.ToInt32(RandomProvider.GetThreadRandom().NextDouble() * (Math.Pow(2, 31))).ToString(), TypeParam.String);
                mTarget.ConfigSend.AddRow(COMMAND_COMMANDER_ID, $"Компьютер:<{System.Environment.MachineName}> соединение:<{mTarget.HostName}> канал:<{mTarget.NamePiepSend}>", TypeParam.String);

                mTarget.ParrentManagerTargets.ParrentReaderWriterCommand.SendXMLCommand(mTarget);
            }
        }

        /// <summary>
        /// Выделенная из списка задача клонируется.
        /// Выведенные в таблице строки параметров позволяют установить желаемые значения.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks></remarks>
        private void ComboBoxTasks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboBoxTasks.SelectedIndex != -1)
            {
                ManagerTaskApplication.TaskApplication selectedTask = (ManagerTaskApplication.TaskApplication)ComboBoxTasks.SelectedItem;
                ButtonSend.Select();
                dgvParameters.Rows.Clear();

                if (selectedTask.Parameters.Values.Count > 0)
                {
                    dgvParameters.Rows.Add(selectedTask.Parameters.Count);

                    foreach (ManagerTaskApplication.TaskApplication.Parameter itemParameter in selectedTask.Parameters.Values)
                    {
                        dgvParameters.Rows[itemParameter.Number - 1].Cells[0].Value = (object)itemParameter.Number;
                        dgvParameters.Rows[itemParameter.Number - 1].Cells[1].Value = (object)itemParameter.Value;
                        dgvParameters.Rows[itemParameter.Number - 1].Cells[2].Value = (object)itemParameter.Type;
                        dgvParameters.Rows[itemParameter.Number - 1].Cells[3].Value = (object)itemParameter.Description;
                    }
                }
            }
        }

        /// <summary>
        /// В интерактивном режиме обновление из сервисного класса mReaderWriterCommandClass.
        /// </summary>
        /// <remarks></remarks>
        public void UpdateDataGridReceive()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => UpdateDataGridReceive()));
            }
            else
            {
                DataGridReceive.DataSource = mTarget.ConfigReceive.GetDataTable();
                SetDataGridReceiveColumnsSizeMode();
            }
        }

        /// <summary>
        /// В интерактивном режиме обновление из сервисного класса mReaderWriterCommandClass.
        /// </summary>
        /// <remarks></remarks>
        public void UpdateDataGridSend()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => UpdateDataGridSend()));
            }
            else
            {
                DataGridSend.DataSource = mTarget.ConfigSend.GetDataTable();
                SetDataGridSendColumnsSizeMode();
            }
        }

        /// <summary>
        /// Сбросить таблицу
        /// </summary>
        public void SetNothigDataSourceForDataGridSend()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => SetNothigDataSourceForDataGridSend()));
            }
            else
            {
                DataGridSend.DataSource = null;
            }
        }

        /// <summary>
        /// В интерактивном режиме обновление из сервисного класса mReaderWriterCommandClass.
        /// При загрузке контрола восстановить кешированные данные.
        /// </summary>
        /// <remarks></remarks>
        public void RefreshListCommandsReceive()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => RefreshListCommandsReceive()));
            }
            else
            {
                ListViewItem itmX = null;

                ListTaskReceive.Items.Clear();

                foreach (CommandForListViewItem itemCommand in mTarget.ListCommandsReceive)
                {
                    itmX = new ListViewItem(itemCommand.IDCommamd) { ForeColor = itemCommand.Color };
                    itmX.SubItems.Add(itemCommand.Description);
                    itmX.SubItems.Add(itemCommand.CommanderID);
                    itmX.SubItems.Add(itemCommand.IndexCommamd);
                    ListTaskReceive.Items.Add(itmX);
                    itmX.Selected = true;
                    itmX.EnsureVisible();
                }
            }
        }

        /// <summary>
        /// В интерактивном режиме обновление из сервисного класса mReaderWriterCommandClass.
        /// При загрузке контрола восстановить кешированные данные.
        /// </summary>
        /// <remarks></remarks>
        public void RefreshListCommandsSend()
        {
            if (InvokeRequired)
            {
                //Если вызов не из UI thread, продолжить рекурсивный вызов,
                //пока не достигнут UI thread
                //Invoke(New EventHandler(Of EventArgs)(AddressOf _data_LoadStarted), sender, e)
                //Invoke(New MethodInvoker(Sub() UpdateStatus(status, percent)))

                Invoke(new MethodInvoker(() => RefreshListCommandsSend()));
            }
            else
            {
                //textBoxLog.AppendText("Load started" & Environment.NewLine)
                ListTaskSend.Items.Clear();

                foreach (CommandForListViewItem itemCommand in mTarget.ListCommandsSend)
                {
                    ListViewItem itmX = new ListViewItem(itemCommand.IDCommamd) { ForeColor = itemCommand.Color };
                    itmX.SubItems.Add(itemCommand.Description);
                    itmX.SubItems.Add(itemCommand.CommanderID);
                    itmX.SubItems.Add(itemCommand.IndexCommamd);
                    ListTaskSend.Items.Add(itmX);
                    itmX.Selected = true;
                    itmX.EnsureVisible();
                }
            }
        }

        public void UpdateSendTextBox(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => UpdateSendTextBox(text)));
            }
            else
            {
                SendTextBox.Text = text;
            }
        }

        public void UpdateStatusCommandPipeServer(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => UpdateStatusCommandPipeServer(text)));
            }
            else
            {
                TextBoxPipeServerStatus.Text = text;
            }
        }

        public void UpdateTimeStamp(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => UpdateTimeStamp(text)));
            }
            else
            {
                TimeStampTextBox.Text = text;
            }
        }

        public void UpdateError(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => UpdateError(text)));
            }
            else
            {
                TextBoxError.Text = text;
            }
        }

        public void SetButtonSendEnabled(bool isEnabled)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => SetButtonSendEnabled(isEnabled)));
            }
            else
            {
                ButtonSend.Enabled = isEnabled;
            }
        }

        //Public Sub Update(text As String)
        //    If InvokeRequired Then
        //        Invoke(New MethodInvoker(Sub() (text)))
        //    Else
        //        .Text = text
        //    End If
        //End Sub

        //'обработчик события главной формы, вызываемый из другого потока
        //Private Sub _data_LoadStarted(sender As Object, e As EventArgs)
        //    If InvokeRequired Then
        //        'Если вызов не из UI thread, продолжить рекурсивный вызов,
        //        'пока не достигнут UI thread
        //        Invoke(New EventHandler(Of EventArgs)(AddressOf _data_LoadStarted), sender, e)
        //    Else
        //        'textBoxLog.AppendText("Load started" & Environment.NewLine)
        //    End If
        //End Sub

    }
}
