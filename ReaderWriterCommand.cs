using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows.Forms;
using static TaskClientServerLibrary.Clobal;

// 1. Созданы отдельные асинхронные события "обмена командами".
// На сосновании количества подключённых target создаются 2 по два сетевых каналов типа Read и Write
// для двухстороннего обмена командами XML с индивидуальной упаковкой.
// На события каналов созданы подписки с уведомлением. 
// В случае прихода новой команды вызвается событие приёма команды в подписчике.
// Каналы созданы в ассинхронной манере с уведомлением о результате операции для обработки ошибок.
// Каждому target присваивается класс парсер работы с XML командами (XMLConfigCommand), а им в свою
// очередь присваиваются классы менеджеров команд (ManagerTaskApplication), которые работают только с записанными 
// в файлы "TasksClientServer.xml" шаблонами (сигнатурами) разрешённых команд.

// 2. Подписчик ReaderSubscriber target по свойству LastCommand (кешировать и анализировать на повтор) определяет новую пришедшую команду
// и в случае прихода новой команды она расшифровывается и исполняется. 

// В отличии от команды посылаемой вручную путём выбора из списка и заполнения атрибутов в таблице вручную, 
// программная команда заносится в очередь target.CommandWriterQueue и событии таймера извлекается и посылается на target.
// По таймеру OnTimedEvent 1 сек производится запись команд на исполнение для всех target,
// участвующих в работе из очереди команд (в случае автоматической генерации команд) и вызывается в методе RunTask(TaskReceive).
// При получении <ответа> от target ищется совпадение ИндексКоманды и изменяется цвет отправленной команды в листе отправленных команд.
// Через промежуток времени 10 сек производится запись Nothing, чтобы target не тратило время на долгий разбор команды для анализа, а сразу выходило из цикла.

// 3. Результаты ответов от target или принятые команды отображаются в сообщениях на вкладке окна FormCommand, а при загрузке окна "Обмена командами"
// принятые команды отображаются и там.
// target имеет поля кеширующие все свойства контрола и истории вызова команд,
// UserControlCommand при его отображении на закладке формы, элементы контрола заполняются из этих свойств
// при загрузке формы обмена командами истории всех вызовов восстанавливалась в листах контрола свзанного с target.
// Контрол служит только интерфейсом отображения и командным вызовом методов target.

// 4. На target:
// При получении на target анализирует поля <Name>Command ID</Name> и поля <Name>Commander ID</Name> на повторы для определения новой команды
// Command ID для команд подверждения берётся из пришедших команд от Сервера.
// Для вновь вводимых команд Command ID генерируется программно.

// <Task Name="Сообщение" Description="Послать сообщение на другой компьютер" ProcedureName="Сообщение" WhatModule="FormMain" Index="123">
//     <Parameter Key="1" Value="0" Type="String" Description="Текст посылаемого сообщеня" />
// </Task>
// Можно в файле TasksClientServer.xml описать любую задачу которая вызывает открытую процедуру
// в основной форме FormMain или в основной форме frmBaseKT.vb
// процедуры или должны быть или их надо добавить

/// <summary>
/// Сервисный класс обслуживания командного обмена между target
/// </summary>
/// <remarks></remarks>
namespace TaskClientServerLibrary
{
    public class ReaderWriterCommand
    {
        /// <summary>
        /// Управляющий класс коллекцией компьютеров на стенде 
        /// </summary>
        /// <returns></returns>
        public ManagerTargets ManagerAllTargets { get; }
        /// <summary>
        /// Имя удаленного компьютера, к которому нужно подключиться, или значение ".", чтобы указать локальный компьютер. 
        /// </summary>
        public string ServerName { get; set; } = "."; // "DESKTOP-6SPCMGA"

        /// <summary>
        /// Метод в основном окне для вывода сообщения
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public Action<string, int, MessageBoxIcon> AppendMethod { get; set; }

        public bool IsServer { get; } = false;
        private const int LIM_COUNT_TIMED_EVENT = 10; // через 10 сек. пошлётся всем target команда Nothing
        private int counterTimedEvent; // счётчик 10 секунд для посылки команды Nothing

        private const int TIMER_INTERVAL = 1000;
        private readonly System.Timers.Timer aTimer;

        // управление коллекцией разрешённых задач
        //Private ReadOnly mTasksReceiveManager As ManagerTaskApplication' если задачи на target отличаются, например на cRio
        private readonly ManagerTaskApplication mTasksSendManager;
        /// <summary>
        /// вызывающая основная форма, в которая содержит вызываемые командой продедуры
        /// </summary>
        private readonly Form mParentForm;
        /// <summary>
        /// Отображение формы производится из меню вызывающей основной формы
        /// </summary>
        public FormCommand FormCommander;
        /// <summary>
        /// заголовок окна клиентской вкладки
        /// </summary>
        public string Caption { get; }
        /// <summary>
        /// Число клиентов для регистратора запущенного как Сервер или
        /// номер клиента для регистратора запущенного как Клиент.
        /// </summary>
        /// <returns></returns>
        public int CountClientOrNumberClient { get; }

        //INSTANT C# TODO TASK: This method is a constructor, but no class name was found:
        public ReaderWriterCommand(Form inParentForm,
                                   string inPathРесурсы,
                                   bool inIsServer,
                                   int inCountClientOrNumberClient,
                                   string inServerWorkingFolder,
                                   string inClientWorkingFolder,
                                   Action<string, int, MessageBoxIcon> inAction)
        {
            mParentForm = inParentForm;
            IsServer = inIsServer;
            CountClientOrNumberClient = inCountClientOrNumberClient;

            if (IsServer)
            {
                // образец: "\\006-stend21\System\doublearray" ' "\\localhost\system\doublearray"
                //sendAddressURL = "\\localhost\system\task1" ' было "dstp://localhost/task1"
                ServerName = GetReceiveAddressURL(inClientWorkingFolder); // Клиент Рабочий Каталог
                Caption = SERVER;
            }
            else // клиент' просмотр снимка
            {
                //sendAddressURL = "\\localhost\system\task2"
                // только для случая при запуске просмотра снимков
                if (inServerWorkingFolder == null)
                {
                    inServerWorkingFolder = inClientWorkingFolder;
                }

                ServerName = GetReceiveAddressURL(inServerWorkingFolder); // Server Рабочий Каталог
                Caption = "Клиент";
            }

            // надо считать из конфигурационного файла или ещё как-то создать
            ManagerAllTargets = new ManagerTargets(this);

            if (ManagerAllTargets.LoadTargets() && ManagerAllTargets.Count == 0)
            {
                ManagerAllTargets = null;
                MessageBox.Show("Отсутствуют Клиент-Серверные задачи.", "Создание задач Клиент-Серверного обмена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //mTasksReceiveManager = New ManagerTaskApplication(inPathРесурсы)
            mTasksSendManager = new ManagerTaskApplication(inPathРесурсы);

            AppendMethod = inAction;
            CreateCollectionReceiverSender();
            AddHandlerDataUpdatedWriteCompleted();
            AddHandlerDataUpdatedWriteCompleted();

            aTimer = new System.Timers.Timer(TIMER_INTERVAL);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.Enabled = true;

            //For Each itemTarget As Target In ManagerAllTargets.Targets.Values
            //    itemTarget.RunListenAsync()
            //    ' послать на очистку после подключения какого-либо клиента
            //    SendRequestProgrammed(itemTarget, "Очистка линии")
            //Next
        }

        /// <summary>
        /// 1 этап Close
        /// </summary>
        public void Close()
        {
            aTimer.Enabled = false;

            try
            {
                if (FormCommander != null)
                {
                    FormCommander.Close();
                }

                foreach (Target itemTarget in ManagerAllTargets.Targets.Values)
                {
                    //Await itemTarget.WriteStopAsync
                    //Await Task.Delay(100)
                    itemTarget.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: {0}", ex.ToString());
                //Await Task.Run(Function() Console.WriteLine("ERROR: {0}", ex.ToString))
            }
        }

        private string GetReceiveAddressURL(string workingFolder)
        {
            if (workingFolder.IndexOf("\\\\") != -1) // клиент на другом компьютере \\Stend_NN\c\Registration\Store\Channels.mdb
            {
                // вырезать имя компьютера
                return $"{workingFolder.Substring(2, workingFolder.IndexOf("\\", 2) - 2)}";
            }
            else // клиент на локальном компьютере D:\ПрограммыVBNET\Регистратор.NET\bin\Ресурсы\Channels.mdb
            {
                return ".";
            }
        }

        public void ShowFormCommand()
        {
            FormCommander = new FormCommand(this);
            FormCommander.Show();
            FormCommander.Activate();
        }

        public void HideFormCommand()
        {
            FormCommander.Close();
        }

        #region Сетевые переменные контейнеры команд обмена
        /// <summary>
        /// Создать Коллекции Читателей Писателей.
        /// Коллекция Read содержит имена сетевых переменных строкового типа Location: \\IP Address target\RT Variables\Invoke_SSDVariable.
        /// Коллекция Write содержит имена сетевых переменных строкового типа Location: \\IP Address target\RT Variables\Invoke_cRIO.
        /// </summary>
        /// <remarks></remarks>
        private void CreateCollectionReceiverSender()
        {
            foreach (Target itemTarget in ManagerAllTargets.Targets.Values)
            {
                // т.к. задачи исполнения процедур в модулях(формах) программы одинаковы для исходящих и входящих команд, то из манеджеры одинаковы
                itemTarget.ConfigSend.ManagerTasks = mTasksSendManager;
                itemTarget.ConfigReceive.ManagerTasks = mTasksSendManager; // mTasksReceiveManager
            }
        }

        /// <summary>
        /// Подписаться на уведомление получения команды от Target.
        /// Присвоить делегат окончания отправки команды для Target.
        /// </summary>
        private void AddHandlerDataUpdatedWriteCompleted()
        {
            // подписаться на события уведомления
            foreach (Target itemTarget in ManagerAllTargets.ListTargets)
            {
                itemTarget.DataUpdated += OnSubscriber_DataUpdated;
                itemTarget.WriteCompleted += Writer_WriteCompleted;
            }
        }

        /// <summary>
        /// Делегат завершения асинхронной операции записи
        /// одинаков для типов NetworkVariableWriter(Of String) и NetworkVariableWriter(Of Boolean)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks></remarks>
        private void Writer_WriteCompleted(object sender, WriteCompletedEventArgs e)
        {
            Target targetWriter = (Target)e.UserState;
            string errorText = string.Empty;

            if (e.Error != null)
            {
                // обработать ошибку      
                errorText = Convert.ToString(e.Error);
                AppendMethod.Invoke($"Ошибка при записи для: {targetWriter.URLSendLocation}{Environment.NewLine}{errorText}",
                    targetWriter.IndexTab, MessageBoxIcon.Error);
            }

            if (targetWriter.IsControlCommadVisible)
            {
                targetWriter.UserControlCommandTarget.UpdateError(errorText);
            }
        }

        /// <summary>
        /// Отслеживание события изменения данных
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks></remarks>
        private void OnSubscriber_DataUpdated(object sender, DataUpdatedEventArgs<string> e)
        {
            string messageReceive = null;
            Target targetReader = null;

            try
            {
                //targetReader = ManagerAllTargets.GetTargetReaderFromURL(CType(sender, Target).URLSendLocation)
                targetReader = (Target)sender;

                if (targetReader != null)
                {
                    targetReader.TimeStampTarget = e.Data.TimeStamp.ToLocalTime().ToString();
                    targetReader.PipeServerStatus = e.Data.Quality;
                    messageReceive = e.Data.GetValue(); // содержат все сведения о данных

                    // получить только отличающиеся от старых данные
                    if (string.IsNullOrEmpty(messageReceive) || messageReceive == COMMAND_NOTHING) { return; }

                    if (targetReader.LastCommand == e.Data.GetValue())
                    {
                        return;
                    }
                    else
                    {
                        // обновить поля target
                        targetReader.CommandValueReader = e.Data.GetValue();
                        targetReader.LastCommand = e.Data.GetValue();
                        targetReader.ConfigReceive.LoadXMLfromString(messageReceive);
                        PopulateReader(targetReader);
                        RunTask(targetReader);
                    }
                }
            }
            catch (TimeoutException ex)
            {
                const string CAPTION = "Проблема с сетью:";
                string text = $"Получение данных коммандного обмена в <{nameof(OnSubscriber_DataUpdated)}>: ";
                if (targetReader != null)
                {
                    text += targetReader.URLReceiveLocation;
                    AppendMethod.Invoke(text + Environment.NewLine + Convert.ToString(ex), KEY_RICH_TEXT_SERVER, MessageBoxIcon.Error);
                }
                MessageBox.Show(text, CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //RegistrationEventLog.EventLog_MSG_APPLICATION_MESSAGE(String.Format("<{0}> {1}", CAPTION, text))
            }
        }

        /// <summary>
        /// Заполнить лист и таблицу Слушателя
        /// </summary>
        private void PopulateReader(Target targetReader)
        {
            string strIndex = targetReader.ConfigReceive.GetRowValue(INDEX)[0];

            // поиск в листе посылки для отметки команды, требующей подтверждения
            foreach (CommandForListViewItem itemCommand in targetReader.ListCommandsSend)
            {
                if (itemCommand.IndexCommamd == strIndex)
                {
                    itemCommand.Color = Color.Green;
                    // команда Найдена - пришел ответ о выполнении задачи, ее надо снять
                    if (targetReader.IsControlCommadVisible)
                    {
                        targetReader.UserControlCommandTarget.RefreshListCommandsSend();
                    }
                    break;
                }
            }

            // пришел запрос поставить задачу на выполнение
            targetReader.ListCommandsReceive.Add(new CommandForListViewItem(
                targetReader.ConfigReceive.GetRowValue(COMMAND_NAME)[0],
                targetReader.ConfigReceive.GetRowValue(COMMAND_DESCRIPTION)[0],
                targetReader.ConfigReceive.GetRowValue(COMMAND_COMMANDER_ID)[0],
                targetReader.ConfigReceive.GetRowValue(INDEX)[0]));

            if (targetReader.IsControlCommadVisible)
            {
                targetReader.UserControlCommandTarget.UpdateDataGridReceive();
                targetReader.UserControlCommandTarget.UpdateTimeStamp(targetReader.TimeStampTarget);
                targetReader.UserControlCommandTarget.UpdateStatusCommandPipeServer(targetReader.PipeServerStatus);
            }

            AppendMethod.Invoke($"Получена команда: {targetReader.ConfigReceive.GetRowValue(COMMAND_DESCRIPTION)[0]} индекс: {targetReader.ConfigReceive.GetRowValue(INDEX)[0]}{Environment.NewLine} от компьютера: {targetReader.HostName}",
                KEY_RICH_TEXT_SERVER, MessageBoxIcon.Information);

            //RegistrationEventLog.EventLog_AUDIT_SUCCESS($"Получена команда {readValue} от компьютера: {targetReader.HostName}")
        }

        /// <summary>
        /// Запуск метода содержащегося в атрибутах RuningTask 
        /// </summary>
        /// <param name="inReader"></param>
        private void RunTask(Target inReader)
        {
            // Получить Type и MethodInfo
            // Diagnostics.Process.GetCurrentProcess.ProcessName дает в среде строку "Registration.vshost"
            // надо очистить
            //Dim MyType As Type = Type.GetType(Diagnostics.Process.GetCurrentProcess.ProcessName & "." & RuningTask.WhatModule) '("Registration.frmMain") 
            try
            {
                //Dim processName As String = Process.GetCurrentProcess.ProcessName
                //processName = Left(processName, If(InStr(1, processName, ".") = 0, Len(processName), InStr(1, processName, ".") - 1))
                //Dim MyType As Type = Type.GetType($"{processName}.{RuningTask.WhatModule}") '("Registration.frmMain") "Registration.FormSnapshotViewingDiagram"
                //Dim Mymethodinfo As MethodInfo = MyType.GetMethod(RuningTask.ProcedureName)

                //'RegistrationEventLog.EventLog_AUDIT_SUCCESS("RunTask " & RuningTask.ProcedureName)
                //'Dim parameters As Object() = {True, Модуль1.enuЗапросы.enuПоставитьМеткуКТ, "Пример"} 'ConfigReceive.GetValue(conПараметр)(0)}

                //If RuningTask.Parameters.Values.Count > 0 Then
                //    Dim parameters(RuningTask.Parameters.Values.Count - 1) As Object

                //    For Each itemParameter As TasksReceiveManager.TaskApplication.Parameter In RuningTask.Parameters.Values
                //        parameters(itemParameter.Number - 1) = Convert.ChangeType(itemParameter.Value, Type.GetType("System." & itemParameter.Type))
                //    Next

                //    Mymethodinfo.Invoke(mParentForm, parameters)
                //Else
                //    Mymethodinfo.Invoke(mParentForm, Nothing)
                //End If

                string hostName = inReader.HostName;
                string indexResponse = inReader.ConfigReceive.GetRowValue(INDEX)[0];
                ManagerTaskApplication.TaskApplication runingTask = inReader.ConfigReceive.GetTask(); // десериализация задачи с реальными параметрами
                Type myType2 = mParentForm.GetType();
                MethodInfo myMethodinfo2 = myType2.GetMethod(runingTask.ProcedureName);

                if (runingTask.Parameters.Values.Count > 0)
                {
                    object[] parameters = new object[runingTask.Parameters.Values.Count + 1];

                    foreach (ManagerTaskApplication.TaskApplication.Parameter itemParameter in runingTask.Parameters.Values)
                    {
                        parameters[itemParameter.Number - 1] = Convert.ChangeType(itemParameter.Value, Type.GetType("System." + itemParameter.Type.ToString()));
                    }

                    parameters[runingTask.Parameters.Values.Count] = new string[] { hostName, indexResponse };
                    myMethodinfo2.Invoke(mParentForm, parameters);
                }
                else
                {
                    //Dim parameters As Object() = {inReader.HostName}
                    myMethodinfo2.Invoke(mParentForm, new[] { new string[] { hostName, indexResponse } });
                }
            }
            catch (Exception ex)
            {
                string caption = $"Процедура <{nameof(RunTask)}>";
                string text = ex.ToString();
                MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //RegistrationEventLog.EventLog_MSG_EXCEPTION($"<{CAPTION}> {text}")
            }
        }
        #endregion

        #region Послать Команду
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            SendCommandOutOfQueueFromAllTargets();
            counterTimedEvent += 1;
        }

        //Private syncPointDoMonitor As Integer = 0 'для синхронизации

        /// <summary>
        /// Отправить команды на все Targets.
        /// Вызыватся из одного обработчика таймера,
        /// если будут вызываться из разных потоков,
        /// то могут мешать друг другу, поэтому следует применять очередь команд в каждом target
        /// </summary>
        /// <remarks></remarks>
        private void SendCommandOutOfQueueFromAllTargets()
        {
            // не стал применять синхронизацию (syncPointDoMonitor), т.к. при выводе окна из родительской формы события таймера теряются
            //<MethodImplAttribute(MethodImplOptions.Synchronized)>

            foreach (Target itemTarget in ManagerAllTargets.ListTargets)
            {
                // '' Должна быть очередь команд для каждого компьютера. Какой-то сервис кладёт туда команду, а здесь происходит извлечение
                // ''--- Тест ---------------------------------------------------------
                //Dim random As Random = New Random(Convert.ToInt32((DateTime.Now.Millisecond >> 32)))
                //itemTarget.CommandWriterQueue.Enqueue(New NetCommandForTask("Clear Polynomial Channel",
                //                                                                  New String() {((random.NextDouble) * 100).ToString,
                //                                                                                "2",
                //                                                                                "3",
                //                                                                                "4",
                //                                                                                "5",
                //                                                                                "6",
                //                                                                                "7"})) 'TODO: где-то занести значение в очередь
                // ''--- Тест ---------------------------------------------------------

                //Dim sync As Integer = Interlocked.CompareExchange(syncPointDoMonitor, 1, 0)
                //If sync = 0 Then
                if (itemTarget.CommandWriterQueue.Count > 0)
                {
                    while (itemTarget.CommandWriterQueue.Count > 0)
                    {
                        NetCommandForTask mNetCommandForTask = itemTarget.CommandWriterQueue.Dequeue();
                        SendRequestProgrammed(itemTarget, mNetCommandForTask);
                        // задача ни чего не дала
                        //Dim tsk As Task = Task.Factory.StartNew(Sub() SendRequestProgrammed(itemTarget, mNetCommandForTask.NameCommand, mNetCommandForTask.Parameters))
                        //tsk.Wait()
                    }
                }
                //syncPointDoMonitor = 0 ' освободить
            }

            //TODO: проверить не пришло ли время очистить буфера команд для target
            //If countTimedEvent > LIM_COUNT_TIMED_EVENT Then
            //    countTimedEvent = 0
            //    For  itemTarget As Target In ManagerAllTargets.ListTargets
            //        SendRequestProgrammed(itemTarget, COMMAND_NOTHING)
            //    Next
            //End If
        }

        /// <summary>
        /// Послать Запрос Программно
        /// Необходимо занести значения в параметры
        /// -> они заносятся в таблицу
        /// -> таблица сереализуется в XML (было)
        /// </summary>
        /// <param name="targetWriter"></param>
        /// <param name="inNetCommandForTask"></param>
        private void SendRequestProgrammed(Target targetWriter, NetCommandForTask inNetCommandForTask)
        {
            string procedureName = inNetCommandForTask.ProcedureName;
            var querytask = mTasksSendManager.Tasks.Where((task) => task.Value.ProcedureName == procedureName).First().Value;
            if (querytask == null)
            {
                throw new ArgumentNullException($"<{procedureName}> отсутствует в колекции <mTasksSendManager.Tasks> в процедуре <{nameof(SendRequestProgrammed)}>");
            }

            ManagerTaskApplication.TaskApplication taskSend = (ManagerTaskApplication.TaskApplication)(mTasksSendManager.Tasks[querytask.Name].Clone()); // десериализация задачи

            if (targetWriter.IsControlCommadVisible)
            {
                targetWriter.UserControlCommandTarget.SetNothigDataSourceForDataGridSend();
            }

            targetWriter.ConfigSend.Clear();
            targetWriter.ConfigSend.AddRow(COMMAND_NAME, taskSend.Name, TypeParam.String);
            targetWriter.ConfigSend.AddRow(COMMAND_DESCRIPTION, taskSend.Description, TypeParam.String);

            if (!(inNetCommandForTask.Parameters == null))
            {
                if (inNetCommandForTask.Parameters.Length == taskSend.Parameters.Values.Count)
                {
                    // скопировать в itemParameter значение parameters(itemParameter.Number - 1)
                    foreach (var itemParameter in taskSend.Parameters.Values)
                    {
                        targetWriter.ConfigSend.AddRow($"{itemParameter.Number} {COMMAND_PARAMETER}", inNetCommandForTask.Parameters[itemParameter.Number - 1], itemParameter.Type);
                    }
                }
                else
                {
                    MessageBox.Show($"Число переданных параметров для исполнения не соответствует{Environment.NewLine}числу параметров в конфигурационном описании задачи.", $"Процедура <{nameof(SendRequestProgrammed)}>", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                    return;
                }
            }

            if (inNetCommandForTask.IsResponse)
            {
                targetWriter.ConfigSend.AddRow(INDEX, inNetCommandForTask.IndexResponse, TypeParam.String);
            }
            else
            {
                //Dim random As Random = RandomProvider.GetThreadRandom()
                //Dim random As Random = New Random(Convert.ToInt32((DateTime.Now.Millisecond >> 32)))
                targetWriter.ConfigSend.AddRow(INDEX, Convert.ToInt32(RandomProvider.GetThreadRandom().NextDouble() * (Math.Pow(2, 31))).ToString(), TypeParam.String);
            }

            targetWriter.ConfigSend.AddRow(COMMAND_COMMANDER_ID, $"Компьютер:<{System.Environment.MachineName}> соединение:<{targetWriter.HostName}> канал:<{targetWriter.NamePiepSend}>", TypeParam.String);

            SendXMLCommand(targetWriter);

            if (targetWriter.IsControlCommadVisible)
            {
                targetWriter.UserControlCommandTarget.UpdateDataGridSend();
            }
        }

        //    <MethodImplAttribute(MethodImplOptions.Synchronized)>
        /// <summary>
        /// Сформировать XML Команду Отправить к Target.
        /// Вызов из дежурного таймера по цепочке.
        /// </summary>
        /// <param name="targetWriter"></param>
        public void SendXMLCommand(Target targetWriter)
        {
            string strIndex = targetWriter.ConfigSend.GetRowValue(INDEX)[0];

            // поиск в листе приема
            if (targetWriter.ListCommandsReceive.Count > 0)
            {
                foreach (CommandForListViewItem itemCommand in targetWriter.ListCommandsReceive)
                {
                    if (itemCommand.IndexCommamd == strIndex)
                    {
                        itemCommand.Color = Color.Green;
                        // задача Найдена - отвечаем на запрос после выполнения, ее надо снять
                        if (targetWriter.IsControlCommadVisible)
                        {
                            targetWriter.UserControlCommandTarget.RefreshListCommandsReceive();
                        }
                        break;
                    }
                }
            }

            // отметить и послать задачу на выполнение
            targetWriter.ListCommandsSend.Add(new CommandForListViewItem(targetWriter.ConfigSend.GetRowValue(COMMAND_NAME)[0], targetWriter.ConfigSend.GetRowValue(COMMAND_DESCRIPTION)[0], targetWriter.ConfigSend.GetRowValue(COMMAND_COMMANDER_ID)[0], targetWriter.ConfigSend.GetRowValue(INDEX)[0]));

            //' для команды Stop нет необходимости в упаковке в XML, поэтому посылается простой текст
            //If targetWriter.ConfigSend.GetRowValue(NAME_COMMAND) = COMMAND_STOP Then
            //    commandValueWriter = COMMAND_STOP
            //Else
            string commandXMLValueWriter = targetWriter.ConfigSend.ToString();
            //End If

            targetWriter.SendText = commandXMLValueWriter;

            if (targetWriter.IsControlCommadVisible)
            {
                targetWriter.UserControlCommandTarget.UpdateSendTextBox(commandXMLValueWriter);
            }

            SendCommandConcreteTargetAsync(targetWriter, commandXMLValueWriter);
            AppendMethod.Invoke($"Послана команда: {targetWriter.ConfigSend.GetRowValue(COMMAND_DESCRIPTION)} Индекс:{targetWriter.ConfigSend.GetRowValue(INDEX)}", targetWriter.IndexTab, MessageBoxIcon.Information);
            //RegistrationEventLog.EventLog_AUDIT_SUCCESS($"Послана команда {commandValueWriter} на target: {targetWriter.HostName}")
        }

        /// <summary>
        /// Отправить сформированную XML Команду к Target.
        /// Вызов из дежурного таймера по цепочке.
        /// </summary>
        /// <param name="targetWriter"></param>
        /// <remarks></remarks>
        private async void SendCommandConcreteTargetAsync(Target targetWriter, string commandValueWriter)
        {
            // при отсутствии связи ни чего не делать 
            counterTimedEvent = 0; // сбросить счётчик времени
            targetWriter.CommandValueWriter = commandValueWriter;
            // передать в сеть
            await targetWriter.WriteDataAsync(commandValueWriter);
        }
        #endregion

        #region RaiseEvent FormCommandClosed
        public void UcheckMenuCommandClientServer(bool isVisible)
        {
            //CType(mParentForm, FormMain).MenuCommandClientServer.Checked = False
            OnFormCommandClosed(new FormCommandVisibleClosedEventArg(false));
        }

        public delegate void FormCommandVisibleClosedEventHandler(object sender, FormCommandVisibleClosedEventArg e);
        public event FormCommandVisibleClosedEventHandler FormCommandClosed;

        private void OnFormCommandClosed(FormCommandVisibleClosedEventArg e)
        {
            //If FormCommandChanged IsNot Nothing Then
            FormCommandClosed?.Invoke(this, e);
            //End If
        }
        #endregion
    }

    public class FormCommandVisibleClosedEventArg : EventArgs
    {
        //Private eventAction As System.Action

        //Public Sub New(ByVal Message As String, ByVal ex As Exception, targetNumber As Integer) ', ByVal action As System.Action)
        //    MyBase.New()
        //    Me.Message = Message
        //    Me.ex = ex
        //    Me.targetNumber = targetNumber
        //    'Me.eventAction = action
        //End Sub

        //Public ReadOnly Property Message() As String
        //Public ReadOnly Property ex As Exception
        //Public ReadOnly Property targetNumber As Integer

        //'Public ReadOnly Property Action() As System.Action
        //'    Get
        //'        Return Me.eventAction
        //'    End Get
        //'End Property

        public bool IsVisible { get; }

        public FormCommandVisibleClosedEventArg(bool inIsVisible) : base()
        {

            IsVisible = inIsVisible;
        }
    }
}

//Private Sub GetEnum()
//    Console.WriteLine("**** Очень простой генератор подключений *****" & vbLf)
//    Dim ClassName As String = System.Configuration.ConfigurationManager.AppSettings("DiagramClassName")

//    ' Чтение ключа поставщика. 
//    Dim replyString As String = ConfigurationManager.AppSettings("provider")
//    ' Преобразование строки в перечисление. 
//    Dim dp As Reply = Reply.Unknown
//    If [Enum].IsDefined(GetType(Reply), replyString) Then
//        dp = DirectCast([Enum].Parse(GetType(Reply), replyString), Reply)
//    Else
//        Console.WriteLine("К сожалению, поставщик отсутствует.")
//    End If

//    ' Получение конкретного подключения. 
//    Dim myCn As IDbConnection = GetConnection(dp)
//    If myCn IsNot Nothing Then
//        Console.WriteLine("Ваше подключение — {О}", myCn.[GetType]().Name)
//    End If
//    ' Открытие, использование и закрытие подключения . . . 
//    Console.ReadLine()
//End Sub